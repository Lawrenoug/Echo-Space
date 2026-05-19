using EchoSpace.Player;
using EchoSpace.Core.World;
using EchoSpace.Core.Input;
using EchoSpace.Gameplay.Inventory;
using EchoSpace.Gameplay.Progression;
using Godot;
using System.Collections.Generic;
using System.Text;

namespace EchoSpace.UI;

public partial class WorldOverlay : CanvasLayer
{
    [Export] public NodePath? LabelPath { get; set; }
    [Export] public NodePath? TintPath { get; set; }
    [Export] public NodePath? PlayerPath { get; set; }
    [Export] public NodePath? HealthLabelPath { get; set; }
    [Export] public NodePath? HealthBarPath { get; set; }
    [Export] public NodePath? StaminaLabelPath { get; set; }
    [Export] public NodePath? StaminaBarPath { get; set; }
    [Export] public NodePath? InventoryPanelPath { get; set; }
    [Export] public NodePath? InventoryTextPath { get; set; }
    [Export] public NodePath? ProgressionPanelPath { get; set; }
    [Export] public NodePath? ProgressionTextPath { get; set; }
    [Export] public NodePath? TalentPanelPath { get; set; }
    [Export] public NodePath? TalentTreeViewPath { get; set; }
    [Export] public NodePath? TalentDetailTextPath { get; set; }

    private Label? _label;
    private Label? _healthLabel;
    private Label? _staminaLabel;
    private Range? _healthBar;
    private Range? _staminaBar;
    private Control? _inventoryPanel;
    private RichTextLabel? _inventoryText;
    private Control? _progressionPanel;
    private RichTextLabel? _progressionText;
    private Control? _talentPanel;
    private TalentTreeView? _talentTreeView;
    private RichTextLabel? _talentDetailText;
    private CanvasModulate? _tint;
    private PlayerController? _player;
    private InventoryManager? _inventoryManager;
    private ProgressionManager? _progressionManager;
    private TalentTreeManager? _talentTreeManager;
    private readonly List<Control> _systemPanelStack = new();
    private bool _isSubscribed;
    private bool _isTalentViewBound;
    private int _lastDisplayedHealth = int.MinValue;
    private int _lastDisplayedMaxHealth = int.MinValue;
    private int _lastDisplayedStamina = int.MinValue;
    private int _lastDisplayedMaxStamina = int.MinValue;
    private string _lastInventoryMessage = string.Empty;
    private string _selectedTalentNodeId = string.Empty;

    public override void _Ready()
    {
        ResolveBindings();

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            OnWorldChanged(WorldManager.Instance.CurrentWorld);
        }

        _inventoryManager = InventoryManager.Instance;
        if (_inventoryManager != null)
        {
            _inventoryManager.InventoryChanged += RefreshInventoryPanel;
        }

        _progressionManager = ProgressionManager.Instance;
        if (_progressionManager != null)
        {
            _progressionManager.LevelChanged += OnProgressionChanged;
            _progressionManager.UnspentPointsChanged += OnProgressionChanged;
            _progressionManager.AttributeChanged += OnAttributeChanged;
            _progressionManager.AttributesReset += RefreshProgressionPanel;
        }

        _talentTreeManager = TalentTreeManager.Instance;
        if (_talentTreeManager != null)
        {
            _talentTreeManager.TreeChanged += RefreshTalentPanel;
            _selectedTalentNodeId = _talentTreeManager.StartNodeId;
        }

        BindTalentTreeView();

        if (!TrySubscribeToPlayer())
        {
            GD.PrintErr("Player not found! Check PlayerPath.");
        }

