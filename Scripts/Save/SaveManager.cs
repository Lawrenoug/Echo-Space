using System;
using System.Collections.Generic;
using System.Text.Json;
using EchoSpace.Core.Input;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Inventory;
using EchoSpace.Gameplay.Progression;
using EchoSpace.Player;
using Godot;

namespace EchoSpace.Save;

public partial class SaveManager : Node
{
    public static SaveManager? Instance { get; private set; }

    [Export] public string SaveFilePath { get; set; } = "user://savegame.save";
    [Export(PropertyHint.File, "*.tscn")] public string FallbackGameScenePath { get; set; } = "res://Scenes/Main.tscn";

    private SaveGameData? _pendingSaveData;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameInputActions.QuickSave))
        {
            SaveCurrentGame();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(GameInputActions.QuickLoad))
        {
            ContinueFromLatestSave();
            GetViewport().SetInputAsHandled();
        }
    }

    public bool HasSave()
    {
        return FileAccess.FileExists(SaveFilePath);
    }

    public void StartNewGame(string? scenePath = null)
    {
        ResetGlobalStateToDefaults();
        _pendingSaveData = null;
        ChangeScene(scenePath);
    }

    public void ContinueFromLatestSave(string? fallbackScenePath = null)
    {
        if (!TryLoadSaveFile(out var saveData))
        {
            GD.PushWarning("Continue requested, but no save file could be loaded.");
            return;
        }

        _pendingSaveData = saveData;
        var targetScene = string.IsNullOrWhiteSpace(saveData.ScenePath)
            ? fallbackScenePath ?? FallbackGameScenePath
            : saveData.ScenePath;
        if (ChangeScene(targetScene))
        {
            ApplyPendingSaveDeferred();
        }
    }

    public Error SaveCurrentGame()
    {
        var currentScene = GetTree().CurrentScene;
        if (currentScene == null)
        {
            return Error.DoesNotExist;
        }

        if (GetTree().GetFirstNodeInGroup("player") is not PlayerController)
        {
            GD.PushWarning("Save skipped because no active player was found in the current scene.");
            return Error.Unavailable;
        }

        var saveData = new SaveGameData
        {
            ScenePath = string.IsNullOrWhiteSpace(currentScene.SceneFilePath)
                ? FallbackGameScenePath
                : currentScene.SceneFilePath,
            CurrentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality,
            SavedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Player = BuildPlayerSaveData(),
            InventoryItems = InventoryManager.Instance?.BuildSaveData() ?? new List<InventoryItemSaveData>(),
            Progression = ProgressionManager.Instance?.BuildSaveData() ?? new ProgressionSaveData(),
            DualWorldObjects = BuildDualWorldSaveData(),
        };

        try
        {
            var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
            file.StoreString(json);
            return Error.Ok;
        }
        catch (Exception exception)
        {
            GD.PushError($"Failed to save game: {exception.Message}");
            return Error.Failed;
        }
    }

    private bool ChangeScene(string? scenePath)
    {
        var targetScene = string.IsNullOrWhiteSpace(scenePath) ? FallbackGameScenePath : scenePath;
        var result = GetTree().ChangeSceneToFile(targetScene);
        if (result != Error.Ok)
        {
            GD.PushError($"Failed to change scene to '{targetScene}': {result}");
            return false;
        }

        return true;
    }

    private async void ApplyPendingSaveDeferred()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ApplyPendingSave();
    }

    private void ApplyPendingSave()
    {
        if (_pendingSaveData == null)
        {
            return;
        }

        InventoryManager.Instance?.ApplySaveData(_pendingSaveData.InventoryItems);
        ProgressionManager.Instance?.ApplySaveData(_pendingSaveData.Progression);
        WorldManager.Instance?.SetWorld(_pendingSaveData.CurrentWorld);

        if (GetTree().GetFirstNodeInGroup("player") is PlayerController player)
        {
            player.ApplySaveData(_pendingSaveData.Player);
        }

        var stateLookup = new Dictionary<string, DualWorldObjectSaveData>(StringComparer.Ordinal);
        foreach (var entry in _pendingSaveData.DualWorldObjects)
        {
            if (!string.IsNullOrWhiteSpace(entry.SaveId))
            {
                stateLookup[entry.SaveId] = entry;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("saveable_dual_world"))
        {
            if (node is not DualWorldObject dualWorldObject || !dualWorldObject.HasSaveId)
            {
                continue;
            }

            if (stateLookup.TryGetValue(dualWorldObject.SaveId, out var saveState))
            {
                dualWorldObject.ApplySaveState(saveState);
            }
        }

        _pendingSaveData = null;
    }

    private void ResetGlobalStateToDefaults()
    {
        WorldManager.Instance?.SetWorld(WorldType.Reality);
        InventoryManager.Instance?.ResetToDefaults();
        ProgressionManager.Instance?.ResetToDefaults();
    }

    private bool TryLoadSaveFile(out SaveGameData saveData)
    {
        saveData = new SaveGameData();

        if (!HasSave())
        {
            return false;
        }

        try
        {
            using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            var deserialized = JsonSerializer.Deserialize<SaveGameData>(json);
            if (deserialized == null)
            {
                return false;
            }

            saveData = deserialized;
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"Failed to load save file: {exception.Message}");
            return false;
        }
    }

    private PlayerSaveData BuildPlayerSaveData()
    {
        if (GetTree().GetFirstNodeInGroup("player") is not PlayerController player)
        {
            return new PlayerSaveData();
        }

        return player.BuildSaveData();
    }

    private List<DualWorldObjectSaveData> BuildDualWorldSaveData()
    {
        var entries = new List<DualWorldObjectSaveData>();

        foreach (Node node in GetTree().GetNodesInGroup("saveable_dual_world"))
        {
            if (node is not DualWorldObject dualWorldObject || !dualWorldObject.HasSaveId)
            {
                continue;
            }

            entries.Add(dualWorldObject.BuildSaveData());
        }

        return entries;
    }
}
