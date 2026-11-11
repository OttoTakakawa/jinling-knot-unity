# 金陵绳结 · Jinling Knot

从 Godot 迁移到 Unity 的绳结节奏小游戏，以秦淮水岸为场景，通过续弯、穿织与轻点完成绳结。目前是持续迭代中的可玩原型，包含一个完整关卡。

## 运行

1. 使用 Unity Hub 添加此目录，安装 Unity **2022.3.62f2c1**（项目记录的编辑器版本）。
2. 等待资源导入，打开 `Assets/JinlingKnot/Scenes/JinlingKnot.unity`。
3. 点击 Play；在主菜单选择开始织结或观织演示。

macOS 构建：安装对应 Build Support，在编辑器菜单选择 **金陵绳结 → 构建 macOS 预览**。输出为 `Builds/JinlingKnot.app`。仓库不包含预编译游戏包和 Unity 缓存。

## 操作

| 输入 | 操作 |
| --- | --- |
| 左键 / Enter | 续弯、轻点 |
| ↑ / ↓ | 选择穿线方向 |
| 右键 | 自动选择正确穿线方向 |
| Esc | 暂停 / 继续 |
| R | 重开 |
| D | 自动演示 |

## 当前功能

- 主菜单、设置、绳形剪影网格选关、HUD、暂停、失败重试与结算。
- 按钮悬停与按压反馈；关卡进入和重试黑场转场，随后 3、2、1 倒计时。
- 完成状态与最佳成绩本地保存，自动演示不计入成绩。
- 卡通水体包含平面反射、吸收与散射近似、岸边及水面泡沫、船体涟漪。
- 绳头星芒、拖尾、命中光圈、穿线弧光，以及绳网格增量更新。

只有第一个绳形已接入真实关卡，其余格子是占位。建筑仍使用迁移的原始网格；建筑造型与绳材质还在迭代。缺少河床处使用设定水深，水体光学属于风格化近似。

## 目录

- `Assets/JinlingKnot`：玩法、场景、Shader、迁移资源与编辑器构建工具。
- `Assets/JinlingKnotUI`：UI 图片、字体、预制件和交互脚本。
- `Packages` / `ProjectSettings`：Unity 依赖与工程配置。
- `Tools/bake_shore_distance.py`：静态岸线距离烘焙工具，修改城市几何后运行 `python3 Tools/bake_shore_distance.py`。
- `SourceManifest.json`：原 Godot 迁移输入的内容哈希；运行项目不依赖原 Godot 工程。

## 验证

本地 macOS / Apple M4 已完成构建及无窗口流程验证：36 项玩法检查、UI 流程、选关与按钮事件、转场、倒计时顺序及计时冻结。自动演示完成得分 5000，包含 13 次续弯、13 次穿织、28 次轻点。这是本地验证记录，尚未配置 CI。

## 字体许可

Noto Sans CJK 的 SIL Open Font License 随字体保留在 `Assets/JinlingKnot/Resources/OFL.txt` 与 `Assets/JinlingKnotUI/Fonts/OFL.txt`。
