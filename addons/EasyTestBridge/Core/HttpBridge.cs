using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EasyTestBridge.Core;

/// <summary>
/// 简易 HTTP 服务器，将 HTTP 请求转为 JSON 命令交给 ProcessCommand 处理。
/// 支持 curl 零依赖访问：curl localhost:9877/scene?depth=5
/// </summary>
public partial class HttpBridge : Node
{
    private TcpServer _tcp = new();
    private int _port;
    private TestBridgeServer _server = null!;

    public void Setup(TestBridgeServer server, int port)
    {
        _server = server;
        _port = port;

        var err = _tcp.Listen((ushort)_port);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[HttpBridge] 启动失败: {err}");
            return;
        }
        GD.Print($"[HttpBridge] HTTP 监听 http://localhost:{_port}");
    }

    public override void _Process(double delta)
    {
        if (!_tcp.IsConnectionAvailable()) return;

        var conn = _tcp.TakeConnection();
        if (conn == null) return;

        try
        {
            // 读取 HTTP 请求（简单实现，只读第一行）
            var raw = conn.GetUtf8String(conn.GetAvailableBytes());
            if (string.IsNullOrEmpty(raw)) { conn.DisconnectFromHost(); return; }

            var firstLine = raw.Split('\n')[0].Trim();
            // GET /scene?depth=5 HTTP/1.1
            var parts = firstLine.Split(' ');
            if (parts.Length < 2) { conn.DisconnectFromHost(); return; }

            var fullPath = parts[1]; // /scene?depth=5
            var json = HttpPathToJson(fullPath);
            var result = _server.ProcessCommand(json);

            var response = BuildHttpResponse(result);
            conn.PutData(Encoding.UTF8.GetBytes(response));
        }
        catch (Exception e)
        {
            var errBody = TestBridgeServer.Respond(false, e.Message);
            var response = BuildHttpResponse(errBody, 500);
            conn.PutData(Encoding.UTF8.GetBytes(response));
        }
        finally
        {
            conn.DisconnectFromHost();
        }
    }

    /// <summary>
    /// 将 HTTP 路径+查询参数转为 JSON 命令。
    /// /scene?depth=5 → {"cmd":"scene","depth":5}
    /// /find?name=Player → {"cmd":"find","name":"Player"}
    /// /inspect?path=/root/Town/Player → {"cmd":"inspect","path":"/root/Town/Player"}
    /// </summary>
    private static string HttpPathToJson(string fullPath)
    {
        var qIndex = fullPath.IndexOf('?');
        var path = qIndex >= 0 ? fullPath[1..qIndex] : fullPath[1..]; // 去掉前导 /
        var query = qIndex >= 0 ? fullPath[(qIndex + 1)..] : "";

        var dict = new Dictionary<string, object> { ["cmd"] = path };

        if (!string.IsNullOrEmpty(query))
        {
            foreach (var pair in query.Split('&'))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length != 2) continue;
                var key = Uri.UnescapeDataString(kv[0]);
                var val = Uri.UnescapeDataString(kv[1]);

                if (int.TryParse(val, out var intVal))
                    dict[key] = intVal;
                else
                    dict[key] = val;
            }
        }

        return JsonSerializer.Serialize(dict);
    }

    private static string BuildHttpResponse(string body, int status = 200)
    {
        var statusText = status == 200 ? "OK" : "Internal Server Error";
        return $"HTTP/1.1 {status} {statusText}\r\n"
             + "Content-Type: application/json; charset=utf-8\r\n"
             + "Access-Control-Allow-Origin: *\r\n"
             + $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n"
             + "Connection: close\r\n"
             + "\r\n"
             + body;
    }

    public void Shutdown()
    {
        _tcp.Stop();
    }
}