using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ItemFilterLibrary;
using ImGuiNET;
using System.Numerics;

namespace ItemFilterLibInspector;

public class ItemFilterLibInspector : BaseSettingsPlugin<ItemFilterLibInspectorSettings>
{
    private readonly CachedValue<List<ItemData>> _cursorItems;

    private readonly InventorySlotE[] _cursorSlot = [InventorySlotE.Cursor1];
    private readonly InventorySlotE[] _inventorySlot = [InventorySlotE.MainInventory1];
    private readonly CachedValue<List<ItemData>> _invItems;
    private readonly CachedValue<List<ItemData>> _playerItems;

    private readonly InventorySlotE[] _playerItemSlots =
    [
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
        InventorySlotE.Ring2,
        InventorySlotE.Belt1
    ];

    private readonly CachedValue<List<CustomNPCItemData>> _rewardItems;
    private readonly CachedValue<List<CustomNPCItemData>> _ritualItems;
    private readonly CachedValue<List<ItemData>> _stashItems;
    private readonly CachedValue<List<TraderTab>> _storedTraderTabs;

    private PurchaseWindow _purchaseWindow;
    private PurchaseWindow _purchaseWindowHideout;
    private bool _showItemCollection;
    private ItemData _storedHoverItem;

    public ItemFilterLibInspector()
    {
        Name = "IFL Inspector";
        
        // Initialize caches with proper error handling
        _storedTraderTabs = new TimeCache<List<TraderTab>>(() => 
        {
            if (!_showItemCollection) return [];
            try 
            { 
                return CacheUtils.RememberLastValue<List<TraderTab>>(UpdateCurrentTraderTabs)(); 
            }
            catch (Exception ex)
            {
                LogError($"Error updating trader tabs: {ex.Message}");
                return [];
            }
        }, 1000);
        
        _rewardItems = new TimeCache<List<CustomNPCItemData>>(() => 
        {
            if (!_showItemCollection) return [];
            try { return GetRewardItems(); }
            catch (Exception ex)
            {
                LogError($"Error getting reward items: {ex.Message}");
                return [];
            }
        }, 1000);
        
        _ritualItems = new TimeCache<List<CustomNPCItemData>>(() => 
        {
            if (!_showItemCollection) return [];
            try { return GetRitualItems(); }
            catch (Exception ex)
            {
                LogError($"Error getting ritual items: {ex.Message}");
                return [];
            }
        }, 1000);
        
        _stashItems = new TimeCache<List<ItemData>>(() => 
        {
            if (!_showItemCollection) return [];
            try { return GetVisibleStashItems(); }
            catch (Exception ex)
            {
                LogError($"Error getting stash items: {ex.Message}");
                return [];
            }
        }, 1000);
        
        _invItems = new TimeCache<List<ItemData>>(() => 
        {
            if (!_showItemCollection) return [];
            try { return GetInventoryItems(); }
            catch (Exception ex)
            {
                LogError($"Error getting inventory items: {ex.Message}");
                return [];
            }
        }, 1000);
        
        _playerItems = new TimeCache<List<ItemData>>(() => 
        {
            if (!_showItemCollection) return [];
            try { return GetPlayerItems(); }
            catch (Exception ex)
            {
                LogError($"Error getting player items: {ex.Message}");
                return [];
            }
        }, 1000);
        
        _cursorItems = new TimeCache<List<ItemData>>(() => 
        {
            if (!_showItemCollection) return [];
            try { return GetCursorItems(); }
            catch (Exception ex)
            {
                LogError($"Error getting cursor items: {ex.Message}");
                return [];
            }
        }, 1000);
    }

    private Element UIHoverWithFallback =>
        GameController?.IngameState?.UIHover switch 
        { 
            null or { Address: 0 } => GameController?.IngameState?.UIHoverElement, 
            var s => s 
        };

