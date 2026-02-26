# Changelog

## [1.1.0] - 2026-02-26

### Added
- HTTP API server (port 9877) — zero-dependency alternative to WebSocket, use with `curl`
- `find` command — search nodes by name or type (fuzzy match)
- `inspect` command — inspect any node's public properties by path
- All existing WebSocket commands accessible via HTTP GET

### Changed
- `TestBridgeServer.HandleMessage` is now public for HTTP bridge reuse

## [1.0.0] - 2026-02-26

### Added
- WebSocket server for remote game inspection (port 9876)
- Node registration and reflection-based state inspection
- Input simulation (keyboard, mouse, action, text)
- State capture (FPS, memory, logs, screenshots, scene tree)
- Built-in test runner with `[GameTest]` attribute
- `TestAssert` assertion library
- `TestContext` async utilities (waitFrames, waitSeconds, waitUntil)
- Tag-based test filtering
- Auto-run tests on startup option
- Demo project with DemoPlayer and DemoCounter examples
