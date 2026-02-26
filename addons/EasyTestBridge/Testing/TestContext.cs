using Godot;
using System;
using System.Threading.Tasks;

namespace EasyTestBridge.Testing;

/// <summary>
/// 测试上下文 — 提供帧等待、信号等待等异步工具
/// </summary>
public class TestContext
{
    private readonly SceneTree _tree;

    public TestContext(SceneTree tree)
    {
        _tree = tree;
    }

    /// <summary>等待 N 帧</summary>
    public async Task waitFrames(int count = 1)
    {
        for (int i = 0; i < count; i++)
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>等待指定秒数</summary>
    public async Task waitSeconds(double seconds)
    {
        await _tree.ToSignal(
            _tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>等待条件为 true（每帧检查，带超时）</summary>
    public async Task waitUntil(Func<bool> predicate, double timeoutSeconds = 5.0)
    {
        var elapsed = 0.0;
        while (!predicate())
        {
            var before = Time.GetTicksMsec();
            await waitFrames(1);
            var after = Time.GetTicksMsec();
            elapsed += (after - before) / 1000.0;

            if (elapsed >= timeoutSeconds)
                throw new AssertionException(
                    $"waitUntil timed out after {timeoutSeconds}s");
        }
    }

    /// <summary>获取场景树</summary>
    public SceneTree getTree() => _tree;
}
