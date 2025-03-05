using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace ItemFilterLibInspector;

public class ItemFilterLibInspectorSettings : ISettings
{
    public HotkeyNodeV2 ToggleInspectorWindow { get; set; } = new(Keys.None);

    public HotkeyNodeV2 SaveItemSnapshot { get; set; } = Keys.NumPad5;
    public ToggleNode Enable { get; set; } = new(false);
}