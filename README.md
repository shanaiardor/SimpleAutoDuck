# SimpleAutoDuck

> Windows 自动音频闪避（Auto-Ducking）工具 —— 当主应用（如语音通话、游戏语音）发声时，自动压低背景音量；安静后平滑恢复。

[![Build](https://img.shields.io/badge/build-Release-green)](SimpleAutoDuck/bin/Release) [![Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)](https://dotnet.microsoft.com/) [![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)](#)

---

## 简介 / Overview

SimpleAutoDuck 是一个轻量级的 Windows 桌面工具，基于 NAudio 监听系统音频会话，实时检测"主应用"（你指定的进程，如 Discord、游戏语音通道）的音频输出。一旦主应用发声超过阈值并持续设定时长，背景应用（音乐、浏览器、视频播放器）的音量会被平滑衰减到一个较低水平（duck depth）；主应用静默一段时间后，背景音量再以可配置的曲线恢复到原始水平。

典型使用场景：
- 游戏语音 / Discord 通话时自动压低背景音乐
- 直播 / 录制时自动让位给麦克风音频
- 播客后期监听时自动闪避背景 BGM
- 任何需要"主音频优先"的实时混音场景

## 功能特性 / Features

- **进程级会话识别**：通过 Windows 音频会话 API 枚举所有输出音频的进程，勾选哪些是"主应用"。
- **平滑斜坡控制**：Attack / Release 均为线性斜坡，无爆音、无突兀切换。
- **Hold / Release Delay 防抖**：主应用发声需持续 `HoldMs` 才进入闪避状态；静默需持续 `ReleaseDelayMs` 才恢复，避免短暂停顿导致频繁抖动。
- **5 个内置预设**：温和 / 标准 / 激进 / 强力压低 / 轻柔淡出，覆盖音乐、游戏语音、直播、播客等场景。
- **6 个可调参数**：阈值、Duck 深度、Attack、Release、Hold、释放延迟，全部滑块实时调整。
- **托盘常驻**：最小化到系统托盘，右键菜单可快速启用 / 禁用 / 退出。
- **全局热键**：默认 `Ctrl+Alt+D` 一键切换启用状态，可在配置中修改。
- **实时电平表**：主应用音频电平可视化，便于调整阈值。
- **配置持久化**：JSON 配置文件保存到 `%AppData%\SimpleAutoDuck\config.json`。
- **开源 + 单文件安装包**：提供 Inno Setup 安装程序，一键部署。

## 截图 / Screenshots

> 主界面（参数 / 预设 / 会话列表 / 状态电平表）
>
> _在此放入截图 `docs/screenshot-main.png`_

## 安装 / Installation

### 方式一：使用安装包（推荐普通用户）

1. 直接下载安装包：[**SimpleAutoDuckSetup-0.0.2.0.exe**](installer/Output)（约 2.2 MB）
2. 双击运行安装程序，按向导完成安装。
3. 安装后从开始菜单启动 `SimpleAutoDuck`，或勾选"开机启动"。

> 也可前往 [Releases](../../releases) 页面下载历史版本。

### 方式二：从源码构建

详见下方 [从源码构建](#从源码构建--build-from-source) 章节。

## 使用指南 / Usage

### 1. 启动

启动后主窗口显示四个分区：

| 区域 | 功能 |
|------|------|
| **主应用** | 列出当前所有输出音频的进程，勾选你希望作为"主应用"的进程（如 `Discord.exe`、`csgo.exe`） |
| **状态** | 显示当前是"监测中"还是"鸭子中"，并显示主应用的实时电平 |
| **参数** | 6 个滑块调节闪避行为 |
| **操作** | 启用开关、预设下拉、保存配置按钮 |

### 2. 选择主应用

点击 **刷新会话**，从列表勾选需要优先的进程（例如游戏、语音软件）。被勾选的进程发声时，其他未勾选的进程音量将被压低。

### 3. 启用

勾选 **启用自动鸭子**，或按全局热键 `Ctrl+Alt+D` 切换启用状态。托盘图标会反映当前启用状态。

### 4. 选择预设

如果不想手动调参，从 **预设** 下拉选择一个最贴近你场景的预设：

| 预设 | 适用场景 | 特点 |
|------|---------|------|
| 温和（音乐/视频） | 日常听歌看视频时被语音打断 | Duck 深度 0.5，曲线柔和 |
| 标准（默认） | 通用 | Duck 深度 0.3，平衡 |
| 激进（游戏语音） | FPS / MMO 团队语音 | 反应快、压得低 |
| 强力压低（直播） | 直播推流 | 几乎完全压低背景 |
| 轻柔淡出（播客） | 播客后期 / 录制 | 长斜坡，听感自然 |

### 5. 调参（可选）

| 参数 | 范围 | 含义 |
|------|------|------|
| **阈值** Threshold | 0.00 – 1.00 | 主应用电平达到此值才视为"发声" |
| **Duck 深度** DuckDepth | 0.00 – 1.00 | 闪避时背景音量目标值（0=静音，1=不变） |
| **Attack** | 1 – 2000 ms | 从原音量过渡到 Duck 深度的时间 |
| **Release** | 1 – 5000 ms | 从 Duck 深度恢复到原音量的时间 |
| **Hold** | 0 – 2000 ms | 进入闪避前主应用需持续发声的时间（防误触） |
| **释放延迟** ReleaseDelay | 0 – 5000 ms | 主应用静默后多久才恢复（防抖） |

### 6. 保存

点击 **保存配置**，参数写入 `%AppData%\SimpleAutoDuck\config.json`，下次启动自动加载。

### 7. 托盘行为

- 关闭主窗口 → 程序最小化到托盘继续运行
- 右键托盘图标 → 显示主窗口 / 启用-禁用 / 退出
- 全局热键 `Ctrl+Alt+D` → 一键切换启用状态

## 配置文件 / Configuration

路径：`%AppData%\SimpleAutoDuck\config.json`

```json
{
  "Threshold": 0.02,
  "DuckDepth": 0.3,
  "AttackMs": 200,
  "ReleaseMs": 800,
  "HoldMs": 50,
  "ReleaseDelayMs": 300,
  "Enabled": false,
  "Hotkey": "Ctrl+Alt+D",
  "MainAppProcessNames": ["Discord.exe"],
  "BackgroundBlacklist": []
}
```

支持的热键修饰键：`Ctrl` / `Control`、`Alt`、`Shift`、`Win`。键位支持单字符（`A`-`Z`、`0`-`9`）、`F1`-`F12`、`Space`、`Enter`、`Esc`、`Tab`、`Home`、`End`、`PgUp`、`PgDn`、`Ins`、`Del`。

`BackgroundBlacklist` 中的进程不会被闪避（例如系统通知音、关键提示音）。

## 从源码构建 / Build from Source

### 环境要求

- **Windows** 10 / 11（依赖 WASAPI）
- **Visual Studio 2022**（含 .NET 桌面开发工作负载）或单独安装 MSBuild
- **.NET Framework 4.7.2** 开发工具包（VS 安装器中勾选）
- **Inno Setup 6**（可选，仅生成安装包时需要）

### 构建

```powershell
# 使用 VS 2022 的 MSBuild（推荐，因为 dotnet CLI 无法处理 WinForms + .NET Framework）
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SimpleAutoDuck.sln -p:Configuration=Release -restore
```

产物位于 `SimpleAutoDuck\bin\Release\SimpleAutoDuck.exe`。

### 生成安装包

```powershell
# 自动版本 +1、Release 构建、Inno 打包、提交并推送当前分支和 tag
.\scripts\Package-Release.ps1

# 如果 Inno Setup 安装在非默认目录，可显式指定
.\scripts\Package-Release.ps1 -InnoPath "C:\Users\<你>\AppData\Local\Programs\Inno Setup 6"
```

安装包输出到 `installer\Output\SimpleAutoDuckSetup-1.0.0.0.exe`。

脚本会把 `0.0.2.0` 这类四段版本号递增为下一个补丁版本，例如 `0.0.3.0`，并同步更新 `AssemblyInfo.cs`、`installer.iss` 和 README 中的安装包文件名。新生成的 `installer\Output\SimpleAutoDuckSetup-版本.exe` 会随 release commit 一起提交并推送到 GitHub。默认要求工作区干净；如需在已有未提交改动上强制执行，可加 `-AllowDirty`。

## 测试 / Tests

测试项目 `SimpleAutoDuck.Tests` 使用 **xUnit 2.9**，覆盖：

- `DuckEngine` 状态机：Monitoring → Ducking 切换、Hold 防误触、Release Delay 防抖、阈值边界、Excluded 排除、Disabled 禁用
- `RampInterpolator` 斜坡插值
- `DuckConfig` 序列化 / 反序列化 / Clamp
- `HotkeyDefinition` 解析

```powershell
# 运行测试（需 VS Test Agent 或在 VS 中使用 Test Explorer）
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SimpleAutoDuck.sln -p:Configuration=Debug -restore
vstest.console.exe SimpleAutoDuck.Tests\bin\Debug\SimpleAutoDuck.Tests.dll
```

## 项目结构 / Project Structure

```
SimpleAutoDuck/
├── SimpleAutoDuck.sln
├── installer.iss                  # Inno Setup 安装脚本
├── SimpleAutoDuck/                # 主项目
│   ├── Program.cs                 # WinForms 入口
│   ├── DuckingState.cs            # 状态枚举
│   ├── Config/
│   │   └── DuckConfig.cs          # 配置 + 预设
│   ├── Audio/
│   │   ├── IDuckSession.cs        # 会话抽象
│   │   ├── SessionEnvelope.cs     # 会话封装
│   │   ├── AudioSessionManager.cs # NAudio 会话管理
│   │   ├── DuckEngine.cs          # 闪避引擎（状态机）
│   │   └── RampInterpolator.cs    # 斜坡插值
│   ├── Hotkey/
│   │   ├── HotkeyDefinition.cs    # 热键解析
│   │   └── GlobalHotkeyRegistrar.cs # RegisterHotKey P/Invoke
│   ├── UI/
│   │   ├── MainForm.cs            # 主窗体逻辑
│   │   ├── MainForm.Designer.cs   # 主窗体布局
│   │   └── TrayIcon.cs            # 托盘图标
│   └── Properties/
└── SimpleAutoDuck.Tests/          # xUnit 单元测试
```

## 架构说明 / Architecture

```
┌─────────────┐   每进程    ┌──────────────────┐
│ AudioSession │ ─────────▶ │ SessionEnvelope  │  (NAudio AudioSessionControl 封装)
│   Manager    │            │  - ProcessName   │
└─────────────┘            │  - GetPeakLevel  │
        ▲                    │  - Get/SetVolume │
        │ new session        │  - Snapshot      │
        │                    └──────────────────┘
        │                            │
        │                            ▼  (IDuckSession 列表)
┌─────────────┐            ┌──────────────────┐
│   MainForm   │ ◀─────────│   DuckEngine      │  每 50ms Tick 一次
│  (UI + Timer)│  状态/电平 │  - Monitoring     │
└─────────────┘            │  - Ducking        │
        │                    └──────────────────┘
        │ config                      ▲
        ▼                              │
┌─────────────┐            ┌──────────────────┐
│  DuckConfig  │ ────────▶ │   RampInterpolator│  线性斜坡
│  (JSON)      │  参数      └──────────────────┘
└─────────────┘
```

引擎是一个简单的两状态状态机：

- **Monitoring**：持续监听主应用峰值；超过阈值且累计达 `HoldMs` → 进入 **Ducking**
- **Ducking**：把所有非主、非排除会话以 Attack 曲线压向 `DuckDepth`；主应用静默累计达 `ReleaseDelayMs` → 返回 **Monitoring**，并以 Release 曲线恢复到用户原始音量

## 贡献 / Contributing

欢迎 Issue 和 PR！

1. Fork 本仓库
2. 新建分支：`git checkout -b feature/your-feature`
3. 提交修改：`git commit -m "feat: ..."`
4. 推送：`git push origin feature/your-feature`
5. 提交 Pull Request

请确保：
- 新增功能附带单元测试
- 提交前 `MSBuild` 构建通过、`xUnit` 测试全部通过
- 遵循现有代码风格（4 空格缩进、PascalCase 命名）

## 路线图 / Roadmap

- [ ] 多输出设备支持（当前仅默认 Render 端点）
- [ ] 排除列表 UI 编辑（当前仅可通过配置文件设置 `BackgroundBlacklist`）
- [ ] 自定义热键 UI（当前需编辑 config.json）
- [ ] 开机自启管理界面
- [ ] 日志面板 / 调试视图
- [ ] 国际化（i18n，目前 UI 仅简体中文）

## 许可证 / License

本项目基于 **MIT License** 开源。

依赖项：
- [NAudio](https://github.com/naudio/NAudio) — MIT License
- .NET Framework 4.7.2 — Microsoft EULA

## 致谢 / Acknowledgements

- [NAudio](https://github.com/naudio/NAudio) —— .NET 下最好的 Windows 音频库
- [Inno Setup](https://jrsoftware.org/isinfo.php) —— 强大且免费的 Windows 安装包制作工具
