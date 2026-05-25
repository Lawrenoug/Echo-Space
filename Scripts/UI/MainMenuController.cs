using EchoSpace.Core.Input;
using EchoSpace.Core.Settings;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Equipment;
using EchoSpace.Gameplay.Inventory;
using EchoSpace.Gameplay.Progression;
using Godot;

namespace EchoSpace.UI;

public partial class MainMenuController : Control
{
    [Signal] public delegate void NewGameRequestedEventHandler();
    [Signal] public delegate void SettingsOpenedEventHandler();
    [Signal] public delegate void SettingsClosedEventHandler();
    [Signal] public delegate void QuitRequestedEventHandler();

    [Export(PropertyHint.File, "*.tscn")] public string GameScenePath { get; set; } = "res://Scenes/Main.tscn";
    [Export(PropertyHint.File, "*.png")] public string MenuBackgroundPath { get; set; } = "res://Docs/Art/Menu.png";
    [Export(PropertyHint.File, "*.png")] public string ButtonNormalTexturePath { get; set; } = "res://Docs/Art/button.png";
    [Export(PropertyHint.File, "*.png")] public string ButtonHoverTexturePath { get; set; } = "res://Docs/Art/button hover.png";
    [Export(PropertyHint.File, "*.png")] public string ButtonPressedTexturePath { get; set; } = "res://Docs/Art/button pressed.png";
    [Export] public NodePath? StatusLabelPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/Status");
    [Export] public NodePath? NewGameButtonPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/NewGameButton");
    [Export] public NodePath? SettingsButtonPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/SettingsButton");
    [Export] public NodePath? QuitButtonPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/QuitButton");
    [Export] public NodePath? SettingsBackdropPath { get; set; } = new("Overlay/SettingsBackdrop");
    [Export] public NodePath? SettingsPanelPath { get; set; } = new("Overlay/SettingsCard");
    [Export] public NodePath? SettingsBodyPath { get; set; } = new("Overlay/SettingsCard/Margin/Body");
    [Export] public NodePath? SettingsSummaryPath { get; set; } = new("Overlay/SettingsCard/Margin/Body/Summary");
    [Export] public NodePath? DisplayButtonPath { get; set; } = new("Overlay/SettingsCard/Margin/Body/DisplayButton");
    [Export] public NodePath? AudioButtonPath { get; set; } = new("Overlay/SettingsCard/Margin/Body/AudioButton");
    [Export] public NodePath? ControlsButtonPath { get; set; } = new("Overlay/SettingsCard/Margin/Body/ControlsButton");
    [Export] public NodePath? GameplayButtonPath { get; set; } = new("Overlay/SettingsCard/Margin/Body/GameplayButton");
    [Export] public NodePath? BackButtonPath { get; set; } = new("Overlay/SettingsCard/Margin/Body/BackButton");

    private Label? _statusLabel;
    private Button? _newGameButton;
    private Button? _settingsButton;
    private Button? _quitButton;
    private Button? _backButton;
    private ColorRect? _settingsBackdrop;
    private Control? _settingsPanel;
    private VBoxContainer? _settingsBody;
    private RichTextLabel? _settingsSummary;
    private OptionButton? _resolutionOption;
    private CheckButton? _fullscreenToggle;
    private CheckButton? _vsyncToggle;
    private HSlider? _masterVolumeSlider;
    private HSlider? _musicVolumeSlider;
    private HSlider? _sfxVolumeSlider;
    private CheckButton? _keyboardCombatToggle;
    private HSlider? _inputBufferSlider;
    private HSlider? _coyoteSlider;
    private HSlider? _deflectSlider;
    private Label? _masterVolumeValue;
    private Label? _musicVolumeValue;
    private Label? _sfxVolumeValue;
    private Label? _inputBufferValue;
    private Label? _coyoteValue;
    private Label? _deflectValue;

