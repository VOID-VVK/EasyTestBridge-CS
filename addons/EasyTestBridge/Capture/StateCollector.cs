using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
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
