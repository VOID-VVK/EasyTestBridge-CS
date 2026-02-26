using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using EasyTestBridge.Input;
using EasyTestBridge.Capture;
using EasyTestBridge.Testing;

namespace EasyTestBridge.Core;

public partial class TestBridgeServer : Node
{
    private TcpServer _tcp = new();
    private readonly List<WebSocketPeer> _clients = new();
    private int _port = 9876;

    private TestableRegistry _registry = null!;
    private InputSimulator _input = null!;
    private StateCollector _collector = null!;
    private TestRunner? _testRunner;

    public void Setup(TestableRegistry registry, InputSimulator input,
        StateCollector collector, TestRunner testRunner, int port)
    {
        _registry = registry;
        _input = input;
        _collector = collector;
        _testRunner = testRunner;
        _port = port;

        var err = _tcp.Listen((ushort)_port);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[TestBridgeServer] Failed to start: {err}");
            return;
        }
    }

    public override void _Process(double delta)
    {
        while (_tcp.IsConnectionAvailable())
        {
            var peer = new WebSocketPeer();
            peer.AcceptStream(_tcp.TakeConnection());
            _clients.Add(peer);
            GD.Print("[TestBridgeServer] Client connected");
        }

        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var client = _clients[i];
            client.Poll();

            var state = client.GetReadyState();
            if (state == WebSocketPeer.State.Closed)
            {
                _clients.RemoveAt(i);
                continue;
            }

            while (client.GetAvailablePacketCount() > 0)
            {
                try
                {
                    var msg = client.GetPacket().GetStringFromUtf8();
                    var response = ProcessCommand(msg);
                    client.PutPacket(response.ToUtf8Buffer());
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[TestBridgeServer] Message processing error: {e.Message}");
                }
            }
        }
    }

    public string ProcessCommand(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var cmd = root.GetProperty("cmd").GetString();

            return cmd switch
            {
                "ping" => Respond(true, "pong"),
                "nodes" => _registry.ListNodes(),
                "state" => _registry.GetAllStates(),
                "get" => _registry.GetNodeState(root),
                "call" => _registry.CallAction(root),
                "input" => _input.HandleInput(root),
                "perf" => _collector.GetPerformance(),
                "logs" => _collector.GetLogs(root),
                "screenshot" => _collector.TakeScreenshot(),
                "scene" => _collector.GetSceneTree(root),
                "test" => _testRunner?.handleTestCommand(root)
                    ?? Respond(false, "test runner not available"),
                "test_result" => _testRunner?.handleTestResultCommand()
                    ?? Respond(false, "test runner not available"),
                _ => Respond(false, $"unknown command: {cmd}")
            };
        }
        catch (Exception e)
        {
            return Respond(false, e.Message);
        }
    }

    public static string Respond(bool ok, object? data = null)
    {
        return JsonSerializer.Serialize(new { ok, data });
    }

    public void Shutdown()
    {
        foreach (var client in _clients)
            client.Close();
        _clients.Clear();
        _tcp.Stop();
    }
}
