# NormalMaps

[SPT 4.1.3] 独立复刻增强版 DynamicMaps：战局小地图 + Peek 全景 + 11 张高清地图 + 全标记（自包含独立 mod）

适用：SPT 4.1.3 ｜ 自包含独立 mod（含地图数据与 SVG 依赖库，无需额外下载 DynamicMaps）

## 这是什么

NormalMaps 是原版 [DynamicMaps](https://github.com/samswat/DynamicMaps)（作者 mpstark，MIT）的独立复刻增强版：
0.3.4 引擎 + v1.2.1 地图数据/功能移植 + 增强功能 + 内置汉化，装一个包即可获得全部功能。
独立 GUID（`com.jeanlannce.normalmaps`），与原版 DynamicMaps（`com.mpstark.dynamicmaps`）互不冲突。

## 功能

### 1. 战局内小地图（MiniMap）
- 进战局自动在屏幕角落显示小地图，持续跟随玩家
- 小键盘 8 / 小键盘 5：放大 / 缩小小地图（主地图与小地图为两套独立缩放记忆，互不干扰）
- `End`：显示 / 隐藏小地图
- 位置、尺寸、偏移、缩放级别均可 F12 调整

### 2. Peek 全景模式（按 M）
- 按住 M 打开地图时显示整图全景，完整地图自动填满屏幕
- 小键盘 8 / 5：放大 / 缩小；方向键：平移地图
- 松开 M：恢复打开前的地图位置与缩放（小地图同时自动恢复）
- 全景缩放倍率可 F12 调整（默认 1.0 = 完整地图刚好填满屏幕）

### 3. 高清 SVG 矢量渲染（11 张地图，含 Labyrinth）
Customs / Factory / GroundZero / Interchange / Labs / Labyrinth / Lighthouse / Reserve / Shoreline / Streets / Woods
（地图数据来源：TarkovDev 与 TarkovData，各自遵循其原始许可，详见 `发布源/Maps/` 下的 LICENSE 文件）

### 4. 标记功能全量
玩家 / 友军 / 敌人 / Scav / Boss / 上锁门 / 任务 / 撤离点（状态着色）/ 背包 / BTR / 空投 / 尸体
v1.2.1 移植：愿望清单物品 / 隐藏仓库 / 转运点 / 秘密撤离点 / 直升机坠毁点

### 5. 纹理缓存清理（内存优化）
战局结束时自动清理地图纹理缓存（PNG/SVG 图层），长会话切多图时内存/显存不持续累积。

### 6. 内置汉化
界面文本与全部配置项均为中文。

## 安装（1 分钟）

1. 从 [Releases](../../releases) 下载 `NormalMaps_vX.Y.Z.zip`
2. 解压得到 `BepInEx`、`EscapeFromTarkov_Data` 两个文件夹
3. 拖入 SPT 游戏根目录覆盖合并（若已安装旧版 DynamicMaps，请先删除 `BepInEx\plugins\mpstark-dynamicmaps\` 及 `SPT_Runtime\user\mods\mpstark-dynamicmaps\`）
4. 重启游戏

> `EscapeFromTarkov_Data\Managed\` 下的 4 个 Unity 库（Unity.VectorGraphics.dll 及其依赖）是 SVG 地图渲染必需文件，干净安装时不可省略。

## 配置（F12）

所有配置通过游戏内 F12（BepInEx 配置管理器）调整，无需手动改文件：
- `[1. 通用设置]`：替换地图界面、方向键移动、缩放热键、移动速度、Y 轴反转
- `[2. 动态标记]`：全部标记类型开关
- `[3. 战局内]`：自动选层、自动居中、peek 快捷键、按住窥视、窥视全景缩放倍率、主地图缩放级别
- `[4. 标记颜色]`：战利品/秘密点/隐藏仓库/转运点颜色
- `[5. 小地图]`：启用、位置、尺寸、偏移、显示/隐藏热键、缩放热键、缩放级别

## 卸载

删除以下内容：
```
BepInEx\plugins\NormalMaps\            （mod 本体，整个文件夹）
EscapeFromTarkov_Data\Managed\        （仅删除 4 个 Unity 库；其他 mod 若在用请保留）
  Unity.VectorGraphics.dll
  Unity.Mathematics.dll
  Unity.Collections.dll
  Unity.Collections.LowLevel.ILSupport.dll
```

## 兼容性

- SPT 4.1.3（Tarkov 40743）
- 独立 GUID，与原版 DynamicMaps 不冲突，可共存
- 纯 BepInEx 插件，不修改任何游戏文件

## 构建

```bash
# 需要先准备 lib/（引用 SPT/EFT 程序集，版权原因不入库）
dotnet build -c Release
```

## 致谢

- 原版 [DynamicMaps](https://github.com/samswat/DynamicMaps) by mpstark（MIT）
- 地图数据：[TarkovDev](https://github.com/the-hideout/tarkov-dev)、[TarkovData](https://github.com/TarkovTracker/tarkovdata/)

## License

代码部分遵循原版 DynamicMaps 的 MIT License；地图数据遵循其各自来源的许可。
