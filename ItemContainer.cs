using System.Collections.Generic;
using ItemFilterLibrary;

namespace ItemFilterLibInspector;

public class ItemContainer
{
    public ItemData HoveredSnapshotItem { get; set; }
    public List<ItemData> PlayerItems { get; set; }
    public List<ItemData> InventoryItems { get; set; }
    public List<ItemData> CursorItems { get; set; }
    public List<CustomNPCItemData> RewardItems { get; set; }
    public List<CustomNPCItemData> RitualItems { get; set; }
    public List<ItemData> StashItems { get; set; }
    public List<TraderTab> TraderTabs { get; set; }
}