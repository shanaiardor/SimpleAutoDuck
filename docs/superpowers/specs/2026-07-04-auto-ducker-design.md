# System-wide Audio Auto Ducker — 设计文档

- **日期**：2026-07-04
- **项目**：SimpleAutoDuck
- **状态**：草案 / 待评审
- **目标框架**：.NET Framework 4.7.2 / WinForms
- **音频库**：NAudio（封装 Windows Core Audio API / WASAPI）

## 1. 需求摘要

实现系统级、按进程的智能音量控制。当用户指定的"主应用"发出声音时，自动降低系统中其他应用的音量；主应用停止发声一段时间后，其他应用的音量平滑恢复。

**功能要点**：
- 主应用识别：列出当前活跃音频会话，用户勾选主应用
- 自动鸭子：主应用有音频输出 → 降低其他应用音量；停止一段时间后 → 平滑恢复
- 按应用粒度：每个进程一个会话，独立控制
- 可调参数：检测阈值、Duck Depth、Attack Time、Release Time、白名单/黑名单
- UI：WinForms 简洁界面 + 主应用列表 + 实时状态 + 滑块/数值输入 + 启停开关 + 保存配置
- 其它：Windows 10/11、托盘后台运行、低资源占用、长时稳定、全局热键开关

## 2. 关键技术决策

| 决策项 | 选定方案 | 理由 |
|---|---|---|
| .NET 版本 | 保持 .NET Framework 4.7.2 | 用户既有项目；无需额外运行时；NAudio 在 4.x 上成熟 |
| 音频 API 封装 | NAudio | 成熟稳定的 WASAPI 封装；COM 互操作代码量小 |
| 触发检测 | 电平阈值 + 双时间常数（防抖+释放延迟） | 过滤短暂杂音/语音间隙，听感稳定 |
| 音量控制 | `ISimpleAudioVolume::SetMasterVolume` 线性缩放 | 与系统音量滑块独立，不影响用户手动设置 |
| 平滑淡入淡出 | 定时器线性 Ramp（20–50ms 间隔） | 实现简单、CPU 低、听感足够平滑 |
| 主应用识别 | 进程会话列表勾选 + 刷新 | 简单实用，符合需求 4.3 |
| 首版范围 | 托盘 + 热键 + 配置全做 | 符合需求 4.3/4.4 |
| 架构组织 | 方案 A：分层 + UI 定时器驱动 | 单 Timer 驱动，无跨线程同步问题 |

## 3. 架构

### 3.1 总体结构

分层架构，UI 线程定时器驱动。所有音频操作在 UI 线程完成，避免跨线程 COM 访问。

```
SimpleAutoDuck/
├── Audio/                      # 音频引擎层（无 UI 依赖，可测试）
│   ├── AudioSessionManager.cs  # 枚举/订阅会话、会话增删通知
│   ├── SessionEnvelope.cs      # 单会话封装：电平采样+音量控制+Ramp 状态机
│   └── DuckEngine.cs           # 核心：轮询主应用电平、阈值/防抖判定、驱动 Ramp
├── Config/                     # 配置层
│   └── DuckConfig.cs           # 参数模型 + JSON 序列化
├── Hotkey/                     # 全局热键
│   └── GlobalHotkeyRegistrar.cs # RegisterHotKey 封装
└── UI/                         # WinForms
    ├── MainForm.cs / .Designer.cs
    └── TrayIcon.cs             # NotifyIcon + 上下文菜单
```

### 3.2 组件职责

**AudioSessionManager**
- 通过 `MMDeviceEnumerator` 获取默认渲染设备
- 通过 `AudioSessionManager2` 的 `Sessions` 枚举所有活跃会话
- 订阅 `SessionCreated` 事件，会话出现时通知 `DuckEngine`
- 提供 `Refresh()` 重新枚举当前会话
- 为每个会话创建 `SessionEnvelope` 包装
- 处理会话消失（`SessionEnded` 事件）的清理