    public override void _Ready()
    {
        GameInputActions.EnsureDefaults();
        GameSettingsManager.Instance?.ApplyAll();
        ResolveBindings();
        ApplyMenuArt();
        BuildSettingsPanel();
        BindButtons();
        RefreshSettingsControls();
        SetSettingsVisible(false);
        UpdateStatus("主菜单已就绪。");
        _newGameButton?.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") || @event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            if (_settingsPanel?.Visible == true)
            {
                CloseSettings();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void ResolveBindings()
    {
        _statusLabel = ResolveNode<Label>(StatusLabelPath);
        _newGameButton = ResolveNode<Button>(NewGameButtonPath);
        _settingsButton = ResolveNode<Button>(SettingsButtonPath);
        _quitButton = ResolveNode<Button>(QuitButtonPath);
        _settingsBackdrop = ResolveNode<ColorRect>(SettingsBackdropPath);
        _settingsPanel = ResolveNode<Control>(SettingsPanelPath);
        _settingsBody = ResolveNode<VBoxContainer>(SettingsBodyPath);
        _settingsSummary = ResolveNode<RichTextLabel>(SettingsSummaryPath);
        _backButton = ResolveNode<Button>(BackButtonPath);
    }

    private void ApplyMenuArt()
    {
        var backgroundTexture = ResourceLoader.Load<Texture2D>(MenuBackgroundPath);
        if (backgroundTexture != null && GetNodeOrNull<TextureRect>("ArtBackground") == null)
        {
            var background = new TextureRect
            {
                Name = "ArtBackground",
                Texture = backgroundTexture,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            background.SetAnchorsPreset(LayoutPreset.FullRect);
            background.Set("expand_mode", 1);
            background.Set("stretch_mode", 6);
            AddChild(background);
            MoveChild(background, 1);
        }

        if (GetNodeOrNull<CanvasItem>("WarmGlow") is { } warmGlow)
        {
            warmGlow.Visible = false;
        }

        if (GetNodeOrNull<CanvasItem>("CoolGlow") is { } coolGlow)
        {
            coolGlow.Visible = false;
        }

        foreach (var button in new[] { _newGameButton, _settingsButton, _quitButton, _backButton })
        {
            ApplyButtonArt(button);
        }
    }

    private void ApplyButtonArt(Button? button)
    {
        if (button == null)
        {
            return;
        }

        var normal = CreateButtonStyle(ButtonNormalTexturePath);
        var hover = CreateButtonStyle(ButtonHoverTexturePath);
        var pressed = CreateButtonStyle(ButtonPressedTexturePath);
        if (normal != null)
        {
            button.AddThemeStyleboxOverride("normal", normal);
        }

        if (hover != null)
        {
            button.AddThemeStyleboxOverride("hover", hover);
            button.AddThemeStyleboxOverride("focus", hover);
        }

        if (pressed != null)
        {
            button.AddThemeStyleboxOverride("pressed", pressed);
        }

        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.91f, 0.82f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", new Color(0.68f, 0.91f, 1f));
    }

    private StyleBoxTexture? CreateButtonStyle(string texturePath)
    {
        var texture = ResourceLoader.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            return null;
        }

        var style = new StyleBoxTexture();
        style.Set("texture", texture);
        style.Set("texture_margin_left", 24);
        style.Set("texture_margin_top", 18);
        style.Set("texture_margin_right", 24);
        style.Set("texture_margin_bottom", 18);
        style.Set("content_margin_left", 24);
        style.Set("content_margin_top", 8);
        style.Set("content_margin_right", 24);
        style.Set("content_margin_bottom", 8);
        return style;
    }

    private void BuildSettingsPanel()
    {
        if (_settingsBody == null)
        {
            return;
        }

        HideLegacySettingsButtons();
        if (_settingsBody.GetNodeOrNull<Control>("SettingsControls") != null)
        {
            return;
        }

        var controls = new VBoxContainer
        {
            Name = "SettingsControls",
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        controls.AddThemeConstantOverride("separation", 10);
        _settingsBody.AddChild(controls);

        AddSectionLabel(controls, "显示");
        _resolutionOption = AddOptionRow(controls, "分辨率");
        _fullscreenToggle = AddCheckRow(controls, "全屏");
        _vsyncToggle = AddCheckRow(controls, "垂直同步");

        AddSectionLabel(controls, "音频");
        (_masterVolumeSlider, _masterVolumeValue) = AddSliderRow(controls, "主音量", 0, 100, 1);
        (_musicVolumeSlider, _musicVolumeValue) = AddSliderRow(controls, "音乐", 0, 100, 1);
        (_sfxVolumeSlider, _sfxVolumeValue) = AddSliderRow(controls, "音效", 0, 100, 1);

        AddSectionLabel(controls, "按键");
        _keyboardCombatToggle = AddCheckRow(controls, "启用 J 攻击 / K 防反备选");

        AddSectionLabel(controls, "玩法");
        (_inputBufferSlider, _inputBufferValue) = AddSliderRow(controls, "输入缓冲", 0.05, 0.25, 0.01);
        (_coyoteSlider, _coyoteValue) = AddSliderRow(controls, "土狼时间", 0.03, 0.20, 0.01);
        (_deflectSlider, _deflectValue) = AddSliderRow(controls, "弹反窗口", 0.08, 0.32, 0.01);

        var back = new Button
        {
            Text = "返回",
            CustomMinimumSize = new Vector2(0f, 48f),
        };
        back.Pressed += CloseSettings;
        controls.AddChild(back);
        _backButton = back;
        ApplyButtonArt(_backButton);
        BindSettingsControls();
    }

    private void HideLegacySettingsButtons()
    {
        foreach (var path in new[]
        {
            SettingsSummaryPath,
            DisplayButtonPath,
            AudioButtonPath,
            ControlsButtonPath,
            GameplayButtonPath,
            BackButtonPath,
        })
        {
            var node = ResolveNode<CanvasItem>(path);
            if (node != null)
            {
                node.Visible = false;
            }
        }
    }

    private void BindButtons()
    {
        if (_newGameButton != null)
        {
            _newGameButton.Text = "开始游戏";
            _newGameButton.Pressed += OnNewGamePressed;
        }

        if (_settingsButton != null)
        {
            _settingsButton.Text = "设置";
            _settingsButton.Pressed += OnSettingsPressed;
        }

        if (_quitButton != null)
        {
            _quitButton.Text = "退出游戏";
            _quitButton.Pressed += OnQuitPressed;
        }
    }

    private void BindSettingsControls()
    {
        if (_resolutionOption != null)
        {
            _resolutionOption.ItemSelected += index =>
            {
                GameSettingsManager.Instance?.SetResolutionIndex((int)index);
                RefreshSettingsControls();
            };
        }

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.Toggled += value =>
            {
                GameSettingsManager.Instance?.SetFullscreen(value);
                RefreshSettingsControls();
            };
        }

        if (_vsyncToggle != null)
        {
            _vsyncToggle.Toggled += value =>
            {
                GameSettingsManager.Instance?.SetVSync(value);
                RefreshSettingsControls();
            };
        }

        BindPercentSlider(_masterVolumeSlider, value => GameSettingsManager.Instance?.SetMasterVolume(value));
        BindPercentSlider(_musicVolumeSlider, value => GameSettingsManager.Instance?.SetMusicVolume(value));
        BindPercentSlider(_sfxVolumeSlider, value => GameSettingsManager.Instance?.SetSfxVolume(value));

        if (_keyboardCombatToggle != null)
        {
            _keyboardCombatToggle.Toggled += value =>
            {
                GameSettingsManager.Instance?.SetKeyboardCombatAlternative(value);
                RefreshSettingsControls();
            };
        }

        BindGameplaySlider(_inputBufferSlider);
        BindGameplaySlider(_coyoteSlider);
        BindGameplaySlider(_deflectSlider);
    }

    private void BindPercentSlider(HSlider? slider, System.Action<float> setter)
    {
        if (slider == null)
        {
            return;
        }

        slider.ValueChanged += value =>
        {
            setter((float)value / 100f);
            RefreshSettingsControls();
        };
    }

    private void BindGameplaySlider(HSlider? slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.ValueChanged += _ =>
        {
            if (_inputBufferSlider == null || _coyoteSlider == null || _deflectSlider == null)
            {
                return;
            }

            GameSettingsManager.Instance?.SetGameplayFeel(
                (float)_inputBufferSlider.Value,
                (float)_coyoteSlider.Value,
                (float)_deflectSlider.Value);
            RefreshSettingsControls();
        };
    }

    private void RefreshSettingsControls()
    {
        var settings = GameSettingsManager.Instance;
        if (settings == null)
        {
            UpdateStatus("设置管理器未找到。");
            return;
        }

        if (_resolutionOption != null && _resolutionOption.ItemCount == 0)
        {
            for (var index = 0; index < settings.GetResolutionCount(); index++)
            {
                _resolutionOption.AddItem(settings.GetResolutionLabel(index), index);
            }
        }

        _resolutionOption?.Select(settings.ResolutionIndex);
        SetToggleNoSignal(_fullscreenToggle, settings.Fullscreen);
        SetToggleNoSignal(_vsyncToggle, settings.VSync);
        SetSliderNoSignal(_masterVolumeSlider, settings.MasterVolume * 100f);
        SetSliderNoSignal(_musicVolumeSlider, settings.MusicVolume * 100f);
        SetSliderNoSignal(_sfxVolumeSlider, settings.SfxVolume * 100f);
        SetToggleNoSignal(_keyboardCombatToggle, settings.EnableKeyboardCombatAlternative);
        SetSliderNoSignal(_inputBufferSlider, settings.InputBufferTime);
        SetSliderNoSignal(_coyoteSlider, settings.CoyoteTime);
        SetSliderNoSignal(_deflectSlider, settings.GuardDeflectWindow);

        SetLabel(_masterVolumeValue, $"{Mathf.RoundToInt(settings.MasterVolume * 100f)}%");
        SetLabel(_musicVolumeValue, $"{Mathf.RoundToInt(settings.MusicVolume * 100f)}%");
        SetLabel(_sfxVolumeValue, $"{Mathf.RoundToInt(settings.SfxVolume * 100f)}%");
        SetLabel(_inputBufferValue, $"{settings.InputBufferTime:0.00}s");
        SetLabel(_coyoteValue, $"{settings.CoyoteTime:0.00}s");
        SetLabel(_deflectValue, $"{settings.GuardDeflectWindow:0.00}s");
        _settingsSummary?.Set("text", settings.BuildSummary());
    }

    private static void SetToggleNoSignal(CheckButton? toggle, bool value)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.SetPressedNoSignal(value);
    }

    private static void SetSliderNoSignal(HSlider? slider, double value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueNoSignal(value);
    }

    private static void SetLabel(Label? label, string text)
    {
        if (label != null)
        {
            label.Text = text;
        }
    }

    private void OnNewGamePressed()
    {
        EmitSignal(SignalName.NewGameRequested);
        ResetPrototypeGlobalState();
        UpdateStatus("正在进入原型关卡...");
        CallDeferred(nameof(ChangeToGameScene));
    }

    private void OnSettingsPressed()
    {
        EmitSignal(SignalName.SettingsOpened);
        SetSettingsVisible(true);
        RefreshSettingsControls();
        UpdateStatus("设置已打开，调整后会立即保存。");
        _resolutionOption?.GrabFocus();
    }

    private void OnQuitPressed()
    {
        EmitSignal(SignalName.QuitRequested);
        GetTree().Quit();
    }

    private void CloseSettings()
    {
        if (_settingsPanel?.Visible != true)
        {
            return;
        }

        SetSettingsVisible(false);
        UpdateStatus("已返回主菜单。");
        EmitSignal(SignalName.SettingsClosed);
        _settingsButton?.GrabFocus();
    }

    private void ChangeToGameScene()
    {
        var result = GetTree().ChangeSceneToFile(GameScenePath);
        if (result != Error.Ok)
        {
            UpdateStatus($"切换场景失败：{result}");
        }
    }

    private void ResetPrototypeGlobalState()
    {
        WorldManager.Instance?.SetWorld(WorldType.Reality);
        InventoryManager.Instance?.ResetToDefaults();
        EquipmentManager.Instance?.ResetToDefaults();
        ProgressionManager.Instance?.ResetToDefaults();
        TalentTreeManager.Instance?.ResetToDefaults();
    }

    private void SetSettingsVisible(bool visible)
    {
        if (_settingsBackdrop != null)
        {
            _settingsBackdrop.Visible = visible;
        }

        if (_settingsPanel != null)
        {
            _settingsPanel.Visible = visible;
        }
    }

    private void UpdateStatus(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = text;
        }
    }

