using EasyTestBridge.Testing;

namespace EasyTestBridge.Demo.Tests;

/// <summary>
/// Tests for DemoCounter — demonstrates pure unit testing.
/// </summary>
public class DemoCounterTests
{
    [GameTest(Tag = "counter")]
    public void Counter_StartsAtZero()
    {
        var counter = new DemoCounter();
        TestAssert.areEqual(0, counter.Value);
    }

    [GameTest(Tag = "counter")]
    public void Counter_Increment()
    {
        var counter = new DemoCounter();
        counter.Increment();
        counter.Increment();
        TestAssert.areEqual(2, counter.Value);
    }

    [GameTest(Tag = "counter")]
    public void Counter_Decrement()
    {
        var counter = new DemoCounter();
        counter.Increment();
        counter.Increment();
        counter.Decrement();
        TestAssert.areEqual(1, counter.Value);
    }

    [GameTest(Tag = "counter")]
    public void Counter_Add()
    {
        var counter = new DemoCounter();
        counter.Add(42);
        TestAssert.areEqual(42, counter.Value);
    }

    [GameTest(Tag = "counter")]
    public void Counter_Reset()
    {
        var counter = new DemoCounter();
        counter.Add(99);
        counter.Reset();
        TestAssert.areEqual(0, counter.Value);
    }
}
