# Conan Desktop Assistant · 公开源码版

Windows 桌面宠物与监控助手的可公开源码快照。当前产品版本见 [PUBLIC_SOURCE.json](PUBLIC_SOURCE.json)。

## 包含什么

- WPF/.NET Framework 桌宠框架、动画调度、通知气泡与侦探工作台。
- CPU/GPU/内存/网速监控、任务栏显示、历史统计与配色编辑。
- 计时提醒、Codex 状态与额度接入、本机授权保护等实现。
- 经过公开清单审核的测试、辅助脚本及第三方许可说明。

## 素材与运行边界

**本仓库没有角色图集、动漫原声、背景图、图标、运行包或任何个人配置。** 这是源码版，不能下载后直接双击运行。当前程序启动仍要求本机提供兼容图集；没有图集会提示缺失。

- 主图集文件名为 `conan.png`，1536×2288 像素，8列×11行，每格192×208；请使用有权使用的自备素材。
- 追加动画、台词、语音目录和可选组件按源码定义自行配置。此仓库不提供素材下载或提取脚本。
- 私有授权、密钥、通知内容、会话日志与流量历史均应保留本机，不要提交。

## 构建

在 Windows x64 的 PowerShell 中运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File work\offline-pet\build.ps1
```

使用 Windows 自带的 .NET Framework C# 编译器，无需 NuGet。公开构建脚本可在没有角色素材和驱动文件时编译源码；产物位于 `work/offline-pet/dist`。**编译成功不等于素材齐全或功能已经授权。** 运行前需把自备图集放入产物目录。限速、温度采集与系统通知属于可选组件，仍需各自的依赖和本机授权；不会在构建时联网下载、安装或加载驱动。

基础逻辑测试例如：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File work\offline-pet\tests\bubble-test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File work\offline-pet\tests\assistant-test.ps1
```

许多原生UI/动画测试需要本机素材，公开快照不宣称这些测试可在无素材环境全部通过。

## 反馈与同步

欢迎通过 Issues 提交问题与建议。此仓库是由完整开发仓库按文件清单生成的独立快照，不包含私有提交历史。`PUBLIC_SOURCE.json` 记录来源提交和逐文件校验。直接修改公开仓库会使自动同步停止并要求维护者处理，避免覆盖独立贡献。

未为本项目新增统一开源许可证；第三方部分请保留并遵守各自已有许可说明。本项目为个人桌面助手项目，与角色权利方没有官方关联。