    private static void AddSectionLabel(VBoxContainer parent, string text)
    {
        var label = new Label
        {
            Text = text,
        };
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", new Color(0.95f, 0.76f, 0.48f));
        parent.AddChild(label);
    }

    private static OptionButton AddOptionRow(VBoxContainer parent, string labelText)
    {
        var row = CreateRow(parent, labelText);
        var option = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddChild(option);
        return option;
    }

    private static CheckButton AddCheckRow(VBoxContainer parent, string labelText)
    {
        var row = CreateRow(parent, labelText);
        var toggle = new CheckButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddChild(toggle);
        return toggle;
    }

    private static (HSlider slider, Label valueLabel) AddSliderRow(VBoxContainer parent, string labelText, double min, double max, double step)
    {
        var row = CreateRow(parent, labelText);
        var slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        var valueLabel = new Label
        {
            CustomMinimumSize = new Vector2(70f, 0f),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        row.AddChild(slider);
        row.AddChild(valueLabel);
        return (slider, valueLabel);
    }

    private static HBoxContainer CreateRow(VBoxContainer parent, string labelText)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0f, 34f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 12);

        var label = new Label
        {
            Text = labelText,
            CustomMinimumSize = new Vector2(210f, 0f),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(label);
        parent.AddChild(row);
        return row;
    }

    private T? ResolveNode<T>(NodePath? path) where T : class
    {
        if (path == null || path.IsEmpty)
        {
            return null;
        }

        return GetNodeOrNull<T>(path);
    }
}
