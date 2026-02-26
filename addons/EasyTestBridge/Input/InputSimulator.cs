using Godot;
using System;
using System.Text.Json;
using EasyTestBridge.Core;

namespace EasyTestBridge.Input;

public class InputSimulator
{
    /// <summary>处理输入指令</summary>
    public string HandleInput(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeProp))
            return TestBridgeServer.Respond(false, "missing 'type' property");

        var type = typeProp.GetString();

        return type switch
        {
            "key" => HandleKey(root),
            "mouse" => HandleMouse(root),
            "action" => HandleAction(root),
            "text" => HandleText(root),
            _ => TestBridgeServer.Respond(false, $"unknown input type: {type}")
        };
    }

    private string HandleKey(JsonElement root)
    {
        if (!root.TryGetProperty("key", out var keyProp))
            return TestBridgeServer.Respond(false, "missing 'key' property");

        var keyStr = keyProp.GetString()!;
        if (!Enum.TryParse<Key>(keyStr, true, out var key))
            return TestBridgeServer.Respond(false, $"unknown key: {keyStr}");

        var pressed = true;
        if (root.TryGetProperty("pressed", out var pressedProp))
            pressed = pressedProp.GetBoolean();

        SimulateKey(key, pressed);
        return TestBridgeServer.Respond(true, $"key {keyStr} {(pressed ? "pressed" : "released")}");
    }

    private string HandleMouse(JsonElement root)
    {
        if (!root.TryGetProperty("x", out var xProp) || !root.TryGetProperty("y", out var yProp))
            return TestBridgeServer.Respond(false, "missing 'x' or 'y' property");

        var x = xProp.GetSingle();
        var y = yProp.GetSingle();
        var buttonStr = "left";
        var pressed = true;

        if (root.TryGetProperty("button", out var btnProp))
            buttonStr = btnProp.GetString()!;
        if (root.TryGetProperty("pressed", out var pressedProp))
            pressed = pressedProp.GetBoolean();

        var button = buttonStr switch
        {
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => MouseButton.Left
        };

        SimulateMouse(x, y, button, pressed);
        return TestBridgeServer.Respond(true, $"mouse {buttonStr} at ({x}, {y})");
    }

    private string HandleAction(JsonElement root)
    {
        if (!root.TryGetProperty("action", out var actionProp))
            return TestBridgeServer.Respond(false, "missing 'action' property");

        var action = actionProp.GetString()!;
        Godot.Input.ActionPress(action);

        var tree = Engine.GetMainLoop() as SceneTree;
        tree?.CreateTimer(0.1).Connect("timeout", Callable.From(() => Godot.Input.ActionRelease(action)));

        return TestBridgeServer.Respond(true, $"action '{action}' triggered");
    }

    private string HandleText(JsonElement root)
    {
        if (!root.TryGetProperty("text", out var textProp))
            return TestBridgeServer.Respond(false, "missing 'text' property");

        var text = textProp.GetString()!;

        foreach (var c in text)
        {
            var evDown = new InputEventKey
            {
                Keycode = (Key)c,
                Unicode = c,
                Pressed = true
            };
            Godot.Input.ParseInputEvent(evDown);

            var evUp = new InputEventKey
            {
                Keycode = (Key)c,
                Unicode = c,
                Pressed = false
            };
            Godot.Input.ParseInputEvent(evUp);
        }

        return TestBridgeServer.Respond(true, $"text '{text}' typed");
    }

    public void SimulateKey(Key key, bool pressed)
    {
        var ev = new InputEventKey
        {
            Keycode = key,
            Pressed = pressed
        };
        Godot.Input.ParseInputEvent(ev);
    }

    public void SimulateMouse(float x, float y, MouseButton button, bool pressed)
    {
        var ev = new InputEventMouseButton
        {
            Position = new Vector2(x, y),
            GlobalPosition = new Vector2(x, y),
            ButtonIndex = button,
            Pressed = pressed
        };
        Godot.Input.ParseInputEvent(ev);
    }
}
