# DD2 Character Viewer

暗黑地牢 2 角色动画查看器：加载全部 16 个英雄的模型与动画，支持自由视角、动画播放与搜索过滤。

## 功能

- 16 个英雄可切换（含亚种：畸变体兽形/人形）
- 每条动画可单独播放、调速（0.05x–2x）、暂停、重播
- 左右两列搜索过滤（角色名 / 动画名 / 状态名）
- 自由视角：左键旋转、右键平移、滚轮缩放、F 复位、Tab 隐藏 UI
- 黑色背景 + 三点布光（主光暖、补光冷、轮廓光）

## 环境

- Unity 2022.3.16f1（内置渲染管线）
- Windows 独立程序

## 使用

1. 把MODDING TOOL安装目录下的 `Assets/Data/Characters/Heroes/` 整个复制到 `Assets/Heroes/`
2. 打开 Unity 工程
3. 菜单 `Viewer → Setup All Heroes`（扫描角色 + 修材质 shader）
4. 菜单 `Viewer → Build EXE`
5. 产物在 `Downloads/DD2Viewer/DD2Viewer.exe`

## 说明

游戏资源（模型 FBX、贴图、动画）不包含在本仓库中，需从自己的MODDING TOOL安装目录提取。
脚本与场景配置均在此仓库内，可直接复用。

## 实现要点

- 模型的 SMR 默认使用 `Default-Material`（FBX 无材质引用），需运行时按 `msh_xxx → mat_xxx` 匹配赋材质
- DD2 原材质用 `Red Hook/Lit/Hero` shader，新工程里替换为内置 `Standard`，贴图从 `_Base` 迁移到 `_MainTex`
- 播放时关闭 `Animator.enabled`，改用 `clip.SampleAnimation()` 每帧强制采样，避免状态机覆盖
