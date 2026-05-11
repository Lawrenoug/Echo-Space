# Echo Space

`Echo Space` 是一个使用 `Godot 4.6.2 + C#` 开发的 2D 横版动作游戏原型。当前阶段的重点不是一次性把内容堆满，而是先把双世界切换、白盒关卡、战斗循环、探索能力和可扩展系统框架做扎实。

## README 维护规则

- 每次有代码、场景或系统配置文件修改时，都要同步检查并更新 `README`
- 每次修改文件后，都要提交版本；仓库状态允许时同步推送到 GitHub
- `README` 不再维护容易快速过期的零散流水账
- 重点保留“当前框架说明”“关键文件位置”“当前最适合继续推进的方向”“人工资源填充清单”
- 正式策划案统一维护在 [GameDesignDocument.md](/F:/Godot%20project/echo-space/GameDesignDocument.md) 和 [Docs/GameDesignDocument.md](/F:/Godot%20project/echo-space/Docs/GameDesignDocument.md)

## 当前项目方向

- 核心玩法：现实世界 / 灵魂世界实时切换
- 关卡结构：长横向、多层白盒地图，强调探索、回收路线和连续切世界
- 战斗方向：以“血量 + 耐力 + 架势 + 处决”为原型的近战系统
- 开发方式：先做稳定可玩的玩法闭环，再逐步接入正式美术、音效和内容

## 当前默认按键

- `A` / `Left`：向左移动
- `D` / `Right`：向右移动
- `Space` / `W` / `Up`：跳跃
- `鼠标左键`：普通攻击 / 处决
- `鼠标右键`：防御 / 弹反
- `Q`：灵魂牵引
- `Tab`：切换世界
- `I`：打开 / 关闭背包
- `P`：打开 / 关闭加点界面
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
- 敌人战斗基类：[Scripts/Gameplay/Enemies/EnemyCombatant.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Enemies/EnemyCombatant.cs)
- 巡逻敌人：[Scripts/Gameplay/Enemies/EnemyController.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Enemies/EnemyController.cs)
- 追击敌人：[Scripts/Gameplay/Enemies/ChaserEnemyController.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Enemies/ChaserEnemyController.cs)
- 灵魂哨卫：[Scripts/Gameplay/Enemies/SoulSentinelEnemyController.cs](/F:/Godot%20project/echo-space/Scripts/Gameplay/Enemies/SoulSentinelEnemyController.cs)
- 白盒环境与机关脚本目录：[Scripts/Gameplay/Environment](/F:/Godot%20project/echo-space/Scripts/Gameplay/Environment)
- 拾取与背包系统：[Scripts/Gameplay/Inventory](/F:/Godot%20project/echo-space/Scripts/Gameplay/Inventory)
- 加点系统：[Scripts/Gameplay/Progression](/F:/Godot%20project/echo-space/Scripts/Gameplay/Progression)
- HUD 与系统界面：[Scripts/UI/WorldOverlay.cs](/F:/Godot%20project/echo-space/Scripts/UI/WorldOverlay.cs)

## 当前框架说明

- 启动入口是独立主菜单场景，不和游戏主关卡混在一起
- 当前存档系统已移除，等关卡、敌人、双世界状态和 UI 结构更稳定后再决定是否重做
- “继续游戏”入口当前暂不接回，后续和存档系统一并恢复
- 主菜单点击开始游戏时，会重置当前原型里的世界状态、背包和加点数据，再进入白盒关卡
- 背包、加点等二级界面使用统一的“面板栈”逻辑：切换时下层界面保留，关闭顶层后恢复下层
- 玩家当前具备血量、耐力、普通攻击、防御、弹反、处决、轻按低跳 / 长按高跳，以及 `灵魂牵引`
- 敌人当前具备血量、架势、受击、破绽、处决、所属世界判定，以及稳定掉落原型
- 敌人运行时位置已经和双世界静态位置刷新解耦，切换世界时不会再因为 `DualWorldObject` 被写回出生点
- 当前白盒关卡已经扩成一段更长的连续路线，串起跳跃、战斗、拾取、加点、双世界切换、双世界机关和探索能力验证

## 当前白盒关卡结构

当前主关卡已经接入两批双世界机关模板，并按一条连续路线布置：