    public override bool Initialise()
    {
        try
        {
            if (Settings?.ToggleInspectorWindow?.Value != null)
                Input.RegisterKey(Settings.ToggleInspectorWindow.Value);
            
            if (Settings?.SaveItemSnapshot?.Value != null)
                Input.RegisterKey(Settings.SaveItemSnapshot.Value);
            
            if (Settings?.ToggleInspectorWindow != null)
                Settings.ToggleInspectorWindow.OnValueChanged += () => 
                { 
                    if (Settings?.ToggleInspectorWindow?.Value != null)
                        Input.RegisterKey(Settings.ToggleInspectorWindow.Value); 
                };
            
            if (Settings?.SaveItemSnapshot != null)
                Settings.SaveItemSnapshot.OnValueChanged += () => 
                { 
                    if (Settings?.SaveItemSnapshot?.Value != null)
                        Input.RegisterKey(Settings.SaveItemSnapshot.Value); 
                };

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize plugin: {ex.Message}");
            return false;
        }
    }

    public override void AreaChange(AreaInstance area)
    {
        // Clear cached data when changing areas to prevent stale data
        try
        {
            _storedHoverItem = null;
        }
        catch (Exception ex)
        {
            LogError($"Error during area change: {ex.Message}");
        }
    }

    public override Job Tick()
    {
        try
        {
            if (Settings?.ToggleInspectorWindow?.PressedOnce() == true)
                _showItemCollection = !_showItemCollection;

            // Safely access purchase windows with null checks
            if (GameController?.Game?.IngameState?.IngameUi != null)
            {
                _purchaseWindowHideout = GameController.Game.IngameState.IngameUi.PurchaseWindowHideout;
                _purchaseWindow = GameController.Game.IngameState.IngameUi.PurchaseWindow;
            }

            if (Settings?.SaveItemSnapshot?.PressedOnce() == true)
                GetHoveredItem();
        }
        catch (Exception ex)
        {
            LogError($"Error in Tick: {ex.Message}");
        }
        
        return null;
    }

