using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ItemFilterLibrary;

namespace ItemFilterLibInspector;

public class TraderTab
{
    public List<CustomNPCItemData> Items { get; set; }

    public override string ToString()
    {
        return $"{Items?.Count ?? 0}";
    }
}

public class ItemContainer
{
    public List<ItemData> PlayerItems { get; set; }
    public List<ItemData> InventoryItems { get; set; }
    public List<CustomNPCItemData> RewardItems { get; set; }
    public List<CustomNPCItemData> RitualItems { get; set; }
    public List<ItemData> StashItems { get; set; }
    public List<TraderTab> TraderTabs { get; set; }
}

public class ItemFilterLibInspector : BaseSettingsPlugin<ItemFilterLibInspectorSettings>
{
    private readonly CachedValue<List<ItemData>> _invItems;
    private readonly CachedValue<List<ItemData>> _playerItems;
    private readonly CachedValue<List<CustomNPCItemData>> _rewardItems;
    private readonly CachedValue<List<CustomNPCItemData>> _ritualItems;
    private readonly CachedValue<List<ItemData>> _stashItems;
    private readonly CachedValue<List<TraderTab>> _storedTraderTabs;

    private PurchaseWindow _purchaseWindow;
    private PurchaseWindow _purchaseWindowHideout;

    public ItemFilterLibInspector()
    {
        Name = "IFL Inspector";
        _storedTraderTabs = new TimeCache<List<TraderTab>>(CacheUtils.RememberLastValue<List<TraderTab>>(UpdateCurrentTraderTabs), 1000);
        _rewardItems = new TimeCache<List<CustomNPCItemData>>(GetRewardItems, 1000);
        _ritualItems = new TimeCache<List<CustomNPCItemData>>(GetRitualItems, 1000);
        _stashItems = new TimeCache<List<ItemData>>(GetVisibleStashItems, 1000);
        _invItems = new TimeCache<List<ItemData>>(GetInventoryItems, 1000);
        _playerItems = new TimeCache<List<ItemData>>(GetPlayerItems, 1000);
    }

    public override bool Initialise()
    {
        Input.RegisterKey(Settings.Hotkey.Value);
        Settings.Hotkey.OnValueChanged += () => { Input.RegisterKey(Settings.Hotkey.Value); };

        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
    }

    public override Job Tick()
    {
        if (Settings.Hotkey.PressedOnce())
            Settings.ShowItemCollection.Value = !Settings.ShowItemCollection.Value;

        _purchaseWindowHideout = GameController.Game.IngameState.IngameUi.PurchaseWindowHideout;
        _purchaseWindow = GameController.Game.IngameState.IngameUi.PurchaseWindow;
        return null;
    }

    public override void Render()
    {
        if (!Settings.ShowItemCollection)
            return;

        var aggregateContainer = new ItemContainer
        {
            PlayerItems = _playerItems.Value,
            InventoryItems = _invItems.Value,
            RewardItems = _rewardItems.Value,
            RitualItems = _ritualItems.Value,
            StashItems = _stashItems.Value,
            TraderTabs = _storedTraderTabs.Value
        };

        GameController.InspectObject(aggregateContainer, "Collection of IFL Items");
    }

    private List<CustomNPCItemData> GetRewardItems()
    {
        return GameController.IngameState.IngameUi.QuestRewardWindow.GetPossibleRewards()
            .Where(x => x.Item1 != null && IsValidItem(x.Item1, e => e, e => e.Address))
            .Select(item => new CustomNPCItemData(item.Item1, GameController, EKind.QuestReward, item.Item2.GetClientRectCache))
            .ToList();
    }

