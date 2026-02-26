using Godot;
using System.Text.Json;
using EasyTestBridge.Core;

namespace EasyTestBridge.Demo;

/// <summary>
/// Demo player node — showcases Registry.Register and RegisterAction.
/// </summary>
public partial class DemoPlayer : Node2D
{
    private const float MOVE_SPEED = 100.0f;

    public int Health { get; set; } = 100;
    public int Score { get; set; } = 0;
    public bool IsAlive => Health > 0;

    public override void _Ready()
    {
        // Register this node with EasyTestBridge
        var bridge = GetNode<global::EasyTestBridge.EasyTestBridge>("/root/EasyTestBridge");
        bridge.Registry.Register("player", this);

        bridge.Registry.RegisterAction("reset_player", _ =>
        {
            Health = 100;
            Score = 0;
            Position = Vector2.Zero;
            return TestBridgeServer.Respond(true, "player reset");
        });

        bridge.Registry.RegisterAction("damage_player", data =>
        {
            var amount = 10;
            if (data.ValueKind != JsonValueKind.Undefined
                && data.TryGetProperty("amount", out var amountProp))
                amount = amountProp.GetInt32();
            TakeDamage(amount);
            return TestBridgeServer.Respond(true, $"dealt {amount} damage");
        });
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public void AddScore(int points)
    {
        Score += points;
    }
}
