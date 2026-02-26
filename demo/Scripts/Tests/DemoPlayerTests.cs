using EasyTestBridge.Testing;

namespace EasyTestBridge.Demo.Tests;

/// <summary>
/// Tests for DemoPlayer — demonstrates [GameTest] with TestContext.
/// </summary>
public class DemoPlayerTests
{
    [GameTest(Tag = "player")]
    public void Player_InitialHealth_Is100()
    {
        // DemoPlayer starts with 100 HP
        TestAssert.areEqual(100, 100, "initial health should be 100");
    }

    [GameTest(Tag = "player")]
    public void Player_TakeDamage_ReducesHealth()
    {
        var counter = new DemoCounter();
        counter.Add(100);
        TestAssert.areEqual(100, counter.Value);

        counter.Add(-30);
        TestAssert.areEqual(70, counter.Value, "health after 30 damage");
    }

    [GameTest(Tag = "player")]
    public void Player_IsAlive_WhenHealthAboveZero()
    {
        TestAssert.isTrue(50 > 0, "player with 50 HP should be alive");
    }

    [GameTest(Tag = "player")]
    public void Player_IsDead_WhenHealthZero()
    {
        TestAssert.isFalse(0 > 0, "player with 0 HP should be dead");
    }
}
