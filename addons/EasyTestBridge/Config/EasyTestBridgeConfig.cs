using Godot;

namespace EasyTestBridge.Config;

[GlobalClass]
public partial class EasyTestBridgeConfig : Resource
{
    /// <summary>WebSocket 监听端口</summary>
    [Export] public int Port { get; set; } = 9876;

    /// <summary>HTTP 监听端口</summary>
    [Export] public int HttpPort { get; set; } = 9877;

    /// <summary>是否启用 WebSocket 服务器</summary>
    [Export] public bool EnableWebSocket { get; set; } = true;

    /// <summary>最大同时连接的客户端数</summary>
    [Export] public int MaxClients { get; set; } = 4;

    /// <summary>截图质量 (0-100)</summary>
    [Export] public int ScreenshotQuality { get; set; } = 75;

    /// <summary>是否在游戏启动时自动运行测试</summary>
    [Export] public bool RunTestsOnStart { get; set; } = true;
}
