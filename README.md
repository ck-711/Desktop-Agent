# Desktop Agent

一个运行在 Windows 桌面的本地 AI 虚拟角色。项目使用 Unity 制作界面与角色表现，并在本机加载 Qwen3、Qwen3-ASR 和 Qwen3-TTS 模型，实现文字对话、语音输入/输出、表情、动作和短期会话记忆。

> 本项目的推理在本地完成；首次启动时载入模型会需要一些时间。

## 功能

- 本地 Qwen3 文本生成，支持流式显示与“思考”内容隐藏。
- 本地语音识别和语音合成（Qwen3-ASR / Qwen3-TTS）。
- 根据模型输出的控制标签切换角色表情和动作。
- 可拖动的桌面角色与边缘菜单。
- 保留最近若干轮会话，并可在下次启动时恢复。
- Windows GPU 加速；不可用时相关组件会回退或可配置为 CPU 运行。

## 下载与运行

从 [Releases](https://github.com/ck-711/Desktop-Agent/releases) 下载最新 Windows 包。完整安装包体积较大，会被拆分为多个附件发布。

1. 下载同一版本的全部 `Desktop-Agent-windows-x64-*.tar` 文件。
2. 将每个 TAR 文件解压到同一个空目录。Windows 10/11 可在终端使用 `tar -xf <文件名>`，或用支持 TAR 的解压工具完成此操作。
3. 解压完成后运行 `Desktop Agent.exe`。

请勿单独移动 `Desktop Agent.exe`：它需要与 `Desktop Agent_Data`、`MonoBleedingEdge` 等同级目录共同存在。

## 系统要求

- Windows 10/11 x64。
- 建议配备 NVIDIA GPU；大模型、语音识别和语音合成会占用较多内存与显存。
- 有麦克风和扬声器/耳机以使用语音交互。
- Windows Defender 或其他安全软件可能在首次运行未签名的自建程序时弹出提示，请按你的安全策略处理。

## 使用方式

- 在输入框中输入内容并提交，与角色进行文字对话。
- 如场景中启用了语音输入，可使用麦克风进行语音交互。
- 角色会随模型返回的标签切换表情和动画；这些控制标签不会显示在对话文本里。
- 会话历史默认保存到应用的本地持久化目录，可在 Unity Inspector 中调整记忆轮数、长度和是否持久化。

## 从源码构建

1. 安装 Unity `6000.3.5f2`（Unity 6.3）及 Windows 构建支持模块。
2. 克隆仓库，并用 Unity Hub 打开项目根目录。
3. 等待 Package Manager 还原依赖；项目依赖包含 UniVRM、uLipSync、URP 和 Unity Input System。
4. 在 Unity 中打开 `Assets/Scenes/SampleScene.unity`，确认 `StreamingAssets/models` 中的模型文件和原生插件可用。
5. 通过 `File > Build Settings` 构建 Windows x86_64 版本。

构建包中的模型文件不纳入 Git 仓库（它们体积很大）。若从源码构建，请自行取得模型并放入 `Assets/StreamingAssets/models`，目录结构需与发布包中的 `Desktop Agent_Data/StreamingAssets/models` 保持一致。

`Assets/Plugins/x86_64/qwen3tts.dll` 同样不会提交到仓库；完整 Release 包已包含它。若从源码构建并需要语音合成功能，请自行将该原生库放回此路径。

## 项目结构

```text
Assets/
  Scenes/SampleScene.unity       主场景
  Scripts/                       对话、语音、角色控制与桌面交互脚本
  StreamingAssets/models/        本地模型（不提交到 Git）
Packages/                        Unity 包依赖
Build/                           本地构建输出（不提交到 Git）
```

## 依赖与致谢

- [Unity](https://unity.com/)
- [UniVRM](https://github.com/vrm-c/UniVRM)
- [uLipSync](https://github.com/hecomi/uLipSync)
- [MCP for Unity](https://github.com/CoplayDev/unity-mcp)
- [llama.cpp](https://github.com/ggml-org/llama.cpp)
- [Qwen](https://github.com/QwenLM)

请在再分发模型、素材或依赖前，分别阅读它们的许可证与使用条款。
