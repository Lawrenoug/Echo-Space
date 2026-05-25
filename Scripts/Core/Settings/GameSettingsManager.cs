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
	private static readonly Vector2I[] ResolutionOptions =
	[
		new(1280, 720),
		new(1600, 900),
		new(1920, 1080),
		new(2560, 1440),
	];

	public static GameSettingsManager? Instance { get; private set; }

	public int ResolutionIndex { get; private set; } = 2;
	public bool Fullscreen { get; private set; }
	public bool VSync { get; private set; } = true;
	public float MasterVolume { get; private set; } = 0.8f;
	public float MusicVolume { get; private set; } = 0.8f;
	public float SfxVolume { get; private set; } = 0.8f;
	public bool EnableKeyboardCombatAlternative { get; private set; }
	public float InputBufferTime { get; private set; } = 0.12f;
	public float CoyoteTime { get; private set; } = 0.10f;
	public float GuardDeflectWindow { get; private set; } = 0.18f;

	public DisplaySettingsPreset DisplayPreset => (Fullscreen, VSync) switch
	{
		(false, true) => DisplaySettingsPreset.WindowedVSync,
		(false, false) => DisplaySettingsPreset.WindowedUnlocked,
		(true, true) => DisplaySettingsPreset.FullscreenVSync,
		(true, false) => DisplaySettingsPreset.FullscreenUnlocked,
	};

	public int MasterVolumeIndex => Mathf.RoundToInt((1f - MasterVolume) * 5f);
	public InputSettingsPreset InputPreset => EnableKeyboardCombatAlternative
		? InputSettingsPreset.MouseAndKeyboardCombat
		: InputSettingsPreset.MouseCombat;
	public GameplaySettingsPreset GameplayPreset => GuardDeflectWindow >= 0.27f
		? GameplaySettingsPreset.Assisted
		: GuardDeflectWindow >= 0.21f
			? GameplaySettingsPreset.Forgiving
			: GameplaySettingsPreset.Standard;

	public string DisplayLabel => $"{GetResolutionLabel(ResolutionIndex)} / {(Fullscreen ? "全屏" : "窗口")} / {(VSync ? "垂直同步开启" : "垂直同步关闭")}";
	public string AudioLabel => $"主音量 {ToPercent(MasterVolume)}%，音乐 {ToPercent(MusicVolume)}%，音效 {ToPercent(SfxVolume)}%";
	public string InputLabel => EnableKeyboardCombatAlternative
		? "鼠标战斗 + J 攻击 / K 防反"
		: "鼠标左键攻击 / 右键防反";
	public string GameplayLabel => $"输入缓冲 {InputBufferTime:0.00}s，土狼时间 {CoyoteTime:0.00}s，弹反窗口 {GuardDeflectWindow:0.00}s";

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

	public int GetResolutionCount()
	{
		return ResolutionOptions.Length;
	}

	public string GetResolutionLabel(int index)
	{
		var resolution = ResolutionOptions[ClampIndex(index, ResolutionOptions.Length)];
		return $"{resolution.X} x {resolution.Y}";
	}

	public void SetResolutionIndex(int value)
	{
		ResolutionIndex = ClampIndex(value, ResolutionOptions.Length);
		ApplyDisplaySettings();
		Save();
	}

	public void SetFullscreen(bool value)
	{
		Fullscreen = value;
		ApplyDisplaySettings();
		Save();
	}

	public void SetVSync(bool value)
	{
		VSync = value;
		ApplyDisplaySettings();
		Save();
	}

	public void SetMasterVolume(float value)
	{
		MasterVolume = Mathf.Clamp(value, 0f, 1f);
		ApplyAudioSettings();
		Save();
	}

	public void SetMusicVolume(float value)
	{
		MusicVolume = Mathf.Clamp(value, 0f, 1f);
		ApplyAudioSettings();
		Save();
	}

	public void SetSfxVolume(float value)
	{
		SfxVolume = Mathf.Clamp(value, 0f, 1f);
		ApplyAudioSettings();
		Save();
	}

	public void SetKeyboardCombatAlternative(bool value)
	{
		EnableKeyboardCombatAlternative = value;
		ApplyInputSettings();
		Save();
	}

	public void SetGameplayFeel(float inputBufferTime, float coyoteTime, float guardDeflectWindow)
	{
		InputBufferTime = Mathf.Clamp(inputBufferTime, 0.05f, 0.25f);
		CoyoteTime = Mathf.Clamp(coyoteTime, 0.03f, 0.2f);
		GuardDeflectWindow = Mathf.Clamp(guardDeflectWindow, 0.08f, 0.32f);
		Save();
	}

	public void CycleDisplayPreset()
	{
		var nextPreset = (DisplaySettingsPreset)NextIndex((int)DisplayPreset, 4);
		Fullscreen = nextPreset is DisplaySettingsPreset.FullscreenVSync or DisplaySettingsPreset.FullscreenUnlocked;
		VSync = nextPreset is DisplaySettingsPreset.WindowedVSync or DisplaySettingsPreset.FullscreenVSync;
		ApplyDisplaySettings();
		Save();
	}

	public void CycleAudioPreset()
	{
		SetMasterVolume(MasterVolume <= 0.01f ? 1f : Mathf.Max(0f, MasterVolume - 0.2f));
	}

	public void CycleInputPreset()
	{
		SetKeyboardCombatAlternative(!EnableKeyboardCombatAlternative);
	}

	public void CycleGameplayPreset()
	{
		switch (GameplayPreset)
		{
			case GameplaySettingsPreset.Standard:
				SetGameplayFeel(0.15f, 0.12f, 0.22f);
				break;
			case GameplaySettingsPreset.Forgiving:
				SetGameplayFeel(0.18f, 0.15f, 0.28f);
				break;
			default:
				SetGameplayFeel(0.12f, 0.10f, 0.18f);
				break;
		}
	}

	public void ApplyAll()
	{
		ApplyDisplaySettings();
		ApplyAudioSettings();
		ApplyInputSettings();
	}

	public void ApplyGameplaySettings(PlayerController player)
	{
		player.InputBufferTime = InputBufferTime;
		player.CoyoteTime = CoyoteTime;
		player.GuardDeflectWindow = GuardDeflectWindow;
	}

	public string BuildSummary()
	{
		return
			"[b]当前设置[/b]\n\n" +
			$"显示：{DisplayLabel}\n" +
			$"音频：{AudioLabel}\n" +
			$"按键：{InputLabel}\n" +
			$"玩法：{GameplayLabel}";
	}

	private void ApplyDisplaySettings()
	{
		DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

		if (!Fullscreen)
		{
			DisplayServer.WindowSetSize(ResolutionOptions[ResolutionIndex]);
		}
	}

	private void ApplyAudioSettings()
	{
		ApplyBusVolume("Master", MasterVolume);
		ApplyBusVolume("Music", MusicVolume * MasterVolume);
		ApplyBusVolume("BGM", MusicVolume * MasterVolume);
		ApplyBusVolume("SFX", SfxVolume * MasterVolume);
	}

	private static void ApplyInputSettings()
	{
		GameInputActions.ApplyBindingPreset(Instance?.EnableKeyboardCombatAlternative == true);
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

		ResolutionIndex = ClampIndex(config.GetValue("display", "resolution_index", ResolutionIndex).AsInt32(), ResolutionOptions.Length);
		Fullscreen = config.GetValue("display", "fullscreen", Fullscreen).AsBool();
		VSync = config.GetValue("display", "vsync", VSync).AsBool();
		MasterVolume = Clamp01(config.GetValue("audio", "master_volume", MasterVolume).AsSingle());
		MusicVolume = Clamp01(config.GetValue("audio", "music_volume", MusicVolume).AsSingle());
		SfxVolume = Clamp01(config.GetValue("audio", "sfx_volume", SfxVolume).AsSingle());
		EnableKeyboardCombatAlternative = config.GetValue("input", "keyboard_combat_alternative", EnableKeyboardCombatAlternative).AsBool();
		InputBufferTime = Mathf.Clamp(config.GetValue("gameplay", "input_buffer_time", InputBufferTime).AsSingle(), 0.05f, 0.25f);
		CoyoteTime = Mathf.Clamp(config.GetValue("gameplay", "coyote_time", CoyoteTime).AsSingle(), 0.03f, 0.2f);
		GuardDeflectWindow = Mathf.Clamp(config.GetValue("gameplay", "guard_deflect_window", GuardDeflectWindow).AsSingle(), 0.08f, 0.32f);
	}

	private void Save()
	{
		var config = new ConfigFile();
		config.SetValue("display", "resolution_index", ResolutionIndex);
		config.SetValue("display", "fullscreen", Fullscreen);
		config.SetValue("display", "vsync", VSync);
		config.SetValue("audio", "master_volume", MasterVolume);
		config.SetValue("audio", "music_volume", MusicVolume);
		config.SetValue("audio", "sfx_volume", SfxVolume);
		config.SetValue("input", "keyboard_combat_alternative", EnableKeyboardCombatAlternative);
		config.SetValue("gameplay", "input_buffer_time", InputBufferTime);
		config.SetValue("gameplay", "coyote_time", CoyoteTime);
		config.SetValue("gameplay", "guard_deflect_window", GuardDeflectWindow);

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

	private static float Clamp01(float value)
	{
		return Mathf.Clamp(value, 0f, 1f);
	}

	private static int ToPercent(float value)
	{
		return Mathf.RoundToInt(Mathf.Clamp(value, 0f, 1f) * 100f);
	}
}
