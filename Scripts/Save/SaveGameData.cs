using System.Collections.Generic;
using EchoSpace.Core.World;

namespace EchoSpace.Save;

public sealed class SaveGameData
{
    public string ScenePath { get; set; } = "res://Scenes/Main.tscn";
    public WorldType CurrentWorld { get; set; } = WorldType.Reality;
    public long SavedAtUnixSeconds { get; set; }
    public PlayerSaveData Player { get; set; } = new();
    public List<InventoryItemSaveData> InventoryItems { get; set; } = new();
    public ProgressionSaveData Progression { get; set; } = new();
    public List<DualWorldObjectSaveData> DualWorldObjects { get; set; } = new();
}
