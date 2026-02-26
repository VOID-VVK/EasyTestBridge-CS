using System;

namespace EasyTestBridge.Testing;

/// <summary>
/// 标记一个方法为游戏测试用例
/// 支持两种签名: void Method() 或 async Task Method(TestContext ctx)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class GameTestAttribute : Attribute
{
    /// <summary>测试名称（默认使用方法名）</summary>
    public string? Name { get; set; }

    /// <summary>测试分类标签，用于过滤</summary>
    public string? Tag { get; set; }

    /// <summary>超时（毫秒），0 = 无超时</summary>
    public int TimeoutMs { get; set; }
}
