# WebSocket Protocol Reference

EasyTestBridge exposes a WebSocket server (default port 9876) that accepts JSON commands and returns JSON responses.

## Connection

```
ws://localhost:9876
```

## Response Format

All responses follow this structure:

```json
{
  "ok": true,
  "data": <response_data>
}
```

On error:

```json
{
  "ok": false,
  "data": "error message"
}
```

## Commands

### ping

Health check.

```json
{"cmd": "ping"}
```

Response: `{"ok": true, "data": "pong"}`

### nodes

List all registered node names.

```json
{"cmd": "nodes"}
```

Response:
```json
{"ok": true, "data": {"nodes": ["player", "enemy"]}}
```

### state

Get all registered nodes' current state (public properties via reflection).

```json
{"cmd": "state"}
```

### get

Get a specific node's state.

```json
{"cmd": "get", "node": "player"}
```

### call

Invoke a registered action.

```json
{"cmd": "call", "action": "reset_player", "data": {"amount": 50}}
```

The `data` field is optional and passed to the action handler.

### input

Simulate input events. The `type` field determines the input kind.

#### Key input

```json
{"cmd": "input", "type": "key", "key": "Space", "pressed": true}
```

- `key`: Godot Key enum name (e.g., "Space", "A", "Escape")
- `pressed`: optional, defaults to `true`

#### Mouse input

```json
{"cmd": "input", "type": "mouse", "x": 100, "y": 200, "button": "left", "pressed": true}
```

- `button`: "left" (default), "right", "middle"
- `pressed`: optional, defaults to `true`

#### Action input

```json
{"cmd": "input", "type": "action", "action": "ui_accept"}
```

Presses the action and auto-releases after 0.1s.

#### Text input

```json
{"cmd": "input", "type": "text", "text": "hello"}
```

Simulates typing each character as key press + release.

### perf

Get performance metrics.

```json
{"cmd": "perf"}
```

Response:
```json
{
  "ok": true,
  "data": {
    "fps": 60,
    "delta": 0.016,
    "static_memory_mb": 128.5,
    "object_count": 1234,
    "node_count": 567,
    "time": 12345678
  }
}
```

### logs

Get cached log entries.

```json
{"cmd": "logs", "last": 20}
```

- `last`: number of recent entries to return (default: 50)

### screenshot

Capture the current viewport as base64 PNG.

```json
{"cmd": "screenshot"}
```

Response:
```json
{
  "ok": true,
  "data": {
    "format": "png",
    "width": 1920,
    "height": 1080,
    "data": "<base64_png_data>"
  }
}
```

### scene

Get the scene tree structure.

```json
{"cmd": "scene", "depth": 3, "root": "/root/Main"}
```

- `depth`: max traversal depth (default: 3)
- `root`: optional root node path (default: viewport root)

### test

Run tests (optionally filtered).

```json
{"cmd": "test", "filter": "tag:player"}
```

- `filter`: optional — name substring or `tag:<tagname>` prefix

Response: `{"ok": true, "data": "tests started"}`

### test_result

Get the latest test results.

```json
{"cmd": "test_result"}
```

Response (when complete):
```json
{
  "ok": true,
  "data": {
    "status": "done",
    "total": 5,
    "passed": 4,
    "failed": 1,
    "durationMs": 120,
    "results": [
      {
        "name": "Player_InitialHealth",
        "tag": "player",
        "passed": true,
        "error": null,
        "durationMs": 2
      }
    ]
  }
}
```

Possible `status` values: `"running"`, `"done"`, `"no_results"`
