namespace EasyTestBridge.Demo;

/// <summary>
/// Pure C# class — showcases unit testing without Godot nodes.
/// </summary>
public class DemoCounter
{
    public int Value { get; private set; }

    public void Increment() => Value++;
    public void Decrement() => Value--;
    public void Reset() => Value = 0;

    public void Add(int amount) => Value += amount;
}
