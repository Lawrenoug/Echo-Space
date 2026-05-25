# Echo Space

`Echo Space` 是一个使用 `Godot 4.6.2 + C#` 开发的 2D 横版动作原型。当前阶段优先把双世界切换、白盒关卡、战斗闭环、成长系统、装备框架和玩家表现层做扎实，再逐步接入正式美术、音频和内容。

## README 维护规则

- 每次有代码、场景或系统配置文件修改时，都要同步检查并更新 `README`
- 每次修改文件后，都要提交版本；仓库状态允许时同步推送到 GitHub
- `README` 不再维护容易快速过期的零散流水账
- 重点保留“当前框架说明”“关键文件位置”“当前最适合继续推进的方向”“人工资源填充清单”
- 正式策划案统一维护在 [Docs/GameDesignDocument.md](/F:/Godot%20project/echo-space/Docs/GameDesignDocument.md)

## 当前项目方向

- 核心玩法：现实世界 / 灵魂世界实时切换
- 关卡结构：长横向、多层白盒地图，强调探索、回收路线和连续切换
- 战斗方向：以“血量 + 耐力 + 架势 + 处决”为原型的近战系统
- 成长方向：基础属性加点 + 节点式天赋树并行
- 装备方向：玩家主体动画独立存在，后续通过装备系统替换武器模型与表现
- 美术方向：先用统一规格的占位动画跑通工程，再按生产规范替换为正式角色资源

## 当前默认按键

- `A` / `Left`：向左移动
- `D` / `Right`：向右移动
- `Space` / `W` / `Up`：跳跃，轻按低跳，长按高跳
- `鼠标左键`：普通攻击 / 处决
- `鼠标右键`：防御 / 弹反
- `Left Shift`：短冲刺
- `Tab`：切换世界
- `I`：打开 / 关闭背包
- `P`：打开 / 关闭属性加点面板
- `T`：打开 / 关闭天赋树面板
- `1-9`：在背包界面使用对应槽位物品
- `Shift + 1-9`：在背包界面丢弃对应槽位物品
- `U`：在背包界面使用第一个可用消耗品
- `Delete`：在背包界面丢弃第一个可丢弃物品
- `Esc`：关闭当前最上层界面

## 关键文件位置

- 项目主关卡：[Scenes/Main.tscn](/F:/Godot%20project/echo-space/Scenes/Main.tscn)
- 主菜单场景：[Scenes/UI/MainMenu.tscn](/F:/Godot%20project/echo-space/Scenes/UI/MainMenu.tscn)
- 玩家控制器：[Scripts/Player/PlayerController.cs](/F:/Godot%20project/echo-space/Scripts/Player/PlayerController.cs)
- 玩家状态机：[Scripts/Player/States](/F:/Godot%20project/echo-space/Scripts/Player/States)
- 双世界系统：[Scripts/Core/World](/F:/Godot%20project/echo-space/Scripts/Core/World)
- 背包系统：[Scripts/Gameplay/Inventory](/F:/Godot%20project/echo-space/Scripts/Gameplay/Inventory)
- 装备系统：[Scripts/Gameplay/Equipment](/F:/Godot%20project/echo-space/Scripts/Gameplay/Equipment)
- 属性加点管理：[Scripts/Gameplay/Progression/ProgressionManager.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Progression/ProgressionManager.cs)
- 天赋树管理：[Scripts/Gameplay/Progression/TalentTreeManager.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Progression/TalentTreeManager.cs)
- 天赋树视图：[Scripts/UI/TalentTreeView.cs](/F:/Godot%20project/echo-space/Scripts/UI/TalentTreeView.cs)
- 敌人战斗基类：[Scripts/Gameplay/Enemies/EnemyCombatant.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Enemies/EnemyCombatant.cs)
- 白盒环境与机关脚本目录：[Scripts/Gameplay/Environment](/F:/Godot%20project/echo-space/Scripts/Gameplay/Environment)
- HUD 与系统界面：[Scripts/UI/WorldOverlay.cs](/F:/Godot%20project/echo-space/Scripts/UI/WorldOverlay.cs)
- 玩家动作制作规范与 AI 提示词模板：[Docs/Art/PlayerAnimationProductionGuide.md](/F:/Godot%20project/echo-space/Docs/Art/PlayerAnimationProductionGuide.md)
- 玩家占位动画生成脚本：[Docs/Art/generate_player_placeholder_frames.py](/F:/Godot%20project/echo-space/Docs/Art/generate_player_placeholder_frames.py)
- 当前运行中的玩家动作条带：[Docs/Art/PlayerSprite](/F:/Godot%20project/echo-space/Docs/Art/PlayerSprite)
- 当前运行中的玩家逐帧目录：[Docs/Art/PlayerSpriteFrames](/F:/Godot%20project/echo-space/Docs/Art/PlayerSpriteFrames)

