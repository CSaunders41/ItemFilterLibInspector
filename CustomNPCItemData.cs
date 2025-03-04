using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using SharpDX;

namespace ItemFilterLibInspector;

public class CustomNPCItemData(Entity queriedItem, GameController gc, EKind kind, RectangleF clientRect = default) : ItemData(queriedItem, gc)
{
    public RectangleF ClientRectangle { get; set; } = clientRect;
    public EKind Kind { get; } = kind;
}

public enum EKind
{
    QuestReward,
    Shop,
    RitualReward
}