using Godot;
using EasyTestBridge.Demo.Tests;

namespace EasyTestBridge.Demo;

/// <summary>
/// Autoload that registers demo test suites with EasyTestBridge.
/// </summary>
public partial class DemoTestSuiteRegistrar : Node
{
    public override void _Ready()
    {
        var bridge = GetNode<global::EasyTestBridge.EasyTestBridge>("/root/EasyTestBridge");
        bridge.TestRunner.registerSuite(new DemoPlayerTests());
        bridge.TestRunner.registerSuite(new DemoCounterTests());
    }
}
