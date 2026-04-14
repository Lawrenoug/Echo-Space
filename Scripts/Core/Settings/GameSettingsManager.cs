using EchoSpace.Core.Input;
using EchoSpace.Player;
using Godot;

namespace EchoSpace.Core.Settings;

public enum DisplaySettingsPreset
{
	WindowedVSync,
	WindowedUnlocked,
	FullscreenVSync,
	FullscreenUnlocked,
}

public enum InputSettingsPreset
{
	MouseCombat,
	MouseAndKeyboardCombat,
}

public enum GameplaySettingsPreset
{
	Standard,
	Forgiving,
	Assisted,
}

public partial class GameSettingsManager : Node
{
	private const string SettingsPath = "user://settings.cfg";
	private static readonly float[] MasterVolumeLevels = [1f, 0.8f, 0.6f, 0.4f, 0.2f, 0f];

	public static GameSettingsManager? Instance { get; private set; }

	public DisplaySettingsPreset DisplayPreset { get; private set; } = DisplaySettingsPreset.WindowedVSync;
	public int MasterVolumeIndex { get; private set; }
	public InputSettingsPreset InputPreset { get; private set; } = InputSettingsPreset.MouseCombat;
	public GameplaySettingsPreset GameplayPreset { get; private set; } = GameplaySettingsPreset.Standard;

	public float MasterVolume => MasterVolumeLevels[MasterVolumeIndex];
	public string DisplayLabel => DisplayPreset switch
	{
		DisplaySettingsPreset.WindowedVSync => "窗口模式 / 垂直同步开启",
		DisplaySettingsPreset.WindowedUnlocked => "窗口模式 / 垂直同步关闭",
		DisplaySettingsPreset.FullscreenVSync => "全屏模式 / 垂直同步开启",
		DisplaySettingsPreset.FullscreenUnlocked => "全屏模式 / 垂直同步关闭",
		_ => "未知显示预设",
	};

	public string AudioLabel => $"主音量 {Mathf.RoundToInt(MasterVolume * 100f)}%";

	public string InputLabel => InputPreset switch
	{
		InputSettingsPreset.MouseCombat => "默认：左键攻击，右键防反",
		InputSettingsPreset.MouseAndKeyboardCombat => "扩展：鼠标战斗 + J 攻击 / K 防反",
		_ => "未知按键预设",
	};

	public string GameplayLabel => GameplayPreset switch
	{
		GameplaySettingsPreset.Standard => "标准手感：默认输入缓冲、土狼时间、弹反窗口",
		GameplaySettingsPreset.Forgiving => "宽松手感：略微增加输入缓冲、土狼时间、弹反窗口",
		GameplaySettingsPreset.Assisted => "辅助手感：明显增加输入缓冲、土狼时间、弹反窗口",
		_ => "未知玩法预设",
	};