    private List<TraderTab> UpdateCurrentTraderTabs(List<TraderTab> previousValue)
    {
        var purchaseWindowItems = (_purchaseWindowHideout, _purchaseWindow) switch
        {
            ({ IsVisible: true } w, _) => w,
            (_, { IsVisible: true } w) => w,
            _ => null
        };

        if (purchaseWindowItems == null)
            return [];

        return purchaseWindowItems.TabContainer.Inventories.Select(
                (uiInventory, i) =>
                {
                    var serverInventory = uiInventory.Inventory.ServerInventory;
                    if (serverInventory == null)
                        DebugWindow.LogError($"Server inventory for ui inventory {uiInventory} " + $"({uiInventory.Inventory.InvType}) is missing");

                    var visibleValidUiItems = uiInventory.Inventory.VisibleInventoryItems
                        .Where(x => x.Item?.Path != null && IsValidItem(x, item => item.Item, item => item.Address))
                        .ToList();

                    var items = new List<CustomNPCItemData>();

                    if (serverInventory != null)
                        items.AddRange(
                            serverInventory.Items?.Where(x => x?.Path != null && IsValidItem(x, item => item, item => item.Address))
                                .Select(x => new CustomNPCItemData(x, GameController, EKind.Shop)) ??
                            new List<CustomNPCItemData>());

                    items.AddRange(visibleValidUiItems.Select(x => new CustomNPCItemData(x.Item, GameController, EKind.Shop, x.GetClientRectCache)));

                    return new TraderTab
                    {
                        Items = items
                    };
                })
            .ToList();
    }

    private List<CustomNPCItemData> GetRitualItems()
    {
        if (!GameController.IngameState.IngameUi.RitualWindow.IsVisible)
            return [];

        return GameController.IngameState.IngameUi.RitualWindow.InventoryElement.VisibleInventoryItems
            .Where(x => IsValidItem(x, item => item.Item, item => item.Address))
            .Select(x => new CustomNPCItemData(x.Item, GameController, EKind.RitualReward, x.GetClientRectCache))
            .ToList();
    }

    public List<ItemData> GetInventoryItems()
    {
        var serverData = GameController.Game.IngameState.Data.ServerData;
        var invItems = serverData.PlayerInventories[0].Inventory.InventorySlotItems;
        return invItems.Where(invItem => invItem != null && IsValidItem(invItem, item => item.Item, item => item.Address))
            .Select(invItem => new ItemData(invItem.Item, GameController))
            .ToList();
    }

    public List<ItemData> GetPlayerItems()
    {
        var slots = new List<InventorySlotE>
        {
            InventorySlotE.Weapon1,
            InventorySlotE.Offhand1,
            InventorySlotE.Weapon2,
            InventorySlotE.Offhand2,
            InventorySlotE.BodyArmour1,
            InventorySlotE.Helm1,
            InventorySlotE.Gloves1,
            InventorySlotE.Boots1,
            InventorySlotE.Amulet1,
            InventorySlotE.Ring1,
            InventorySlotE.Ring2
        };

        var serverData = GameController.Game.IngameState.Data.ServerData;

        var items = slots.SelectMany(slot => serverData.PlayerInventories[(int)slot].Inventory.InventorySlotItems)
            .Where(invItem => invItem != null && IsValidItem(invItem, item => item.Item, item => item.Address))
            .Select(invItem => new ItemData(invItem.Item, GameController))
            .ToList();

        return items;
    }

    public List<ItemData> GetVisibleStashItems()
    {
        if (!TryGetVisibleStashInventory(out var stashContents))
            return [];

        return stashContents.Where(x => x != null && IsValidItem(x, item => item.Item, item => item.Address))
            .Select(x => new ItemData(x.Item, GameController))
            .ToList();
    }

    public InventoryType GetTypeOfCurrentVisibleStash()
    {
        return GameController?.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash?.InvType ?? InventoryType.InvalidInventory;
    }

    public bool IsVisibleStashValidCondition()
    {
        return GetTypeOfCurrentVisibleStash() != InventoryType.InvalidInventory;
    }

    public bool TryGetVisibleStashInventory(out IList<NormalInventoryItem> inventoryItems)
    {
        inventoryItems = IsVisibleStashValidCondition() ? GetVisibleStashInventory() : null;
        return inventoryItems != null;
    }

    public IList<NormalInventoryItem> GetVisibleStashInventory()
    {
        return GameController?.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems?.ToList();
    }

    private static bool IsValidItem<T>(T item, Func<T, Entity> getEntity, Func<T, long> getAddress)
    {
        var entity = getEntity(item);
        return entity is { IsValid: true } &&
            entity.TryGetComponent<Base>(out var baseComp) &&
            baseComp.Address != 0 &&
            entity.TryGetComponent<Base>(out var modComp) &&
            modComp.Address != 0 &&
            getAddress(item) != 0;
    }
}