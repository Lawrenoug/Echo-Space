# Echo Space

`Echo Space` 是一个使用 `Godot 4.6.2 + C#` 开发的 2D 横版动作游戏原型。当前项目重点不是一次性堆完整内容，而是先把双世界切换、平台探索、战斗循环和可扩展系统框架做扎实。

## README 维护规则

- 每次有代码、场景或系统配置文件修改时，都要同步检查并更新 `README`
- 每次修改文件后，都要提交版本；如果仓库状态允许，也同步推送到 GitHub
- `README` 不再维护容易快速过期的“细碎已完成流水账”
- 重点保留“当前框架说明”“关键文件位置”“当前最适合继续推进的方向”“人工资源填充清单”
- 正式策划案统一维护在 [GameDesignDocument.md](/F:/Godot%20project/echo-space/GameDesignDocument.md) 和 [Docs/GameDesignDocument.md](/F:/Godot%20project/echo-space/Docs/GameDesignDocument.md)

## 当前项目方向

- 核心玩法：现实世界 / 灵魂世界实时切换
- 关卡结构：长横向、多层白盒地图，强调探索和折返验证
- 战斗方向：以“血量 + 耐力 + 架势 + 处决”为原型的近战系统
- 开发方式：先完成可玩的系统闭环，再逐步接入正式美术、音效和内容

## 当前默认按键

- `A` / `Left`：向左移动
- `D` / `Right`：向右移动
- `Space` / `W` / `Up`：跳跃
- `鼠标左键`：普通攻击 / 处决
- `鼠标右键`：防御 / 弹反
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
- HUD 与系统界面：[Scripts/UI/WorldOverlay.cs](/F:/Godot%20project/echo-space/Scripts/UI/WorldOverlay.cs)
- 背包系统：[Scripts/Gameplay/Inventory](/F:/Godot%20project/echo-space/Scripts/Gameplay/Inventory)
- 加点系统：[Scripts/Gameplay/Progression](/F:/Godot%20project/echo-space/Scripts/Gameplay/Progression)
- 项目正式策划案：[Docs/GameDesignDocument.md](/F:/Godot%20project/echo-space/Docs/GameDesignDocument.md)

## 当前框架说明

- 启动入口是独立主菜单场景，不和游戏主关卡混在一起
- 当前存档系统已移除，后续等关卡、敌人、双世界状态和 UI 结构更稳定后再决定是否重做
- “继续游戏”入口当前暂不接回，后续和存档系统一并恢复
- 主菜单点击开始游戏时，会重置当前原型里的世界状态、背包和加点数据，再进入白盒关卡
- 背包、加点等二级界面使用统一的“面板栈”逻辑：切换时下层界面保留，关闭顶层后恢复下层
- 玩家当前具备血量、耐力、普通攻击、防御、弹反、处决、轻按低跳 / 长按高跳
- 敌人当前具备血量、架势、受击、破绽、处决、所属世界判定
- 敌人运行时位置已经和双世界静态位置刷新解耦，切换世界时不会再因为 `DualWorldObject` 被写回出生点
- 当前白盒关卡已经扩成一段可从左到右连续推进的长横向路线，串起跳跃、战斗、拾取、加点、双世界切换和处决验证

## 当前白盒关卡结构

当前主关卡已经接入第一批双世界机关模板，并按一条连续路线布置：

1. 起点教学段：基础移动、跳跃、拾取与第一只巡逻敌人
2. 早期平台段：用多层平台验证跳跃手感与横向移动
3. 单世界平台段：通过现实平台和灵魂桥验证“切世界找路”
4. 按钮门段：踩下按钮后打开前方门，验证机关联动
5. 差异运动平台段：平台在现实世界静止，在灵魂世界移动，用于跨越障碍
6. 终段爬升：结合单世界平台、灵魂敌人与最终目标点完成收尾

## 当前双世界机关模板

- 按钮门：`WorldButtonSwitch + WorldGate`
- 单世界平台：`DualWorldPlatform`
- 差异运动平台：`DifferentialMovingPlatform`
- 可破坏墙：`BreakableWall`

对应场景与脚本：

- [Scenes/Environment/WorldButtonSwitch.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldButtonSwitch.tscn)
- [Scenes/Environment/WorldGate.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/WorldGate.tscn)
- [Scenes/Environment/DualWorldPlatform.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/DualWorldPlatform.tscn)
- [Scenes/Environment/DifferentialMovingPlatform.tscn](/F:/Godot%20project/echo-space/Scenes/Environment/DifferentialMovingPlatform.tscn)

## 当前最适合继续推进的方向

建议下一步优先从下面这些方向里选：

1. 战斗验证：基于这张新白盒图反复测试普通攻击、弹反、架势打满、破绽提示、处决和敌人掉落是否顺畅
2. 机关扩展：继续补第二批双世界机关，例如按钮反向门、双状态升降台、灵魂世界可穿透平台、连续切换谜题段
3. 掉落闭环：让敌人稳定掉落可拾取物，把战斗、背包、加点串成一个完整循环
4. 内容扩展：增加第三种敌人或第一个小 Boss，验证敌人战斗基类在更复杂行为上的复用性
5. 能力原型：开始做一个新的探索能力，例如灵魂牵引、短冲刺或垂直位移，用来验证后续 Metroidvania 式回收路线
6. 白盒细化：继续优化关卡节奏，把当前长横向路线压成更像 3-5 分钟教学关的体验

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
- 场景环境音
- BGM
