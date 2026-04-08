# Echo Space

Godot 4.6.2 + C# 的 2D 平台动作原型骨架，核心围绕“现实 / 灵魂”双世界切换展开。

## 当前已落地

- 玩家基础移动：左右移动、跳跃、空中下落、基础攻击状态
- 手感底座：输入缓冲、Coyote Time、可调重力曲线
- 状态机框架：玩家已接入，敌人预留同构入口
- 双世界系统：`WorldManager` + `DualWorldObject`
- 对象池底座：`PoolManager` + `NodePool`
- 原型主场景：可直接运行，包含世界切换演示平台

## 目录结构

- `Scenes/`: 主场景与玩家原型
- `Scripts/Core/`: FSM、输入缓冲、对象池、世界管理
- `Scripts/Player/`: 玩家控制器与状态
- `Scripts/Gameplay/`: 战斗接口与敌人占位框架
- `Scripts/UI/`: 世界提示 UI

## 默认按键

- `A` / `Left`: 向左移动
- `D` / `Right`: 向右移动
- `Space` / `W` / `Up`: 跳跃
- `J` / `K`: 攻击
- `Tab`: 切换世界

## 运行

```powershell
dotnet build EchoSpace.csproj
```

然后用 Godot 4.6.2 Mono 打开项目，或直接运行主场景 `res://Scenes/Main.tscn`。

## 下一步建议

1. 给 `PlayerAttackState` 接入真正的命中盒与伤害结算。
2. 将 `DualWorldObject` 扩展为可破坏墙、按钮门、移动平台三类具体玩法对象。
3. 在敌人侧复用 `StateMachine<TContext>`，先做一个巡逻/追击/攻击三态近战敌人。
