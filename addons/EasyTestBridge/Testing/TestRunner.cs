using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using EasyTestBridge.Core;

namespace EasyTestBridge.Testing;

/// <summary>
/// 测试结果
/// </summary>
public class TestResult
{
    public string Name { get; set; } = "";
    public string? Tag { get; set; }
    public bool Passed { get; set; }
    public string? Error { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// 测试报告
/// </summary>
public class TestReport
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public long TotalDurationMs { get; set; }
    public List<TestResult> Results { get; set; } = new();
}

/// <summary>
/// 测试运行器 — 发现、执行、报告测试
/// </summary>
public partial class TestRunner : Node
{
    private readonly List<object> _suites = new();

    public bool IsRunning { get; private set; }
    public TestReport? LastReport { get; private set; }

    /// <summary>注册一个测试套件实例</summary>
    public void registerSuite(object suite)
    {
        _suites.Add(suite);
        GD.Print($"[TestRunner] 注册测试套件: {suite.GetType().Name}");
    }

    /// <summary>运行所有匹配的测试</summary>
    public async Task<TestReport> runTests(string? filter = null)
    {
        if (IsRunning)
            return LastReport ?? new TestReport();

        IsRunning = true;
        var report = new TestReport();
        var startTime = Time.GetTicksMsec();
        var ctx = new TestContext(GetTree());

        foreach (var suite in _suites)
        {
            var methods = discoverTests(suite, filter);
            foreach (var (method, attr) in methods)
            {
                var result = await runSingleTest(suite, method, attr, ctx);
                report.Results.Add(result);
            }
        }

        report.Total = report.Results.Count;
        report.Passed = report.Results.Count(r => r.Passed);
        report.Failed = report.Total - report.Passed;
        report.TotalDurationMs = (long)(Time.GetTicksMsec() - startTime);

        LastReport = report;
        IsRunning = false;

        logReport(report);
        return report;
    }

    /// <summary>处理 WebSocket "test" 命令</summary>
    public string handleTestCommand(JsonElement root)
    {
        if (IsRunning)
            return TestBridgeServer.Respond(false, "tests already running");

        string? filter = null;
        if (root.TryGetProperty("filter", out var filterProp))
            filter = filterProp.GetString();

        _ = runTests(filter);
        return TestBridgeServer.Respond(true, "tests started");
    }

    /// <summary>处理 WebSocket "test_result" 命令</summary>
    public string handleTestResultCommand()
    {
        if (IsRunning)
            return TestBridgeServer.Respond(true, new { status = "running" });

        if (LastReport == null)
            return TestBridgeServer.Respond(true, new { status = "no_results" });

        return TestBridgeServer.Respond(true, new
        {
            status = "done",
            total = LastReport.Total,
            passed = LastReport.Passed,
            failed = LastReport.Failed,
            durationMs = LastReport.TotalDurationMs,
            results = LastReport.Results.Select(r => new
            {
                name = r.Name, tag = r.Tag, passed = r.Passed,
                error = r.Error, durationMs = r.DurationMs
            })
        });
    }

    private List<(MethodInfo method, GameTestAttribute attr)> discoverTests(
        object suite, string? filter)
    {
        var results = new List<(MethodInfo, GameTestAttribute)>();
        foreach (var method in suite.GetType().GetMethods(
            BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = method.GetCustomAttribute<GameTestAttribute>();
            if (attr == null) continue;

            var testName = attr.Name ?? method.Name;
            if (filter != null && !matchesFilter(testName, attr.Tag, filter))
                continue;

            results.Add((method, attr));
        }
        return results;
    }

    private async Task<TestResult> runSingleTest(
        object suite, MethodInfo method, GameTestAttribute attr, TestContext ctx)
    {
        var testName = attr.Name ?? method.Name;
        var result = new TestResult { Name = testName, Tag = attr.Tag };
        var startTime = Time.GetTicksMsec();

        GD.Print($"[TestRunner] 运行: {testName}");

        try
        {
            var returnValue = method.GetParameters().Length switch
            {
                0 => method.Invoke(suite, null),
                1 => method.Invoke(suite, new object[] { ctx }),
                _ => throw new InvalidOperationException(
                    $"test method {testName} has unsupported parameter count")
            };

            if (returnValue is Task task)
            {
                if (attr.TimeoutMs > 0)
                {
                    var timeoutTask = Task.Delay(attr.TimeoutMs);
                    var completed = await Task.WhenAny(task, timeoutTask);
                    if (completed == timeoutTask)
                        throw new AssertionException(
                            $"test timed out after {attr.TimeoutMs}ms");
                    await task;
                }
                else
                {
                    await task;
                }
            }

            result.Passed = true;
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            result.Passed = false;
            result.Error = tie.InnerException.Message;
        }
        catch (Exception e)
        {
            result.Passed = false;
            result.Error = e.Message;
        }

        result.DurationMs = (long)(Time.GetTicksMsec() - startTime);
        var status = result.Passed ? "PASS" : "FAIL";
        GD.Print($"[TestRunner] {status}: {testName} ({result.DurationMs}ms)");
        if (!result.Passed)
            GD.PrintErr($"[TestRunner]   错误: {result.Error}");

        return result;
    }

    private static bool matchesFilter(string name, string? tag, string filter)
    {
        if (filter.StartsWith("tag:"))
        {
            var tagFilter = filter.Substring(4);
            return tag != null && tag.Contains(tagFilter,
                StringComparison.OrdinalIgnoreCase);
        }
        return name.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static void logReport(TestReport report)
    {
        GD.Print("========================================");
        GD.Print($"[TestRunner] 测试完成: {report.Total} 总计, " +
                 $"{report.Passed} 通过, {report.Failed} 失败 " +
                 $"({report.TotalDurationMs}ms)");

        if (report.Failed > 0)
        {
            GD.PrintErr("[TestRunner] 失败的测试:");
            foreach (var r in report.Results.Where(r => !r.Passed))
                GD.PrintErr($"  - {r.Name}: {r.Error}");
        }
        GD.Print("========================================");
    }
}