## 当前框架说明

- 启动入口是独立主菜单场景，不和游戏主关卡混在一起
- 主菜单运行时会接入 [Docs/Art/Menu.png](/F:/Godot%20project/echo-space/Docs/Art/Menu.png) 作为背景，中央信息框改为半透明面板，避免遮住背景图
- 主菜单按钮使用从原始按钮图裁切适配后的 [Docs/Art/UI](/F:/Godot%20project/echo-space/Docs/Art/UI) 资源，当前统一为 `420x86` 三态按钮，避免原始 `1024x1024` 棋盘底被拉伸进界面
- 设置菜单当前已经接入真实可调整项：分辨率、全屏、垂直同步、主音量、音乐音量、音效音量、键盘战斗备选、输入缓冲、土狼时间和弹反窗口
- 当前存档系统已移除，等关卡、敌人、双世界状态和 UI 结构更稳定后再决定是否重做
- “继续游戏”入口当前暂不接回，后续和存档系统一并恢复
- 主菜单点击开始游戏时，会重置当前原型里的世界状态、背包、装备、属性点和天赋树状态，再进入白盒关卡
- 背包、属性加点、天赋树等二级界面使用统一的“面板栈”逻辑：切换时下层界面保留，关闭顶层后恢复下层
- 玩家当前具备血量、耐力、普通攻击、防御、弹反、处决、轻按低跳 / 长按高跳和短冲刺
- 短冲刺当前已经补上结束后的水平余速，用来验证走路节奏、闪身穿点和后续探索能力空间
- 敌人当前具备血量、架势、受击、破绽、处决、所属世界判定，以及稳定掉落原型
- 敌人运行时位置已经和双世界静态位置刷新解耦，切换世界时不应再被 `DualWorldObject` 写回出生点
- 当前白盒关卡已经串起跳跃、战斗、拾取、加点、双世界切换、双世界机关和短冲刺验证
- 当前已经加入节点式天赋树原型：支持节点显示、连线、解锁、退点、重置，以及后续扩展成类似流放之路的大规模被动树
- 当前装备系统骨架已经接入 `Autoload`，具备装备槽位、默认原型武器、装备事件和玩家武器挂点 `WeaponMount`
- `WeaponMount` 现在会根据 `idle / run / jumpstart / fall / attack / guard / parry / execute / hurt / dead` 自动切换偏移和旋转；攻击与弹反不再跟随整段帧动画慢速插值，而是在动作触发时启动独立的短时武器挥动
- 玩家主体动画当前保持“单一人物主体”方案，不拆身体帧动画和武器帧动画
- 当前玩家主体动画已重新生成成符合策划案方向的无武器占位版本：现实世界偏厚重黑披风、暖色锈金边；灵魂世界偏冷色半透明、青蓝发光边
- 当前玩家占位动画为 `128x160` 统一画布、`baseline = 146`、`scale = 1.0` 的逐帧资源，分为 `reality` 和 `soul` 两套动作
- 玩家显示大小和落点当前统一由 [Scripts/Player/PlayerController.cs](/F:/Godot%20project/echo-space/Scripts/Player/PlayerController.cs) 里的 `AnimationVisualScale`、`AnimationVisualOffset` 和 `WeaponMountOffset` 控制，不再依赖场景里旧的 `AnimatedSprite2D` 缩放值
- 新占位动画是“角色主体优先”的过渡方案，后续正式武器表现仍然应该落在装备系统的武器模型替换上

## 当前玩家动画状态

- 当前玩家动作目录包含：
  - `idle`
  - `run`
  - `jumpstart`
  - `fall`
  - `attack`
  - `hurt`
  - `dead`
  - `execute`
  - `guard`
  - `parry`
