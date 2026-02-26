using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace EasyTestBridge.Core;

public class TestableRegistry
{
    /// <summary>注册的节点，键为注册名</summary>
    private readonly Dictionary<string, Node> _nodes = new();

    /// <summary>注册的动作处理器</summary>
    private readonly Dictionary<string, Func<JsonElement, string>> _actions = new();

    /// <summary>注册一个可测试节点</summary>
    public void Register(string name, Node node)
    {
        _nodes[name] = node;
        GD.Print($"[TestableRegistry] 注册节点: {name}");
    }

    /// <summary>注销节点</summary>
    public void Unregister(string name)
    {
        _nodes.Remove(name);
    }

    /// <summary>注册一个可调用动作</summary>
    public void RegisterAction(string name, Func<JsonElement, string> handler)
    {
        _actions[name] = handler;
    }

    /// <summary>列出所有注册的节点</summary>
    public string ListNodes()
    {
        var nodeNames = _nodes.Keys.ToList();
        return TestBridgeServer.Respond(true, new { nodes = nodeNames });
    }

    /// <summary>获取所有节点的当前状态</summary>
    public string GetAllStates()
    {
        var states = new Dictionary<string, object>();
        foreach (var (name, node) in _nodes)
        {
            states[name] = GetNodeProperties(node);
        }
        return TestBridgeServer.Respond(true, states);
    }

    /// <summary>获取指定节点的状态</summary>
    public string GetNodeState(JsonElement root)
    {
        if (!root.TryGetProperty("node", out var nodeProp))
            return TestBridgeServer.Respond(false, "missing 'node' property");

        var nodeName = nodeProp.GetString()!;
        if (!_nodes.TryGetValue(nodeName, out var node))
            return TestBridgeServer.Respond(false, $"node not found: {nodeName}");

        return TestBridgeServer.Respond(true, GetNodeProperties(node));
    }

    /// <summary>调用节点动作</summary>
    public string CallAction(JsonElement root)
    {
        if (!root.TryGetProperty("action", out var actionProp))
            return TestBridgeServer.Respond(false, "missing 'action' property");

        var actionName = actionProp.GetString()!;
        if (!_actions.TryGetValue(actionName, out var handler))
            return TestBridgeServer.Respond(false, $"action not found: {actionName}");

        JsonElement data = default;
        root.TryGetProperty("data", out data);

        try
        {
            return handler(data);
        }
        catch (Exception e)
        {
            return TestBridgeServer.Respond(false, e.Message);
        }
    }

    /// <summary>通过反射获取节点的可读属性</summary>
    private static Dictionary<string, object?> GetNodeProperties(Node node)
    {
        var state = new Dictionary<string, object?>();
        var type = node.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!prop.CanRead) continue;
            if (prop.GetIndexParameters().Length > 0) continue;

            try
            {
                var value = prop.GetValue(node);
                state[prop.Name] = SerializeValue(value);
            }
            catch
            {
                // 跳过不可读的属性
            }
        }

        return state;
    }

    /// <summary>序列化值为 JSON 兼容格式</summary>
    private static object? SerializeValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            Vector2 v => new { x = v.X, y = v.Y },
            Vector2I v => new { x = v.X, y = v.Y },
            Vector3 v => new { x = v.X, y = v.Y, z = v.Z },
            bool b => b,
            int i => i,
            float f => f,
            double d => d,
            string s => s,
            _ => value.ToString()
        };
    }
}
