using Godot;
using EasyTestBridge.Config;
using EasyTestBridge.Core;
using EasyTestBridge.Input;
using EasyTestBridge.Capture;
using EasyTestBridge.Testing;

namespace EasyTestBridge;

/// <summary>
/// EasyTestBridge 主入口 Autoload 单例
/// Debug 模式下自动启动，发布版本完全禁用
/// </summary>
[GlobalClass]
public partial class EasyTestBridge : Node
{
    /// <summary>配置</summary>
    [Export] public EasyTestBridgeConfig Config { get; set; } = new();

    /// <summary>节点注册表</summary>
    public TestableRegistry Registry { get; private set; } = new();

    /// <summary>输入模拟器</summary>
    public InputSimulator InputSim { get; private set; } = new();

    /// <summary>状态采集器</summary>
    public StateCollector Collector { get; private set; } = new();

    /// <summary>测试运行器</summary>
    public TestRunner TestRunner { get; private set; } = new();

    /// <summary>WebSocket 服务器</summary>
    private TestBridgeServer? _server;

    /// <summary>HTTP 桥接</summary>
    private HttpBridge? _http;

    /// <summary>是否已启动</summary>
    public bool IsRunning { get; private set; }

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            GD.Print("[EasyTestBridge] 发布模式，已禁用");
            return;
        }

        // 初始化子模块
        AddChild(Collector);
        AddChild(TestRunner);

        // 启动 WebSocket 服务器
        _server = new TestBridgeServer();
        _server.Setup(Registry, InputSim, Collector, TestRunner, Config.Port);
        AddChild(_server);

        // 启动 HTTP 桥接
        _http = new HttpBridge();
        _http.Setup(_server, Config.HttpPort);
        AddChild(_http);

        IsRunning = true;
        GD.Print($"[EasyTestBridge] 已启动，WebSocket ws://localhost:{Config.Port} | HTTP http://localhost:{Config.HttpPort}");

        // 自动运行测试（延迟到下一帧，等游戏节点注册完毕）
        if (Config.RunTestsOnStart)
            CallDeferred(nameof(autoRunTests));
    }

    public override void _ExitTree()
    {
        _http?.Shutdown();
        _server?.Shutdown();
        IsRunning = false;
    }

    private async void autoRunTests()
    {
        // 等两帧，确保游戏节点 _Ready() 完成并注册测试套件
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await TestRunner.runTests();
    }
}
