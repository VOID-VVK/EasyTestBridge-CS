# EasyTestBridge

An in-game testing bridge for Godot 4 C# projects.

EasyTestBridge runs inside your game as an autoload singleton. It provides a WebSocket server for remote inspection, input simulation, state capture, and a built-in test runner with `[GameTest]` attribute.

## Features

- **WebSocket Server** (port 9876) for remote game inspection
- **Node Registration** and state inspection via reflection
- **Input Simulation** — keyboard, mouse, actions, text
- **State Capture** — FPS/memory metrics, log collection, viewport screenshots, scene tree
- **Built-in Test Runner** with `[GameTest]` attribute
- **TestAssert** assertion library (`isTrue`, `areEqual`, `isNull`, etc.)
- **TestContext** async utilities (`waitFrames`, `waitSeconds`, `waitUntil`)
- **Tag-based** test filtering

## Requirements

- Godot 4.4+ (Mono/C#)
- .NET 8 SDK

## Installation

### From Godot Asset Library

1. Open Godot Editor → AssetLib tab
2. Search "EasyTestBridge"
3. Download and install
4. Enable the plugin: Project → Project Settings → Plugins → EasyTestBridge ✓

### Manual

1. Copy `addons/EasyTestBridge/` into your project's `addons/` folder
2. Enable the plugin: Project → Project Settings → Plugins → EasyTestBridge ✓

### Setup Autoload

Add EasyTestBridge as an autoload singleton:

Project → Project Settings → Autoload → Add:
- Path: `res://addons/EasyTestBridge/EasyTestBridge.cs`
- Name: `EasyTestBridge`

## Quick Start

### 1. Register Nodes

In your game node's `_Ready()`:

```csharp
var bridge = GetNode<EasyTestBridge.EasyTestBridge>("/root/EasyTestBridge");
bridge.Registry.Register("player", this);
```

### 2. Register Actions

```csharp
bridge.Registry.RegisterAction("reset_player", _ =>
{
    Health = 100;
    return TestBridgeServer.Respond(true, "player reset");
});
```

### 3. Write Tests

```csharp
using EasyTestBridge.Testing;

public class PlayerTests
{
    [GameTest(Tag = "player")]
    public void Player_InitialHealth()
    {
        TestAssert.areEqual(100, 100);
    }

    [GameTest(Tag = "player", TimeoutMs = 5000)]
    public async Task Player_Movement(TestContext ctx)
    {
        await ctx.waitFrames(10);
        TestAssert.isTrue(true);
    }
}
```

### 4. Register Test Suites

Create an autoload to register your test suites:

```csharp
public partial class TestRegistrar : Node
{
    public override void _Ready()
    {
        var bridge = GetNode<EasyTestBridge.EasyTestBridge>("/root/EasyTestBridge");
        bridge.TestRunner.registerSuite(new PlayerTests());
    }
}
```

Tests run automatically on startup when `RunTestsOnStart = true` (default).

## Configuration

| Property | Default | Description |
|----------|---------|-------------|
| `Port` | 9876 | WebSocket listen port |
| `EnableWebSocket` | true | Enable/disable WebSocket server |
| `MaxClients` | 4 | Max concurrent WebSocket clients |
| `ScreenshotQuality` | 75 | Screenshot quality (0-100) |
| `RunTestsOnStart` | true | Auto-run tests on game start |

## WebSocket Commands

Connect to `ws://localhost:9876` and send JSON:

| Command | Example | Description |
|---------|---------|-------------|
| `ping` | `{"cmd":"ping"}` | Health check |
| `nodes` | `{"cmd":"nodes"}` | List registered nodes |
| `state` | `{"cmd":"state"}` | Get all node states |
| `get` | `{"cmd":"get","node":"player"}` | Get specific node state |
| `call` | `{"cmd":"call","action":"reset"}` | Call registered action |
| `input` | `{"cmd":"input","type":"key","key":"Space"}` | Simulate input |
| `perf` | `{"cmd":"perf"}` | Get performance metrics |
| `logs` | `{"cmd":"logs","last":20}` | Get recent logs |
| `screenshot` | `{"cmd":"screenshot"}` | Capture viewport |
| `scene` | `{"cmd":"scene","depth":3}` | Get scene tree |
| `test` | `{"cmd":"test"}` | Run tests |
| `test_result` | `{"cmd":"test_result"}` | Get test results |

See [docs/websocket-protocol.md](docs/websocket-protocol.md) for full protocol reference.

## TestAssert API

| Method | Description |
|--------|-------------|
| `isTrue(condition)` | Assert condition is true |
| `isFalse(condition)` | Assert condition is false |
| `areEqual(expected, actual)` | Assert values are equal |
| `areNotEqual(expected, actual)` | Assert values differ |
| `isNotNull(value)` | Assert value is not null |
| `isNull(value)` | Assert value is null |
| `fail(message)` | Explicitly fail |

## TestContext API

| Method | Description |
|--------|-------------|
| `waitFrames(count)` | Wait N frames |
| `waitSeconds(seconds)` | Wait specified duration |
| `waitUntil(predicate, timeout)` | Wait until condition is true |
| `getTree()` | Access the SceneTree |

## Notes

- EasyTestBridge is automatically disabled in release builds (`OS.IsDebugBuild()`)
- The `demo/` folder is included for reference — you can safely delete it from your project

## License

MIT