        RefreshInventoryPanel();
        RefreshProgressionPanel();
        RefreshTalentPanel();
        CloseAllSystemPanels();
    }

    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = null;
            _isSubscribed = false;
            ResolveBindings();
            TrySubscribeToPlayer();
            return;
        }

        RefreshPlayerVitals(false);
    }

    public override void _ExitTree()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= OnWorldChanged;
        }

        if (_isSubscribed && _player != null)
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
            _player.StaminaChanged -= OnPlayerStaminaChanged;
        }

        if (_inventoryManager != null)
        {
            _inventoryManager.InventoryChanged -= RefreshInventoryPanel;
        }

        if (_progressionManager != null)
        {
            _progressionManager.LevelChanged -= OnProgressionChanged;
            _progressionManager.UnspentPointsChanged -= OnProgressionChanged;
            _progressionManager.AttributeChanged -= OnAttributeChanged;
            _progressionManager.AttributesReset -= RefreshProgressionPanel;
        }

        if (_talentTreeManager != null)
        {
            _talentTreeManager.TreeChanged -= RefreshTalentPanel;
        }

        if (_isTalentViewBound && _talentTreeView != null)
        {
            _talentTreeView.NodeSelected -= OnTalentNodeSelected;
            _talentTreeView.NodeUnlockRequested -= OnTalentUnlockRequested;
            _talentTreeView.NodeRefundRequested -= OnTalentRefundRequested;
        }
    }

    private void OnWorldChanged(WorldType worldType)
    {
        if (_label != null)
        {
            _label.Text = worldType == WorldType.Reality
                ? "Reality World  [Tab]"
                : "Soul World  [Tab]";
        }

        if (_tint != null)
        {
            _tint.Color = worldType == WorldType.Reality
                ? new Color(1f, 0.97f, 0.92f, 1f)
                : new Color(0.72f, 0.82f, 1f, 1f);
        }
    }

    private void OnPlayerStaminaChanged(float currentStamina, float maxStamina)
    {
        _lastDisplayedStamina = Mathf.RoundToInt(currentStamina);
        _lastDisplayedMaxStamina = Mathf.RoundToInt(maxStamina);

        if (_staminaLabel != null)
        {
            _staminaLabel.Text = $"Stamina {Mathf.RoundToInt(currentStamina)}/{Mathf.RoundToInt(maxStamina)}";
        }

        if (_staminaBar != null)
        {
            _staminaBar.MaxValue = maxStamina;
            _staminaBar.Value = currentStamina;
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        _lastDisplayedHealth = currentHealth;
        _lastDisplayedMaxHealth = maxHealth;

        if (_healthLabel != null)
        {
            _healthLabel.Text = $"HP {currentHealth}/{maxHealth}";
        }

        if (_healthBar != null)
        {
            _healthBar.MaxValue = maxHealth;
            _healthBar.Value = currentHealth;
        }
    }

    private void ResolveBindings()
    {
        _label ??= ResolveNode<Label>(LabelPath, "Panel/Label");
        _healthLabel ??= ResolveNode<Label>(HealthLabelPath, "Panel/HealthLabel");
        _staminaLabel ??= ResolveNode<Label>(StaminaLabelPath, "Panel/StaminaLabel");
        _healthBar ??= ResolveNode<Range>(HealthBarPath, "Panel/HealthBar");
        _staminaBar ??= ResolveNode<Range>(StaminaBarPath, "Panel/StaminaBar");
        _tint ??= ResolveNode<CanvasModulate>(TintPath, "../WorldTint");
        _player ??= ResolvePlayer();
        _inventoryPanel ??= ResolveNode<Control>(InventoryPanelPath, "InventoryPanel");
        _inventoryText ??= ResolveNode<RichTextLabel>(InventoryTextPath, "InventoryPanel/Body");
        _progressionPanel ??= ResolveNode<Control>(ProgressionPanelPath, "ProgressionPanel");
        _progressionText ??= ResolveNode<RichTextLabel>(ProgressionTextPath, "ProgressionPanel/Body");
        EnsureTalentPanelExists();
        _talentPanel ??= ResolveNode<Control>(TalentPanelPath, "TalentPanel");
        _talentTreeView ??= ResolveNode<TalentTreeView>(TalentTreeViewPath, "TalentPanel/TreeView");
        _talentDetailText ??= ResolveNode<RichTextLabel>(TalentDetailTextPath, "TalentPanel/Details");
    }

    private bool TrySubscribeToPlayer()
    {
        if (_isSubscribed || _player == null)
        {
            return _isSubscribed;
        }

        _player.HealthChanged += OnPlayerHealthChanged;
        _player.StaminaChanged += OnPlayerStaminaChanged;
        _isSubscribed = true;
        RefreshPlayerVitals(true);
        return true;
    }

    private void RefreshPlayerVitals(bool logChanges)
    {
        if (_player == null)
        {
            return;
        }

        if (_player.CurrentHealth != _lastDisplayedHealth || _player.MaxHealth != _lastDisplayedMaxHealth)
        {
            if (logChanges)
            {
                OnPlayerHealthChanged(_player.CurrentHealth, _player.MaxHealth);
            }
            else
            {
                UpdateHealthVisuals(_player.CurrentHealth, _player.MaxHealth);
            }
        }

        var currentStamina = Mathf.RoundToInt(_player.CurrentStamina);
        var maxStamina = Mathf.RoundToInt(_player.MaxStamina);
        if (currentStamina != _lastDisplayedStamina || maxStamina != _lastDisplayedMaxStamina)
        {
            if (logChanges)
            {
                OnPlayerStaminaChanged(_player.CurrentStamina, _player.MaxStamina);
            }
            else
            {
                UpdateStaminaVisuals(_player.CurrentStamina, _player.MaxStamina);
            }
        }
    }

    private void UpdateHealthVisuals(int currentHealth, int maxHealth)
    {
        _lastDisplayedHealth = currentHealth;
        _lastDisplayedMaxHealth = maxHealth;

        if (_healthLabel != null)
        {
            _healthLabel.Text = $"HP {currentHealth}/{maxHealth}";
        }

        if (_healthBar != null)
        {
            _healthBar.MaxValue = maxHealth;
            _healthBar.Value = currentHealth;
        }
    }

    private void UpdateStaminaVisuals(float currentStamina, float maxStamina)
    {
        var roundedCurrent = Mathf.RoundToInt(currentStamina);
        var roundedMax = Mathf.RoundToInt(maxStamina);
        _lastDisplayedStamina = roundedCurrent;
        _lastDisplayedMaxStamina = roundedMax;

        if (_staminaLabel != null)
        {
            _staminaLabel.Text = $"Stamina {roundedCurrent}/{roundedMax}";
        }

        if (_staminaBar != null)
        {
            _staminaBar.MaxValue = maxStamina;
            _staminaBar.Value = currentStamina;
        }
    }

    private T? ResolveNode<T>(NodePath? primaryPath, string fallbackPath) where T : class
    {
        if (primaryPath != null && !primaryPath.IsEmpty)
        {
            var fromPrimary = GetNodeOrNull<T>(primaryPath);
            if (fromPrimary != null)
            {
                return fromPrimary;
            }
        }

        return GetNodeOrNull<T>(fallbackPath);
    }

    private PlayerController? ResolvePlayer()
    {
        if (PlayerPath != null && !PlayerPath.IsEmpty)
        {
            var fromPath = GetNodeOrNull<PlayerController>(PlayerPath);
            if (fromPath != null)
            {
                return fromPath;
            }
        }

        return GetNodeOrNull<PlayerController>("../Player")
            ?? GetTree().GetFirstNodeInGroup("player") as PlayerController;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameInputActions.ToggleInventory))
        {
            ToggleSystemPanel(_inventoryPanel);
            RefreshInventoryPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(GameInputActions.ToggleProgression))
        {
            ToggleSystemPanel(_progressionPanel);
            RefreshProgressionPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(GameInputActions.ToggleTalentTree))
        {
            ToggleSystemPanel(_talentPanel);
            RefreshTalentPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (keyEvent.Keycode == Key.Escape)
        {
            CloseTopSystemPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (IsTopSystemPanel(_inventoryPanel) && _inventoryManager != null)
        {
            HandleInventoryInput(keyEvent);
            return;
        }

        if (IsTopSystemPanel(_talentPanel) && _talentTreeManager != null)
        {
            if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.Space)
            {
                if (_talentTreeView?.ActivateSelectedNode() == true)
                {
                    GetViewport().SetInputAsHandled();
                }

                return;
            }

            if (keyEvent.Keycode == Key.Backspace || keyEvent.Keycode == Key.Delete)
            {
                if (_talentTreeView?.RefundSelectedNode() == true)
                {
                    GetViewport().SetInputAsHandled();
                }

                return;
            }

            if (keyEvent.Keycode == Key.R)
            {
                _talentTreeManager.ResetUnlockedTalents();
                RefreshTalentPanel();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (_progressionPanel?.Visible != true || _progressionManager == null)
        {
            return;
        }

        if (!IsTopSystemPanel(_progressionPanel))
        {
            return;
        }

        var attributeType = keyEvent.Keycode switch
        {
            Key.Key1 => PlayerAttributeType.Vitality,
            Key.Key2 => PlayerAttributeType.Endurance,
            Key.Key3 => PlayerAttributeType.Strength,
            Key.Key4 => PlayerAttributeType.Deflection,
            Key.Key5 => PlayerAttributeType.SoulAttunement,
            _ => (PlayerAttributeType?)null,
        };

        if (attributeType.HasValue)
        {
            if (keyEvent.ShiftPressed)
            {
                _progressionManager.TryRefundPoint(attributeType.Value);
            }
            else
            {
                _progressionManager.TryAllocatePoint(attributeType.Value);
            }

            RefreshProgressionPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.R)
        {
            _progressionManager.ResetAllocatedPoints();
            RefreshProgressionPanel();
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleInventoryInput(InputEventKey keyEvent)
    {
        if (_inventoryManager == null)
        {
            return;
        }

        var slotIndex = GetNumberKeyIndex(keyEvent.Keycode);
        if (slotIndex >= 0)
        {
            var changed = keyEvent.ShiftPressed
                ? _inventoryManager.TryDropSlot(slotIndex)
                : _inventoryManager.TryUseSlot(slotIndex, _player);

            _lastInventoryMessage = changed
                ? keyEvent.ShiftPressed ? $"已丢弃第 {slotIndex + 1} 格物品。" : $"已使用第 {slotIndex + 1} 格物品。"
                : keyEvent.ShiftPressed ? $"第 {slotIndex + 1} 格物品不可丢弃。" : $"第 {slotIndex + 1} 格物品不可使用。";
            RefreshInventoryPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.U)
        {
            var used = _inventoryManager.TryUseFirstUsable(_player);
            _lastInventoryMessage = used ? "已使用第一个可用消耗品。" : "当前没有可使用物品，或状态已满。";
            RefreshInventoryPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Keycode == Key.Delete)
        {
            var dropped = _inventoryManager.TryDropFirstDroppable();
            _lastInventoryMessage = dropped ? "已丢弃第一个可丢弃物品。" : "当前没有可丢弃物品。";
            RefreshInventoryPanel();
            GetViewport().SetInputAsHandled();
        }
    }

    private void RefreshInventoryPanel()
    {
        if (_inventoryText == null)
        {
            return;
        }

        _inventoryManager ??= InventoryManager.Instance;
        if (_inventoryManager == null)
        {
            _inventoryText.Text = "背包管理器未加载。";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("背包  [I]");
        builder.AppendLine();

        var occupiedSlots = 0;
        for (var index = 0; index < _inventoryManager.Slots.Count; index++)
        {
            var slot = _inventoryManager.Slots[index];
            if (slot.IsEmpty || slot.Item == null)
            {
                continue;
            }

            occupiedSlots += 1;
            builder.Append('[').Append((index + 1).ToString("00")).Append("] ");
            builder.Append(slot.Item.DisplayName);
            builder.Append("  x").Append(slot.Quantity);
            builder.Append("  <").Append(slot.Item.Category).Append('>');
            builder.AppendLine();
        }

        if (occupiedSlots == 0)
        {
            builder.AppendLine("当前背包为空。");
        }

        builder.AppendLine();
        builder.Append("容量: ").Append(occupiedSlots).Append('/').Append(_inventoryManager.Slots.Count);
        builder.AppendLine();
        builder.AppendLine("按 1-9 使用对应槽位");
        builder.AppendLine("按 Shift+1-9 丢弃对应槽位");
        builder.AppendLine("按 U 使用第一个可用消耗品，按 Delete 丢弃第一个可丢弃物品");
        builder.AppendLine("关键道具 / 任务道具不可丢弃");
        if (!string.IsNullOrWhiteSpace(_lastInventoryMessage))
        {
            builder.Append("状态: ").AppendLine(_lastInventoryMessage);
        }

        builder.Append("按 [Esc] 关闭");

        _inventoryText.Text = builder.ToString();
    }

    private void RefreshProgressionPanel()
    {
        if (_progressionText == null)
        {
            return;
        }

        _progressionManager ??= ProgressionManager.Instance;
        if (_progressionManager == null)
        {
            _progressionText.Text = "加点管理器未加载。";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("加点系统  [P]");
        builder.Append("等级: ").Append(_progressionManager.CurrentLevel);
        builder.Append("    未分配点数: ").Append(_progressionManager.UnspentPoints);
        builder.AppendLine();
        builder.AppendLine();

        AppendAttributeLine(builder, Key.Key1, PlayerAttributeType.Vitality, "生命");
        AppendAttributeLine(builder, Key.Key2, PlayerAttributeType.Endurance, "耐力");
        AppendAttributeLine(builder, Key.Key3, PlayerAttributeType.Strength, "架势输出");
        AppendAttributeLine(builder, Key.Key4, PlayerAttributeType.Deflection, "防反效率");
        AppendAttributeLine(builder, Key.Key5, PlayerAttributeType.SoulAttunement, "灵魂适性");

        builder.AppendLine();
        var modifiers = _progressionManager.BuildCombatModifiers();
        builder.Append("当前加成: HP +").Append(modifiers.BonusHealth);
        builder.Append(" / 耐力 +").Append(Mathf.RoundToInt(modifiers.BonusStamina));
        builder.Append(" / 攻击 +").Append(modifiers.BonusAttackDamage);
        builder.AppendLine();
        builder.Append("架势输出 x").Append(modifiers.AttackPostureMultiplier.ToString("0.00"));
        builder.Append(" / 防反架势 x").Append(modifiers.DeflectPostureMultiplier.ToString("0.00"));
        builder.Append(" / 防御耗耐 x").Append(modifiers.GuardStaminaMultiplier.ToString("0.00"));
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("按 1-5 加点");
        builder.AppendLine("按 Shift+1-5 退点");
        builder.AppendLine("按 R 重置已分配点数");
        builder.Append("按 [Esc] 关闭");

        _progressionText.Text = builder.ToString();
    }

    private void RefreshTalentPanel()
    {
        if (_talentDetailText == null)
        {
            return;
        }

        _talentTreeManager ??= TalentTreeManager.Instance;
        BindTalentTreeView();

        if (_talentTreeManager == null)
        {
            _talentDetailText.Text = "天赋树管理器未加载。";
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedTalentNodeId)
            || _talentTreeManager.GetNodeDefinition(_selectedTalentNodeId) == null)
        {
            _selectedTalentNodeId = _talentTreeManager.StartNodeId;
        }

        _talentTreeView?.SetSelectedNode(_selectedTalentNodeId, false);

        var selectedDefinition = _talentTreeManager.GetNodeDefinition(_selectedTalentNodeId)
            ?? _talentTreeManager.GetNodeDefinition(_talentTreeManager.StartNodeId);
        if (selectedDefinition == null)
        {
            _talentDetailText.Text = "当前天赋树没有可用节点。";
            return;
        }

        var selectedState = _talentTreeManager.GetNodeState(selectedDefinition.Id);
        var builder = new StringBuilder();
        builder.AppendLine("天赋树  [T]");
        builder.Append("剩余点数: ").Append(_talentTreeManager.UnspentTalentPoints);
        builder.Append("    已解锁节点: ").Append(_talentTreeManager.GetUnlockedNodeCount());
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("操作说明");
        builder.AppendLine("左键：选中节点");
        builder.AppendLine("左键点击已选中的可用节点 / 回车：解锁");
        builder.AppendLine("右键点击已解锁节点 / Backspace：退款");
        builder.AppendLine("滚轮：缩放    中键拖拽：平移");
        builder.AppendLine("R：重置本次已解锁天赋");
        builder.AppendLine();
        builder.Append("当前选中: ").AppendLine(selectedDefinition.DisplayName);
        builder.Append("类型: ").Append(GetTalentNodeTypeDisplayName(selectedDefinition.NodeType));
        builder.Append("    消耗: ").Append(selectedDefinition.Cost);
        builder.Append("    状态: ").AppendLine(BuildTalentNodeStatus(selectedDefinition, selectedState));
        builder.AppendLine();
        builder.AppendLine(selectedDefinition.Description);
        builder.AppendLine();
        builder.AppendLine("当前是可扩展的白盒天赋树骨架，后续可以继续替换成真正的战斗、探索、双世界和机动类天赋。");
        builder.AppendLine();
        builder.Append("[Esc] 关闭");

        _talentDetailText.Text = builder.ToString();
    }

    private void AppendAttributeLine(StringBuilder builder, Key key, PlayerAttributeType attributeType, string displayName)
    {
        if (_progressionManager == null || !_progressionManager.Attributes.TryGetValue(attributeType, out var attributeState))
        {
            return;
        }

        builder.Append((int)(key - Key.Key0)).Append(". ");
        builder.Append(displayName);
        builder.Append("  Lv ").Append(attributeState.CurrentLevel);
        builder.Append("  (基础 ").Append(attributeState.BaseLevel).Append(')');
        if (attributeState.AllocatedPoints > 0)
        {
            builder.Append("  +").Append(attributeState.AllocatedPoints);
        }

        builder.AppendLine();
    }

    private void OnProgressionChanged(int _)
    {
        RefreshProgressionPanel();
    }

    private void OnAttributeChanged(PlayerAttributeType _, int __)
    {
        RefreshProgressionPanel();
    }

    private void OnTalentNodeSelected(string nodeId)
    {
        _selectedTalentNodeId = nodeId;
        RefreshTalentPanel();
    }

    private void OnTalentUnlockRequested(string nodeId)
    {
        _talentTreeManager ??= TalentTreeManager.Instance;
        if (_talentTreeManager == null)
        {
            return;
        }

        if (_talentTreeManager.TryUnlockNode(nodeId))
        {
            _selectedTalentNodeId = nodeId;
            RefreshTalentPanel();
        }
    }

    private void OnTalentRefundRequested(string nodeId)
    {
        _talentTreeManager ??= TalentTreeManager.Instance;
        if (_talentTreeManager == null)
        {
            return;
        }

        if (_talentTreeManager.TryRefundNode(nodeId))
        {
            _selectedTalentNodeId = nodeId;
            RefreshTalentPanel();
        }
    }

    private void ToggleSystemPanel(Control? panel)
    {
        if (panel == null)
        {
            return;
        }

        if (_systemPanelStack.Count > 0 && ReferenceEquals(_systemPanelStack[^1], panel))
        {
            CloseTopSystemPanel();
            return;
        }

        OpenSystemPanel(panel);
    }

    private void OpenSystemPanel(Control panel)
    {
        _systemPanelStack.Remove(panel);
        _systemPanelStack.Add(panel);
        UpdateSystemPanelLayering();
    }

    private void CloseTopSystemPanel()
    {
        if (_systemPanelStack.Count == 0)
        {
            return;
        }

        _systemPanelStack.RemoveAt(_systemPanelStack.Count - 1);
        UpdateSystemPanelLayering();
    }

    private void CloseAllSystemPanels()
    {
        _systemPanelStack.Clear();
        UpdateSystemPanelLayering();
    }

    private void UpdateSystemPanelLayering()
    {
        ApplyPanelLayer(_inventoryPanel);
        ApplyPanelLayer(_progressionPanel);
        ApplyPanelLayer(_talentPanel);
    }

    private void ApplyPanelLayer(Control? panel)
    {
        if (panel == null)
        {
            return;
        }

        var stackIndex = _systemPanelStack.IndexOf(panel);
        if (stackIndex < 0)
        {
            panel.Visible = false;
            panel.ZIndex = 0;
            panel.Modulate = Colors.White;
            return;
        }

        panel.Visible = true;
        panel.ZIndex = 10 + stackIndex;

        var isTop = stackIndex == _systemPanelStack.Count - 1;
        panel.Modulate = isTop
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(0.82f, 0.82f, 0.82f, 0.82f);
    }

    private bool IsTopSystemPanel(Control? panel)
    {
        return panel != null
            && _systemPanelStack.Count > 0
            && ReferenceEquals(_systemPanelStack[^1], panel);
    }

    private void EnsureTalentPanelExists()
    {
        if (GetNodeOrNull<Control>("TalentPanel") != null)
        {
            return;
        }

        var panel = new Panel
        {
            Name = "TalentPanel",
            Visible = false,
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -480f,
            OffsetTop = -300f,
            OffsetRight = 480f,
            OffsetBottom = 300f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        AddChild(panel);

        var treeView = new TalentTreeView
        {
            Name = "TreeView",
            OffsetLeft = 16f,
            OffsetTop = 16f,
            OffsetRight = 632f,
            OffsetBottom = 584f,
        };
        panel.AddChild(treeView);

        var divider = new ColorRect
        {
            Name = "Divider",
            OffsetLeft = 646f,
            OffsetTop = 16f,
            OffsetRight = 648f,
            OffsetBottom = 584f,
            Color = new Color(0.31f, 0.34f, 0.40f, 0.88f),
        };
        panel.AddChild(divider);

        var details = new RichTextLabel
        {
            Name = "Details",
            OffsetLeft = 664f,
            OffsetTop = 16f,
            OffsetRight = 944f,
            OffsetBottom = 584f,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        panel.AddChild(details);
    }

    private void BindTalentTreeView()
    {
        if (_isTalentViewBound || _talentTreeView == null)
        {
            return;
        }

        _talentTreeView.NodeSelected += OnTalentNodeSelected;
        _talentTreeView.NodeUnlockRequested += OnTalentUnlockRequested;
        _talentTreeView.NodeRefundRequested += OnTalentRefundRequested;
        _isTalentViewBound = true;
    }

    private string BuildTalentNodeStatus(TalentNodeDefinition definition, TalentNodeState? state)
    {
        _talentTreeManager ??= TalentTreeManager.Instance;
        if (_talentTreeManager == null)
        {
            return "不可用";
        }

        if (state?.IsUnlocked == true)
        {
            return definition.IsStart ? "起始节点" : "已解锁";
        }

        return _talentTreeManager.CanUnlockNode(definition.Id)
            ? "可解锁"
            : "未连通";
    }

    private static string GetTalentNodeTypeDisplayName(TalentNodeType nodeType)
    {
        return nodeType switch
        {
            TalentNodeType.Start => "起始",
            TalentNodeType.Minor => "小节点",
            TalentNodeType.Major => "大节点",
            TalentNodeType.Keystone => "关键石",
            _ => "未知",
        };
    }

    private static int GetNumberKeyIndex(Key key)
    {
        return key switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            Key.Key6 => 5,
            Key.Key7 => 6,
            Key.Key8 => 7,
            Key.Key9 => 8,
            _ => -1,
        };
    }
}
