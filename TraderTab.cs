using System.Collections.Generic;

namespace ItemFilterLibInspector;

public class TraderTab
{
    public List<CustomNPCItemData> Items { get; set; }

    public override string ToString()
    {
        return $"{Items?.Count ?? 0}";
    }
}