	public override void _Ready()
	{
		Instance = this;
		Load();
		ApplyAll();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void CycleDisplayPreset()
	{
		DisplayPreset = (DisplaySettingsPreset)NextIndex((int)DisplayPreset, 4);
		ApplyDisplaySettings();
		Save();
	}

	public void CycleAudioPreset()
	{
		MasterVolumeIndex = NextIndex(MasterVolumeIndex, MasterVolumeLevels.Length);
		ApplyAudioSettings();
		Save();
	}

	public void CycleInputPreset()
	{
		InputPreset = (InputSettingsPreset)NextIndex((int)InputPreset, 2);
		ApplyInputSettings();
		Save();
	}

	public void CycleGameplayPreset()
	{
		GameplayPreset = (GameplaySettingsPreset)NextIndex((int)GameplayPreset, 3);
		Save();
	}

	public void ApplyAll()
	{
		ApplyDisplaySettings();
		ApplyAudioSettings();
		ApplyInputSettings();
	}

	public void ApplyGameplaySettings(PlayerController player)
	{
		switch (GameplayPreset)
		{
			case GameplaySettingsPreset.Forgiving:
				player.InputBufferTime = 0.15f;
				player.CoyoteTime = 0.12f;
				player.GuardDeflectWindow = 0.22f;
				break;
			case GameplaySettingsPreset.Assisted:
				player.InputBufferTime = 0.18f;
				player.CoyoteTime = 0.15f;
				player.GuardDeflectWindow = 0.28f;
				break;
			default:
				player.InputBufferTime = 0.12f;
				player.CoyoteTime = 0.10f;
				player.GuardDeflectWindow = 0.18f;
				break;
		}
	}

	public string BuildSummary()
	{
		return
			"[b]当前设置[/b]\n\n" +
			$"显示：{DisplayLabel}\n" +
			$"音频：{AudioLabel}\n" +
			$"按键：{InputLabel}\n" +
			$"玩法：{GameplayLabel}\n\n" +
			"点击左侧分类按钮会循环切换对应预设，并立即应用到当前项目。";
	}

	private void ApplyDisplaySettings()
	{
		var fullscreen = DisplayPreset is DisplaySettingsPreset.FullscreenVSync or DisplaySettingsPreset.FullscreenUnlocked;
		var vsync = DisplayPreset is DisplaySettingsPreset.WindowedVSync or DisplaySettingsPreset.FullscreenVSync;

		DisplayServer.WindowSetMode(fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

		if (!fullscreen)
		{
			DisplayServer.WindowSetSize(new Vector2I(1920, 1080));
		}
	}

	private void ApplyAudioSettings()
	{
		ApplyBusVolume("Master", MasterVolume);
		ApplyBusVolume("Music", MasterVolume);
		ApplyBusVolume("BGM", MasterVolume);
		ApplyBusVolume("SFX", MasterVolume);
	}

	private static void ApplyInputSettings()
	{
		GameInputActions.ApplyBindingPreset(Instance?.InputPreset == InputSettingsPreset.MouseAndKeyboardCombat);
	}

	private static void ApplyBusVolume(string busName, float linearVolume)
	{
		var busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
		{
			return;
		}

		AudioServer.SetBusMute(busIndex, linearVolume <= 0.001f);
		AudioServer.SetBusVolumeDb(busIndex, linearVolume <= 0.001f ? -80f : Mathf.LinearToDb(linearVolume));
	}

	private void Load()
	{
		var config = new ConfigFile();
		if (config.Load(SettingsPath) != Error.Ok)
		{
			Save();
			return;
		}

		DisplayPreset = (DisplaySettingsPreset)ClampIndex(config.GetValue("display", "preset", (int)DisplayPreset).AsInt32(), 4);
		MasterVolumeIndex = ClampIndex(config.GetValue("audio", "master_volume_index", MasterVolumeIndex).AsInt32(), MasterVolumeLevels.Length);
		InputPreset = (InputSettingsPreset)ClampIndex(config.GetValue("input", "preset", (int)InputPreset).AsInt32(), 2);
		GameplayPreset = (GameplaySettingsPreset)ClampIndex(config.GetValue("gameplay", "preset", (int)GameplayPreset).AsInt32(), 3);
	}

	private void Save()
	{
		var config = new ConfigFile();
		config.SetValue("display", "preset", (int)DisplayPreset);
		config.SetValue("audio", "master_volume_index", MasterVolumeIndex);
		config.SetValue("input", "preset", (int)InputPreset);
		config.SetValue("gameplay", "preset", (int)GameplayPreset);

		var result = config.Save(SettingsPath);
		if (result != Error.Ok)
		{
			GD.PushWarning($"Settings save failed: {result}");
		}
	}

	private static int NextIndex(int currentIndex, int count)
	{
		return count <= 0 ? 0 : (currentIndex + 1) % count;
	}

	private static int ClampIndex(int value, int count)
	{
		return count <= 0 ? 0 : Mathf.Clamp(value, 0, count - 1);
	}
}