    public override void Render()
    {
        try
        {
            if (!_showItemCollection)
                return;

            // Create ImGui window instead of using InspectObject
            var windowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            
            if (ImGui.Begin("IFL Inspector - Item Collection", ref _showItemCollection, windowFlags))
            {
                try
                {
                    RenderItemCollections();
                }
                catch (Exception ex)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error rendering collections: {ex.Message}");
                    LogError($"Error rendering item collections: {ex.Message}");
                }
            }
            ImGui.End();
        }
        catch (Exception ex)
        {
            LogError($"Error in Render: {ex.Message}");
        }
    }

    private void RenderItemCollections()
    {
        // Player Items
        if (ImGui.CollapsingHeader($"Player Items ({_playerItems.Value?.Count ?? 0})"))
        {
            RenderItemList(_playerItems.Value);
        }

        // Inventory Items
        if (ImGui.CollapsingHeader($"Inventory Items ({_invItems.Value?.Count ?? 0})"))
        {
            RenderItemList(_invItems.Value);
        }

        // Cursor Items
        if (ImGui.CollapsingHeader($"Cursor Items ({_cursorItems.Value?.Count ?? 0})"))
        {
            RenderItemList(_cursorItems.Value);
        }

        // Stash Items
        if (ImGui.CollapsingHeader($"Stash Items ({_stashItems.Value?.Count ?? 0})"))
        {
            RenderItemList(_stashItems.Value);
        }

        // Quest Reward Items
        if (ImGui.CollapsingHeader($"Quest Reward Items ({_rewardItems.Value?.Count ?? 0})"))
        {
            RenderNPCItemList(_rewardItems.Value);
        }

        // Ritual Items
        if (ImGui.CollapsingHeader($"Ritual Items ({_ritualItems.Value?.Count ?? 0})"))
        {
            RenderNPCItemList(_ritualItems.Value);
        }

        // Trader Items
        if (ImGui.CollapsingHeader($"Trader Tabs ({_storedTraderTabs.Value?.Count ?? 0})"))
        {
            var traderTabs = _storedTraderTabs.Value;
            if (traderTabs != null)
            {
                for (int i = 0; i < traderTabs.Count; i++)
                {
                    if (ImGui.TreeNode($"Tab {i + 1} ({traderTabs[i]?.Items?.Count ?? 0} items)"))
                    {
                        RenderNPCItemList(traderTabs[i]?.Items);
                        ImGui.TreePop();
                    }
                }
            }
        }

        // Hovered Snapshot Item
        if (_storedHoverItem != null)
        {
            if (ImGui.CollapsingHeader("Saved Snapshot Item"))
            {
                ImGui.Text($"Name: {_storedHoverItem.BaseName ?? "Unknown"}");
                ImGui.Text($"Path: {_storedHoverItem.Path ?? "Unknown"}");
                ImGui.Text($"Rarity: {_storedHoverItem.Rarity}");
            }
        }
    }

    private void RenderItemList(List<ItemData> items)
    {
        if (items == null || items.Count == 0)
        {
            ImGui.Text("No items");
            return;
        }

        foreach (var item in items)
        {
            try
            {
                ImGui.Text($"• {item?.BaseName ?? "Unknown"} ({item?.Rarity ?? ItemRarity.Normal})");
                if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(item?.Path))
                {
                    ImGui.SetTooltip($"Path: {item.Path}");
                }
            }
            catch (Exception ex)
            {
                ImGui.Text($"• Error reading item: {ex.Message}");
            }
        }
    }

    private void RenderNPCItemList(List<CustomNPCItemData> items)
    {
        if (items == null || items.Count == 0)
        {
            ImGui.Text("No items");
            return;
        }

        foreach (var item in items)
        {
            try
            {
                ImGui.Text($"• {item?.BaseName ?? "Unknown"} ({item?.Rarity ?? ItemRarity.Normal}) [{item?.Kind ?? EKind.Shop}]");
                if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(item?.Path))
                {
                    ImGui.SetTooltip($"Path: {item.Path}");
                }
            }
            catch (Exception ex)
            {
                ImGui.Text($"• Error reading item: {ex.Message}");
            }
        }
    }

    private void GetHoveredItem()
    {
        try
        {
            var ingameStateUiHover = UIHoverWithFallback;
            if (ingameStateUiHover == null)
                return;

            var hoverItemIcon = ingameStateUiHover.AsObject<HoverItemIcon>();
            if (hoverItemIcon?.Item != null && IsValidItem(hoverItemIcon.Item, e => e, e => e.Address))
            {
                _storedHoverItem = new ItemData(hoverItemIcon.Item, GameController);
            }
        }
        catch (Exception ex)
        {
            LogError($"Error getting hovered item: {ex.Message}");
        }
    }

    private List<CustomNPCItemData> GetRewardItems()
    {
        try
        {
            if (GameController?.IngameState?.IngameUi?.QuestRewardWindow == null)
                return [];

            return GameController.IngameState.IngameUi.QuestRewardWindow.GetPossibleRewards()
                .Where(x => x.Item1 != null && IsValidItem(x.Item1, e => e, e => e.Address))
                .Select(item => new CustomNPCItemData(item.Item1, GameController, EKind.QuestReward, item.Item2?.GetClientRectCache ?? default))
                .ToList();
        }
        catch (Exception ex)
        {
            LogError($"Error getting reward items: {ex.Message}");
            return [];
        }
    }

    private List<TraderTab> UpdateCurrentTraderTabs(List<TraderTab> previousValue)
    {
        try
        {
            var purchaseWindowItems = (_purchaseWindowHideout, _purchaseWindow) switch
            {
                ({ IsVisible: true } w, _) => w,
                (_, { IsVisible: true } w) => w,
                _ => null
            };

            if (purchaseWindowItems?.TabContainer?.Inventories == null)
                return [];

            return purchaseWindowItems.TabContainer.Inventories.Select(
                    (uiInventory, i) =>
                    {
                        try
                        {
                            var serverInventory = uiInventory?.Inventory?.ServerInventory;
                            if (serverInventory == null)
                                LogMessage($"Server inventory for ui inventory {uiInventory} " + $"({uiInventory?.Inventory?.InvType}) is missing");

                            var visibleValidUiItems = uiInventory?.Inventory?.VisibleInventoryItems?
                                .Where(x => x?.Item?.Path != null && IsValidItem(x, item => item.Item, item => item.Address))
                                .ToList() ?? new List<NormalInventoryItem>();

                            var items = new List<CustomNPCItemData>();

                            if (serverInventory?.Items != null)
                                items.AddRange(
                                    serverInventory.Items.Where(x => x?.Path != null && IsValidItem(x, item => item, item => item.Address))
                                        .Select(x => new CustomNPCItemData(x, GameController, EKind.Shop)));

                            items.AddRange(visibleValidUiItems.Select(x => new CustomNPCItemData(x.Item, GameController, EKind.Shop, x.GetClientRectCache)));

                            return new TraderTab
                            {
                                Items = items
                            };
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error processing trader tab {i}: {ex.Message}");
                            return new TraderTab { Items = [] };
                        }
                    })
                .ToList();
        }
        catch (Exception ex)
        {
            LogError($"Error updating trader tabs: {ex.Message}");
            return [];
        }
    }

    private List<CustomNPCItemData> GetRitualItems()
    {
        try
        {
            if (GameController?.IngameState?.IngameUi?.RitualWindow?.IsVisible != true)
                return [];

            return GameController.IngameState.IngameUi.RitualWindow.InventoryElement?.VisibleInventoryItems?
                .Where(x => IsValidItem(x, item => item.Item, item => item.Address))
                .Select(x => new CustomNPCItemData(x.Item, GameController, EKind.RitualReward, x.GetClientRectCache))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            LogError($"Error getting ritual items: {ex.Message}");
            return [];
        }
    }

    private List<ItemData> GetItemsFromSlots(IEnumerable<InventorySlotE> slots)
    {
        try
        {
            var serverData = GameController?.Game?.IngameState?.Data?.ServerData;
            if (serverData?.PlayerInventories == null)
                return [];

            return slots.SelectMany(slot => 
                {
                    try
                    {
                        var inventory = serverData.PlayerInventories[(int)slot]?.Inventory;
                        return inventory?.InventorySlotItems ?? [];
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error accessing inventory slot {slot}: {ex.Message}");
                        return [];
                    }
                })
                .Where(invItem => invItem != null && IsValidItem(invItem, item => item.Item, item => item.Address))
                .Select(invItem => new ItemData(invItem.Item, GameController))
                .ToList();
        }
        catch (Exception ex)
        {
            LogError($"Error getting items from slots: {ex.Message}");
            return [];
        }
    }

    public List<ItemData> GetInventoryItems()
    {
        return GetItemsFromSlots(_inventorySlot);
    }

    public List<ItemData> GetCursorItems()
    {
        return GetItemsFromSlots(_cursorSlot);
    }

    public List<ItemData> GetPlayerItems()
    {
        return GetItemsFromSlots(_playerItemSlots);
    }

    public List<ItemData> GetVisibleStashItems()
    {
        try
        {
            if (!TryGetVisibleStashInventory(out var stashContents))
                return [];

            return stashContents.Where(x => x != null && IsValidItem(x, item => item.Item, item => item.Address))
                .Select(x => new ItemData(x.Item, GameController))
                .ToList();
        }
        catch (Exception ex)
        {
            LogError($"Error getting visible stash items: {ex.Message}");
            return [];
        }
    }

    public InventoryType GetTypeOfCurrentVisibleStash()
    {
        try
        {
            return GameController?.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash?.InvType ?? InventoryType.InvalidInventory;
        }
        catch (Exception ex)
        {
            LogError($"Error getting stash type: {ex.Message}");
            return InventoryType.InvalidInventory;
        }
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
        try
        {
            return GameController?.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash?.VisibleInventoryItems?.ToList();
        }
        catch (Exception ex)
        {
            LogError($"Error getting visible stash inventory: {ex.Message}");
            return null;
        }
    }

    private static bool IsValidItem<T>(T item, Func<T, Entity> getEntity, Func<T, long> getAddress)
    {
        try
        {
            if (item == null)
                return false;

            var entity = getEntity(item);
            if (entity?.IsValid != true)
                return false;

            // Fixed: Check for different component types instead of Base twice
            return entity.TryGetComponent<Base>(out var baseComp) &&
                   baseComp?.Address != 0 &&
                   entity.TryGetComponent<Mods>(out var modComp) &&
                   modComp?.Address != 0 &&
                   getAddress(item) != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}