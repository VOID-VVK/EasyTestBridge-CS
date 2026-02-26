#if TOOLS
using Godot;

namespace EasyTestBridge;

[Tool]
public partial class EasyTestBridgePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        GD.Print("[EasyTestBridge] Plugin loaded");
    }

    public override void _ExitTree()
    {
        GD.Print("[EasyTestBridge] Plugin unloaded");
    }
}
#endif