1. 起点教学段：基础移动、跳跃、拾取与第一只巡逻敌人
2. 早期平台段：多层平台验证跳跃手感和横向移动
3. 单世界平台段：通过现实平台和灵魂桥验证“切世界找路”
4. 按钮门段：踩下按钮后打开前方门，验证机关联动
5. 差异运动平台段：平台在现实世界静止，在灵魂世界移动，用于跨越障碍
6. 第二批机关段：加入反向门、双状态升降台、灵魂世界可穿透平台
7. 连续切换谜题段：要求玩家在现实 / 灵魂之间连续切换，配合平台、按钮和牵引锚点完成推进
8. 终段收尾：完成灵魂牵引验证后切回现实，抵达最终目标点

## 当前双世界机关模板

- 按钮门：`WorldButtonSwitch + WorldGate`
- 反向门：`WorldGate(OpenWhenDeactivated = true)`
- 单世界平台：`DualWorldPlatform`
- 灵魂世界可穿透平台：`DualWorldPlatform(OneWayCollision = true, ExistsInSoul = true)`
- 差异运动平台：`DifferentialMovingPlatform`
- 双状态升降台：`WorldStateLift`
- 可破坏墙：`BreakableWall`
- 灵魂牵引锚点：`SoulTetherAnchor`

对应场景与脚本：

- [Scenes/Environment/WorldButtonSwitch.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldButtonSwitch.tscn)
- [Scenes/Environment/WorldGate.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldGate.tscn)
- [Scenes/Environment/DualWorldPlatform.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/DualWorldPlatform.tscn)
- [Scenes/Environment/DifferentialMovingPlatform.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/DifferentialMovingPlatform.tscn)
- [Scenes/Environment/WorldStateLift.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldStateLift.tscn)
- [Scenes/Environment/SoulTetherAnchor.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/SoulTetherAnchor.tscn)

## 当前掉落与成长闭环

- 敌人死亡后会稳定掉出可拾取的掉落物，而不是后台直接加背包
- 巡逻敌人会掉落基础恢复物
- 追击敌人会掉落可转化为加点收益的 `Memory Shard`
- 灵魂哨卫会掉落更高收益的 `Soul Cluster`
- 掉落物拾取后进入背包，玩家可以再通过背包使用它们，把战斗、拾取、背包、加点真正串成闭环

## 当前最适合继续推进的方向

建议下一步优先从下面这些方向里选：

1. 白盒实跑与修关：沿着现在这条新路线完整跑图，检查哪些跳跃点、按钮位置、切世界时机和牵引锚点距离还不顺
2. 机关细化：继续补更多可复用机关，例如双按钮组合门、世界专属落桥、可反复切换的时间差机关
3. 牵引能力扩展：给灵魂牵引补冷却反馈、锚点筛选规则、特殊锚点类型，正式验证回收路线设计空间
4. 掉落内容扩展：补第一批真正有区别的消耗品、材料和成长道具，而不是只用原型数值物品
5. 战斗内容扩展：增加第三种敌人或第一个小 Boss，验证现有掉落闭环和敌人基类能否继续复用
6. 白盒关卡压缩：把当前长路线整理成更像 3-5 分钟教学关的节奏，形成更清晰的起承转合

## 人工资源填充清单

以下内容默认需要你后续手工填充。  
这部分是长期保留区：

- 后续无论我怎样更新 `README`，都必须保留这块“人工资源填充清单”，如果有新增资源需求，在这里追加，删除已经完成的功能。

### 角色资源

- 玩家待机动画
- 玩家跑步动画
- 玩家跳跃动画
- 玩家下落动画
- 玩家攻击动画
- 玩家防御动作
- 玩家弹反动作或特效表现
- 玩家受伤动画
- 玩家死亡动画
- 玩家处决动作表现
- 玩家灵魂牵引动作与牵引中表现

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

### 双世界相关资源

- 世界切换音效
- 现实世界美术风格资源
- 灵魂世界美术风格资源
- 双世界差异机关资源
- 世界切换色差 / 波纹特效资源
- 灵魂牵引锚点与牵引轨迹特效资源
- 界碑 / 界碑污染状态资源

### 关卡与场景资源

- 白盒关卡正式美术替换
- 长横向多层地图的场景拼接资源
- 背景层
- 前景遮挡
- 装饰物
- 互动提示图标
- 记忆碎片收集物图标与表现资源

### UI 资源

- 主菜单背景资源
- 主菜单标题 Logo
- 主菜单按钮默认 / 悬停状态素材
- 设置菜单图标与分栏装饰资源
- 菜单按钮资源
- 提示框资源
- 敌人 HP 条与架势条正式样式
- 玩家 HP 条与耐力条正式样式

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
- 灵魂牵引音效
- 场景环境音
- BGM
