# ItemFilterLibInspector Plugin

## Overview

The **ItemFilterLibInspector** is a Path of Exile plugin that provides comprehensive real-time monitoring and inspection of items across multiple game systems. This plugin integrates with the ItemFilterLibrary to offer detailed analysis and visualization of items from various sources within the game.

## Purpose

This plugin serves as a debugging and analysis tool for:

- **Item Filter Development**: Test how items are processed and categorized by filters
- **Market Research**: Monitor shop inventories and available items from traders
- **Quest Planning**: Track available quest rewards and ritual items
- **Inventory Management**: Get detailed views of all items in different locations
- **Data Analysis**: Inspect item properties, paths, and metadata for development purposes

## Features

### Real-time Item Monitoring
The plugin continuously monitors and displays items from:

- **Player Equipment**: All currently equipped items (weapons, armor, jewelry, etc.)
- **Inventory**: Items in your main character inventory
- **Cursor**: Items currently held by your cursor
- **Stash**: Items in the currently visible stash tab
- **Quest Rewards**: Available rewards from quest NPCs
- **Ritual Items**: Items available during ritual encounters
- **Trader/Shop Items**: Items available from vendor NPCs organized by tabs

### Interactive Interface
- Clean, organized ImGui window with collapsible sections
- Real-time item counts for each category
- Hover tooltips showing item file paths
- Expandable trader tabs showing items by vendor inventory sections
- Item snapshot functionality for detailed analysis

## Settings & Controls

### Hotkey Settings

#### Toggle Inspector Window
- **Setting**: `ToggleInspectorWindow`
- **Default**: Not assigned (Keys.None)
- **Purpose**: Opens/closes the main item inspection window
- **Usage**: 
  - Set this to any convenient key combination
  - Press the assigned key to show/hide the item collection window
  - The window will display all monitored item categories

#### Save Item Snapshot
- **Setting**: `SaveItemSnapshot` 
- **Default**: NumPad5
- **Purpose**: Captures detailed data for the currently hovered item
- **Usage**:
  - Hover your mouse over any item in the game
  - Press NumPad5 (or your assigned key) to save a snapshot
  - The captured item will appear in the "Saved Snapshot Item" section
  - Shows item name, file path, and rarity information
  - Useful for analyzing specific items in detail

#### Enable Plugin
- **Setting**: `Enable`
- **Default**: False
- **Purpose**: Master toggle for the entire plugin
- **Usage**: Must be enabled for the plugin to function

## How to Use

### Basic Operation
1. **Enable the Plugin**: Set `Enable` to `true` in plugin settings
2. **Assign Hotkeys**: Configure comfortable keys for `ToggleInspectorWindow` and `SaveItemSnapshot`
3. **Open Inspector**: Press your assigned toggle key to open the item collection window

### Monitoring Items
Once the inspector window is open, you'll see organized sections:

- **Player Items (X)**: Shows count and list of equipped items
- **Inventory Items (X)**: Your character's inventory contents
- **Cursor Items (X)**: Items you're currently moving/holding
- **Stash Items (X)**: Contents of the currently visible stash tab
- **Quest Reward Items (X)**: Available quest rewards (when quest window is open)
- **Ritual Items (X)**: Ritual encounter rewards (during rituals)
- **Trader Tabs (X)**: Vendor inventories (when shop windows are open)

### Item Analysis
- **Hover Information**: Hover over any item in the list to see its file path
- **Snapshot Feature**: Use the Save Item Snapshot hotkey while hovering over items for detailed analysis
- **Real-time Updates**: All information updates automatically as you play

### Trader/Shop Monitoring
When visiting NPCs or using vendor panels:
- Trader tabs automatically populate with available items
- Each tab shows item count and can be expanded to view contents
- Items are categorized by their source (Shop, Quest Reward, Ritual)

## Use Cases

### For Item Filter Developers
- Monitor how items are classified across different game situations
- Test filter rules against real item data
- Analyze item paths and properties for filter creation

### For Traders/Market Analysis
- Track vendor inventories and available items
- Monitor quest rewards across different characters/areas
- Analyze item availability patterns

### For General Players
- Better inventory management with detailed item views
- Quest reward planning and comparison
- Understanding item properties and metadata

## Technical Notes

### Performance
- Uses smart caching (1-second intervals) to minimize performance impact
- Only processes data when the inspector window is active
- Implements comprehensive error handling to prevent game crashes

### Compatibility
- Designed for Path of Exile with ExileCore framework
- Integrates with ItemFilterLibrary for enhanced item analysis
- Supports all standard item types and locations

### Error Handling
- Graceful handling of game state changes
- Automatic recovery from memory access errors
- Clear error messages when issues occur

## Troubleshooting

If you encounter issues:
1. Ensure the plugin is enabled in settings
2. Check that hotkeys are properly assigned and not conflicting
3. Verify ExileCore and ItemFilterLibrary are properly installed
4. Check the ExileCore logs for specific error messages

## Tips

- Keep the inspector window open while trading to monitor vendor inventories
- Use the snapshot feature to save interesting items for later analysis
- The plugin works best when combined with item filters for comprehensive item management
- Consider using it during league starts to analyze early-game item availability

---

*This plugin is designed for educational and analytical purposes to better understand Path of Exile's item systems and support filter development.* 