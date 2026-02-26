using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using EasyTestBridge.Core;

namespace EasyTestBridge.Capture;

public partial class StateCollector : Node
{
    /// <summary>最大缓存的日志条数</summary>
    private const int MaxLogCache = 200;

    private readonly List<string> _logCache = new();

    /// <summary>获取性能数据</summary>
    public string GetPerformance()
    {
        var data = new Dictionary<string, object>
        {
            ["fps"] = Engine.GetFramesPerSecond(),
            ["delta"] = GetProcessDeltaTime(),
            ["static_memory_mb"] = OS.GetStaticMemoryUsage() / 1048576.0,
            ["object_count"] = Performance.GetMonitor(Performance.Monitor.ObjectCount),
            ["node_count"] = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
            ["time"] = Time.GetTicksMsec()
        };

        return TestBridgeServer.Respond(true, data);
    }

    /// <summary>获取日志</summary>
    public string GetLogs(JsonElement root)
    {
        int last = 50;
        if (root.TryGetProperty("last", out var lastProp))
            last = lastProp.GetInt32();

        var logs = _logCache.TakeLast(last).ToList();
        return TestBridgeServer.Respond(true, new { logs });
    }

    /// <summary>截图</summary>
    public string TakeScreenshot()
    {
        try
        {
            var viewport = GetViewport();
            if (viewport == null)
                return TestBridgeServer.Respond(false, "no viewport available");

            var image = viewport.GetTexture().GetImage();
            var png = image.SavePngToBuffer();
            var base64 = Convert.ToBase64String(png);

            return TestBridgeServer.Respond(true, new
            {
                format = "png",
                width = image.GetWidth(),
                height = image.GetHeight(),
                data = base64
            });
        }
        catch (Exception e)
        {
            return TestBridgeServer.Respond(false, $"screenshot failed: {e.Message}");
        }
    }

    /// <summary>获取场景树</summary>
    public string GetSceneTree(JsonElement root)
    {
        try
        {
            var maxDepth = 3;
            if (root.TryGetProperty("depth", out var depthProp))
                maxDepth = depthProp.GetInt32();

            var rootNode = GetTree().Root;
            if (root.TryGetProperty("root", out var rootProp))
            {
                var path = rootProp.GetString()!;
                rootNode = (GetTree().Root.GetNodeOrNull(path) as Window) ?? GetTree().Root;
            }

            var tree = BuildTree(rootNode, 0, maxDepth);
            return TestBridgeServer.Respond(true, tree);
        }
        catch (Exception e)
        {
            return TestBridgeServer.Respond(false, $"scene tree failed: {e.Message}");
        }
    }

    /// <summary>按名称/类型搜索场景树节点</summary>
    public string FindNodes(JsonElement root)
    {
        try
        {
            string? namePattern = null;
            string? typePattern = null;

            if (root.TryGetProperty("name", out var nameProp))
                namePattern = nameProp.GetString();
            if (root.TryGetProperty("type", out var typeProp))
                typePattern = typeProp.GetString();

            if (namePattern == null && typePattern == null)
                return TestBridgeServer.Respond(false, "need 'name' or 'type' property");

            var results = new List<Dictionary<string, object>>();
            SearchTree(GetTree().Root, namePattern, typePattern, results);

            return TestBridgeServer.Respond(true, new { count = results.Count, nodes = results });
        }
        catch (Exception e)
        {
            return TestBridgeServer.Respond(false, $"find failed: {e.Message}");
        }
    }

    private static void SearchTree(Node node, string? namePattern, string? typePattern,
        List<Dictionary<string, object>> results)
    {
        var nodeName = node.Name.ToString();
        var nodeType = node.GetType().Name;

        bool match = true;
        if (namePattern != null)
            match &= nodeName.Contains(namePattern, StringComparison.OrdinalIgnoreCase);
        if (typePattern != null)
            match &= nodeType.Contains(typePattern, StringComparison.OrdinalIgnoreCase);

        if (match)
        {
            results.Add(new Dictionary<string, object>
            {
                ["name"] = nodeName,
                ["type"] = nodeType,
                ["path"] = node.GetPath().ToString()
            });
        }

        foreach (var child in node.GetChildren())
            SearchTree(child, namePattern, typePattern, results);
    }

    /// <summary>获取任意节点的属性（按路径）</summary>
    public string GetNodeByPath(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("path", out var pathProp))
                return TestBridgeServer.Respond(false, "missing 'path' property");

            var path = pathProp.GetString()!;
            var node = GetTree().Root.GetNodeOrNull(path);
            if (node == null)
                return TestBridgeServer.Respond(false, $"node not found: {path}");

            var props = GetExportProperties(node);
            props["_name"] = node.Name.ToString();
            props["_type"] = node.GetType().Name;
            props["_path"] = node.GetPath().ToString();
            props["_children"] = node.GetChildCount();

            return TestBridgeServer.Respond(true, props);
        }
        catch (Exception e)
        {
            return TestBridgeServer.Respond(false, $"inspect failed: {e.Message}");
        }
    }

    /// <summary>获取节点的 [Export] 和公开属性</summary>
    private static Dictionary<string, object?> GetExportProperties(Node node)
    {
        var props = new Dictionary<string, object?>();
        var type = node.GetType();

        foreach (var prop in type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            try
            {
                props[prop.Name] = SerializeValue(prop.GetValue(node));
            }
            catch { /* skip */ }
        }

        return props;
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

    private static Dictionary<string, object> BuildTree(Node node, int depth, int maxDepth)
    {
        var tree = new Dictionary<string, object>
        {
            ["name"] = node.Name.ToString(),
            ["type"] = node.GetType().Name,
            ["path"] = node.GetPath().ToString()
        };

        if (depth < maxDepth)
        {
            var children = new List<Dictionary<string, object>>();
            foreach (var child in node.GetChildren())
            {
                children.Add(BuildTree(child, depth + 1, maxDepth));
            }
            if (children.Count > 0)
                tree["children"] = children;
        }

        return tree;
    }
}
