using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace ItemFilterLibInspector;

public class ItemFilterLibInspectorSettings : ISettings
{
    public HotkeyNodeV2 Hotkey { get; set; } = new(Keys.None);
    public ToggleNode ShowItemCollection { get; set; } = new(false);
    public ToggleNode Enable { get; set; } = new(false);
}