- 每个动作都已经生成 `reality / soul` 两个世界版本
- 当前动作资源通过 [Scripts/Player/PlayerController.cs](/F:/Godot%20project/echo-space/Scripts/Player/PlayerController.cs) 直接读取 [Docs/Art/PlayerSpriteFrames](/F:/Godot%20project/echo-space/Docs/Art/PlayerSpriteFrames)
- 当前并不是绕开 Godot 动画系统，而是运行时动态构建 `AnimatedSprite2D + SpriteFrames`；这样保留 Godot 原生播放能力，同时让批量替换帧资源不必手工逐个点编辑器
- `guard` 读取帧数已经对齐为 3 帧，和新占位资源保持一致
- 当前默认人物显示比例切回新无武器主体帧的适配值，默认值为 `AnimationVisualScale = 1.25`、`AnimationVisualOffset = (0, -78)`、`WeaponMountOffset = (18, -82)`
- 当前 live 角色帧是按策划案美术方向重生成的工程占位帧，不包含武器图片；后续正式角色动画仍需要按同名目录和同帧数替换为更精细版本

## 当前白盒关卡结构

当前主关卡已经接入两批双世界机关模板，并按一条连续路线布置：

1. 起点教学段：基础移动、跳跃、拾取与第一只巡逻敌人。
2. 早期平台段：多层平台验证跳跃手感和横向移动。
3. 单世界平台段：通过现实平台和灵魂桥验证“切世界找路”。
4. 按钮门段：踩下按钮后打开前方门，验证机关联动。
5. 差异运动平台段：平台在现实世界静止，在灵魂世界移动，用于跨越障碍。
6. 中段节奏转换段：在差异平台后切回标准跳跃与战斗推进，验证冲刺后的走路节奏。
7. 第二批机关段：加入反向门、双状态升降台、灵魂世界可穿透平台。
8. 连续切换谜题段：要求玩家在现实 / 灵魂之间连续切换，配合平台、按钮完成推进。
9. 终段收尾：完成最后一段机关推进后抵达最终目标点。

## 当前双世界机关模板

- 按钮门：`WorldButtonSwitch + WorldGate`
- 反向门：`WorldGate(OpenWhenDeactivated = true)`
- 单世界平台：`DualWorldPlatform`
- 灵魂世界可穿透平台：`DualWorldPlatform(OneWayCollision = true, ExistsInSoul = true)`
- 差异运动平台：`DifferentialMovingPlatform`
- 双状态升降台：`WorldStateLift`
- 可破坏墙：`BreakableWall`

对应场景与脚本：

- [Scenes/Environment/WorldButtonSwitch.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldButtonSwitch.tscn)
- [Scenes/Environment/WorldGate.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldGate.tscn)
- [Scenes/Environment/DualWorldPlatform.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/DualWorldPlatform.tscn)
- [Scenes/Environment/DifferentialMovingPlatform.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/DifferentialMovingPlatform.tscn)
- [Scenes/Environment/WorldStateLift.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldStateLift.tscn)

## 当前成长与装备系统

- 属性加点面板：负责生命、耐力、攻击、弹反等基础战斗数值成长
- 天赋树面板：负责以后接入技能天赋树、特殊机制节点、探索能力节点和关键石效果
- 当前天赋树是白盒原型，已具备节点绘制、连线关系、可用 / 已解锁 / 已锁定区分、选中详情、解锁、退点、重置、缩放与拖动画布
- 装备系统当前已具备：
  - 装备槽位枚举：武器、副手、头部、身体、饰品
  - 装备管理器 `EquipmentManager`
  - `ItemDefinition` 中的装备槽位与装备模型入口
  - 原型武器场景 [Scenes/Equipment/PrototypeSwordModel.tscn](/F:/Godot%20project/echo-space/Scenes/Equipment/PrototypeSwordModel.tscn)
  - 原型武器贴图 [Docs/Art/Equipment/prototype_training_blade.png](/F:/Godot%20project/echo-space/Docs/Art/Equipment/prototype_training_blade.png)
  - 玩家运行时武器挂点 `WeaponMount`
- 这套结构的目标不是现在就做完整纸娃娃，而是先让“装备数据 -> 装备槽 -> 武器模型挂接”有清晰落点

## 当前掉落与成长闭环

- 敌人死亡后会稳定掉出可拾取的掉落物，而不是后台直接加背包
- 巡逻敌人会掉落基础恢复物
- 追击敌人会掉落可转化为加点收益的 `Memory Shard`
- 灵魂哨卫会掉落更高收益的 `Soul Cluster`
- 掉落物拾取后进入背包，玩家可以再通过背包使用它们，把战斗、拾取、背包、属性加点串成闭环