**SessionEnvelope**（每个音频会话一个实例）
- 持有 `AudioSessionControl2` 引用
- 暴露 `ProcessId`、`ProcessName`、`DisplayName`、`IconPath`
- `GetPeakLevel()`：读取 `AudioMeterInformation.GetPeakValue()` 返回 0.0–1.0
- `Get/SetVolume()`：通过 `SimpleAudioVolume.SetMasterVolume` 控制会话音量
- `IsMainApp`：标记是否为主应用（由配置决定）
- `Ramp` 状态机：维护 `CurrentVolume` / `TargetVolume`，每个 Tick 按线性插值推进
- `UserVolume`：进入 ducking 时通过 `SimpleAudioVolume.GetMasterVolume` 读取的当前会话音量（默认 1.0，若用户在音量混合器中调过则为该值），恢复时回写此值

**DuckEngine**（核心，纯逻辑可测试）
- `Tick()`：被外部 Timer 调用，完成一次完整采样→判定→Ramp 下发
- 主应用活跃判定：
  - 任一主应用 `GetPeakLevel() >= Threshold` 持续 `HoldMs` → 状态 `Ducking`
  - 主应用全部低于阈值持续 `ReleaseDelayMs` → 状态 `Monitoring`
- 状态机：`Monitoring ⇄ Ducking`（含 `HoldTimer` 与 `ReleaseTimer` 内部计时）
- 进入 Ducking：所有非主应用 `UserVolume = GetMasterVolume()`，`TargetVolume = DuckDepth`
- 退出 Ducking：所有非主应用 `TargetVolume = UserVolume`（恢复）
- 每个 Tick 对所有会话调用 `SessionEnvelope.UpdateRamp(dtMs)`，实际音量下发由 Ramp 状态机按 `AttackMs`/`ReleaseMs` 线性插值完成
- 暴露事件 `StateChange`、`SessionListChange` 供 UI 订阅

**DuckConfig**
- 可序列化模型：
  - `Threshold` (0.0–1.0, 默认 0.02)
  - `DuckDepth` (0.0–1.0, 默认 0.3)
  - `AttackMs` (1–2000, 默认 200)
  - `ReleaseMs` (1–5000, 默认 800)
  - `HoldMs` (0–2000, 默认 50，进入 ducking 防抖)
  - `ReleaseDelayMs` (0–5000, 默认 300，退出 ducking 延迟)
  - `MainAppProcessNames` : `List<string>`
  - `BackgroundBlacklist` : `List<string>`（不参与 ducking 的非主应用）
  - `Hotkey` (字符串如 "Ctrl+Alt+D")
  - `Enabled` (bool)
- JSON 序列化到 `%AppData%\SimpleAutoDuck\config.json`
- 静态方法 `Load()` / `Save()`

**GlobalHotkeyRegistrar**
- 通过 `RegisterHotKey` (user32) 注册全局热键
- 重写 `WndProc` 接收 `WM_HOTKEY`
- 暴露 `HotkeyPressed` 事件
- 支持解析 "Ctrl+Alt+D" 格式字符串
- 退出时 `UnregisterHotKey`

**MainForm**
- 主应用列表：`CheckedListBox` 显示所有活跃会话的进程名，勾选即为主应用
- 刷新按钮：调用 `AudioSessionManager.Refresh()`
- 实时状态：`Label` 显示当前 `DuckingEngine.State`，`ProgressBar` 显示主应用电平
- 参数区：6 个 `TrackBar` + `NumericUpDown` 双控件对（阈值/深度/Attack/Release/Hold/ReleaseDelay）
- 启停开关：`CheckBox` 绑定 `DuckConfig.Enabled`
- 保存配置按钮
- `NotifyIcon` 最小化到托盘 + 右键菜单（显示/退出/启停）

## 4. 数据流

```
Timer(50ms) ─▶ DuckEngine.Tick()
                │
                ├─ 1. 采样：遍历主应用 SessionEnvelope.GetPeakLevel()
                ├─ 2. 判定：阈值+防抖 → 状态机状态
                ├─ 3. 设目标音量：非主应用 TargetVolume = (Ducking ? DuckDepth : UserVolume)
                └─ 4. Ramp：遍历所有 SessionEnvelope.UpdateRamp(dtMs)
                            └─ 线性插值 → SetMasterVolume (仅当值变化超过 0.001)
                │
                └─ 触发事件 StateChange → UI Invoke 更新状态显示
```

**会话发现流**（独立于 Tick）：
```
AudioSessionManager.SessionCreated 事件 ─▶ DuckEngine.OnSessionCreated
                                            └─ 创建 SessionEnvelope, 触发 SessionListChange
                                            └─ UI Invoke 刷新主应用列表
```

