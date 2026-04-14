using EchoSpace.Core.Input;
using EchoSpace.Core.World;
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
    [Export] public NodePath? StatusLabelPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/Status");
    [Export] public NodePath? NewGameButtonPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/NewGameButton");
    [Export] public NodePath? SettingsButtonPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/SettingsButton");
    [Export] public NodePath? QuitButtonPath { get; set; } = new("Overlay/Center/Frame/Margin/Content/ActionColumn/QuitButton");
    [Export] public NodePath? SettingsBackdropPath { get; set; } = new("Overlay/SettingsBackdrop");
    [Export] public NodePath? SettingsPanelPath { get; set; } = new("Overlay/SettingsCard");
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
    private ColorRect? _settingsBackdrop;
    private Control? _settingsPanel;
    private RichTextLabel? _settingsSummary;
    private Button? _displayButton;
    private Button? _audioButton;
    private Button? _controlsButton;
    private Button? _gameplayButton;
    private Button? _backButton;

    public override void _Ready()
    {
        GameInputActions.EnsureDefaults();
        ResolveBindings();
        BindButtons();
        RefreshSettingsSummary();
        SetSettingsVisible(false);
        UpdateStatus("主菜单入口已经就位。开始游戏会进入当前白盒关卡。");
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
        _settingsSummary = ResolveNode<RichTextLabel>(SettingsSummaryPath);
        _displayButton = ResolveNode<Button>(DisplayButtonPath);
        _audioButton = ResolveNode<Button>(AudioButtonPath);
        _controlsButton = ResolveNode<Button>(ControlsButtonPath);
        _gameplayButton = ResolveNode<Button>(GameplayButtonPath);
        _backButton = ResolveNode<Button>(BackButtonPath);
    }

    private void BindButtons()
    {
        if (_newGameButton != null)
        {
            _newGameButton.Pressed += OnNewGamePressed;
        }

        if (_settingsButton != null)
        {
            _settingsButton.Pressed += OnSettingsPressed;
        }

        if (_quitButton != null)
        {
            _quitButton.Pressed += OnQuitPressed;
        }

        if (_displayButton != null)
        {
            _displayButton.Pressed += () => ShowPlaceholder("显示设置");
        }

        if (_audioButton != null)
        {
            _audioButton.Pressed += () => ShowPlaceholder("音频设置");
        }

        if (_controlsButton != null)
        {
            _controlsButton.Pressed += () => ShowPlaceholder("按键设置");
        }

        if (_gameplayButton != null)
        {
            _gameplayButton.Pressed += () => ShowPlaceholder("玩法设置");
        }

        if (_backButton != null)
        {
            _backButton.Pressed += CloseSettings;
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
        UpdateStatus("设置菜单已打开。各分类按钮已经留好接口。");
        _displayButton?.GrabFocus();
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

    private void RefreshSettingsSummary()
    {
        if (_settingsSummary == null)
        {
            return;
        }

        _settingsSummary.Text =
            "这里先保留设置系统的入口层。\n\n" +
            "后续可以在这里接入：\n" +
            "1. 显示模式 / 分辨率 / 垂直同步\n" +
            "2. 总音量 / 音效 / BGM / 界面音\n" +
            "3. 键位重绑定 / 手柄支持\n" +
            "4. 镜头震动 / 受击停顿 / 辅助选项\n\n" +
            "当前各按钮已经留好回调接口。";
    }

    private void ShowPlaceholder(string featureName)
    {
        UpdateStatus($"{featureName}接口已预留，后续接入正式设置系统。");
    }

    private void ResetPrototypeGlobalState()
    {
        WorldManager.Instance?.SetWorld(WorldType.Reality);
        InventoryManager.Instance?.ResetToDefaults();
        ProgressionManager.Instance?.ResetToDefaults();
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

    private T? ResolveNode<T>(NodePath? path) where T : class
    {
        if (path == null || path.IsEmpty)
        {
            return null;
        }

        return GetNodeOrNull<T>(path);
    }
}