## 当前最适合继续推进的方向

建议下一步优先从下面这些方向里选：

1. 装备系统实装化：补装备 / 卸下入口、装备栏显示、不同武器原型数据和武器模型切换验证。
2. 武器动作细调：继续按不同武器类型细调 `WeaponMount` 的偏移、旋转、触发时长和打击点，让短剑、长刀、双手武器后续能走不同手感。
3. 天赋树内容化：把当前占位节点替换成真正的战斗、机动、双世界机制和探索能力节点。
4. 白盒实跑与修关：沿着现在这条路线完整跑图，检查哪些跳跃点、按钮位置、切世界时机和冲刺距离还不顺。
5. 正式角色动画替换：按 [Docs/Art/PlayerAnimationProductionGuide.md](/F:/Godot%20project/echo-space/Docs/Art/PlayerAnimationProductionGuide.md) 替换当前 `128x160` 无武器占位动画，保留相同命名和目录结构。
6. 战斗内容扩展：增加第三种敌人或第一个小 Boss，验证现有掉落闭环和敌人基类能否继续复用。

## 人工资源填充清单

以下内容默认需要你后续手工填充。  
这部分是长期保留区：

- 后续无论我怎样更新 `README`，都必须保留这块“人工资源填充清单”，如果有新增资源需求，在这里追加，删除已经完成的功能。

### 角色资源

- 玩家正式待机动画
- 玩家正式跑步动画
- 玩家正式跳跃动画
- 玩家正式下落动画
- 玩家正式攻击动画
- 玩家正式防御动作
- 玩家正式弹反动作或特效表现
- 玩家正式受伤动画
- 玩家正式死亡动画
- 玩家正式处决动作表现
- 玩家正式短冲刺动作、残影或位移特效
- 玩家统一角色帧动画正式重生成，需统一待机 / 跑步 / 跳跃 / 攻击 / 防御 / 处决的尺寸、基线和透视

### 装备资源

- 后续装备系统所需的独立武器模型资源
- 武器图标资源
- 武器掉落地面表现资源
- 装备栏 / 装备面板图标与装饰资源

### 敌人资源

- 基础敌人待机 / 巡逻动作
- 基础敌人攻击动画
- 基础敌人受击动画
- 基础敌人破绽状态表现
- 基础敌人死亡动画
- 后续新敌人的整套动作资源
- 敌人被处决时的专用表现
- 敌人掉落物图标资源
- 灵魂哨卫 / 小 Boss 的正式外观、攻击、受击、破绽和死亡资源

### 战斗特效资源

- 普通攻击命中特效
- 防御命中特效
- 精准弹反强反馈特效
- 架势打满 / 破绽状态特效
- 处决镜头与打击特效
- 短冲刺特效

### 双世界相关资源

- 世界切换音效
- 现实世界美术风格资源
- 灵魂世界美术风格资源
- 双世界差异机关资源
- 世界切换色差 / 波纹特效资源
- 界裂 / 界裂污染状态资源

### 关卡与场景资源

- 白盒关卡正式美术替换
- 长横向多层地图的场景拼接资源
- 背景层
- 前景遮挡
- 装饰物
- 互动提示图标
- 记忆碎片收集物图标与表现资源

### UI 资源

- 主菜单标题 Logo
- 设置菜单图标与分栏装饰资源
- 菜单按钮资源
- 提示框资源
- 敌人 HP 条与架势条正式样式
- 玩家 HP 条与耐力条正式样式
- 天赋树节点图标、连线样式、节点底盘和分支装饰资源
- 天赋树面板背景、边框和分类标识资源
- 装备栏、武器栏、装备详情面板正式样式

### 音频资源

- 普通攻击音效
- 受击音效
- 防御音效
- 弹反音效
- 架势打满音效
- 处决音效
- 跳跃音效
- 落地音效
- 世界切换音效
- 短冲刺音效
- 天赋解锁 / 退点音效
- 场景环境音
- BGM

## 最近更新

- 主菜单中央信息框改为半透明面板，按钮三态素材已裁切成统一尺寸并移除原始棋盘底，背景图现在可以正常透出
- 玩家 live 帧资源已按策划案方向重新生成 `reality / soul` 两套无武器主体动作，武器图片不再烘焙在角色帧里
- 玩家显示比例和武器挂点已同步到新 `128x160` 动作帧，装备系统后续可以继续通过 `WeaponMount` 替换武器模型