## 5. 状态机

`DuckEngine` 主状态：

```
       HoldMs 计满
 Monitoring ─────────▶ Ducking
    ▲                    │
    │ ReleaseDelayMs     │
    └─ ─── ── ── ── ── ─┘  (主应用电平持续低于阈值 ReleaseDelayMs)
```

- **Monitoring**：主应用未活跃，所有非主应用 `TargetVolume = UserVolume`（初始 1.0）
- **Ducking**：主应用活跃，所有非主应用 `TargetVolume = DuckDepth`
- **HoldTimer**：主应用电平刚达阈值但未持续 `HoldMs` 时，仍在 `Monitoring`，仅内部计时
- **ReleaseTimer**：主应用电平刚降阈值但未持续 `ReleaseDelayMs` 时，仍在 `Ducking`，仅内部计时

## 6. 错误处理

- **设备不可用 / COM 异常**：`AudioSessionManager.Refresh()` 捕获并记录日志，下次 Tick 重试；不崩溃进程
- **会话消失**：`SessionEnded` 事件触发清理 `SessionEnvelope`（含 Dispose），从 `DuckEngine` 移除
- **SetMasterVolume 失败**：捕获并忽略（应用可能已退出），下次采样时若 COM 对象释放则清理
- **配置文件损坏**：`DuckConfig.Load()` 捕获异常 → 使用默认配置 + 弹窗提示
- **热键注册失败**：弹窗提示"热键被占用"，但应用继续运行
- **NAudio 会话未启动**：启动时若无法获取默认设备，UI 显示"无音频设备"

## 7. 测试策略

**单元测试**（xUnit/NUnit，目标框架 net48；测试项目 `SimpleAutoDuck.Tests`）：

1. `DuckEngineStateTests`
   - 主应用电平持续 ≥ 阈值 HoldMs → 进入 Ducking
   - 主应用电平波动（短暂低于阈值）→ 保持 Ducking（释放延迟内）
   - 主应用电平持续 < 阈值 ReleaseDelayMs → 退出 Ducking
   - 阈值边界：等于阈值视为活跃

2. `RampTests`
   - Attack：target=0.3，attack=200ms，50ms tick 应前进 1/4
   - Release：target=1.0，release=800ms，50ms tick 应前进 1/16
   - 到达目标后稳定（不再 SetVolume）

3. `ConfigTests`
   - JSON 往返：保存后加载字段相等
   - 损坏 JSON → 默认值 + 异常标记

4. `HotkeyParseTests`
   - "Ctrl+Alt+D" → 正确的修饰符 + Keys.D

**手动验收测试**：
- 启动 Discord/微信语音 + 背景音乐，验证背景音自动降低/恢复
- 主应用列表刷新、勾选持久化
- 托盘最小化、热键 Ctrl+Alt+D 启停
- 长时运行（1 小时）无内存泄漏

## 8. 依赖与构建

- NuGet 包：`NAudio`（最新稳定版，兼容 net47）
- 测试 NuGet：`xunit`、`xunit.runner.visualstudio`（测试项目 net48）
- 构建命令：`msbuild SimpleAutoDuck.sln /p:Configuration=Release`

## 9. 非功能要求

- **性能**：50ms Tick 下 CPU < 1%；只对值变化超过 0.001 的会话下发音量
- **稳定**：所有 COM 调用包裹 try/catch，会话异常自动清理
- **资源**：UI 线程单 Timer，无后台线程；最小化到托盘时仍保持工作
- **兼容**：Windows 10/11（WASAPI 自 Vista 起可用，但本目标限定 Win10+）

## 10. 风险与缓解

| 风险 | 缓解 |
|---|---|
| 主应用产生持续低电平噪声导致一直 ducking | 释放延迟 + 阈值可调；用户可设较高阈值 |
| 用户在 ducking 期间手动调整系统音量 | `UserVolume` 仅在进入 ducking 时缓存，恢复时回该值；手动调整在非 ducking 期间不会被覆盖 |
| 同进程多会话（如浏览器多标签）| Windows 会话粒度为进程级，多标签共享同一会话音量；首版不做标签级区分 |
| 热键冲突 | 注册失败时提示用户重新配置 |