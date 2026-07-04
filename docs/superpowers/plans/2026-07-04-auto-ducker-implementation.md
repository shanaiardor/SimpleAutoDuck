# System-wide Audio Auto Ducker 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 .NET Framework 4.7.2 WinForms 项目中用 NAudio 封装 WASAPI 实现按进程的自动 ducking。

**Architecture:** 分层架构（Audio/Config/Hotkey/UI），UI 线程单 Timer(50ms) 驱动 `DuckEngine.Tick()` 完成采样→阈值防抖判定→Ramp 下发。纯逻辑部分（状态机/Ramp/Config/热键解析）走 TDD，COM/UI 层走完整实现 + 构建验证 + 手动验收。

**Tech Stack:** .NET Framework 4.7.2, WinForms, NAudio (最新稳定版), xUnit (net48 测试项目), Newtonsoft.Json 或 DataContractJsonSerializer。

**Spec:** `docs/superpowers/specs/2026-07-04-auto-ducker-design.md`

---

## 文件结构

```
SimpleAutoDuck/
├── Audio/
│   ├── SessionEnvelope.cs       # 单会话封装：电平采样+音量控制+Ramp
│   ├── AudioSessionManager.cs   # 枚举/订阅会话
│   └── DuckEngine.cs            # 核心状态机（纯逻辑可测试部分抽离）
├── Config/
│   └── DuckConfig.cs           # 参数模型 + JSON 序列化
├── Hotkey/
│   ├── HotkeyDefinition.cs     # 热键解析（纯逻辑，可测试）
│   └── GlobalHotkeyRegistrar.cs # RegisterHotKey 封装
├── UI/
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   └── TrayIcon.cs
├── DuckingState.cs             # 状态枚举（共享）
└── Program.cs (修改)
SimpleAutoDuck.Tests/           # 新建测试项目
├── SimpleAutoDuck.Tests.csproj
├── DuckEngineTests.cs
├── RampTests.cs
├── ConfigTests.cs
└── HotkeyDefinitionTests.cs
```

---

## Task 0: 测试项目骨架 + NAudio 安装

**Files:**
- Create: `SimpleAutoDuck.Tests/SimpleAutoDuck.Tests.csproj`
- Modify: `SimpleAutoDuck.sln`
- Modify: `SimpleAutoDuck/SimpleAutoDuck.csproj`

- [ ] **Step 1: 在主项目 csproj 中添加 NAudio NuGet 引用**

编辑 `SimpleAutoDuck/SimpleAutoDuck.csproj`，在 `</ItemGroup>` 后追加 packages.config 路径或直接 PackageReference。本项目用 packages.config 方式（net47 常规）。

实际操作：用 `dotnet add SimpleAutoDuck/SimpleAutoDuck.csproj package NAudio` 或手动创建 `SimpleAutoDuck/packages.config`：
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="NAudio" version="2.2.1" targetFramework="net472" />
  <package id="NAudio.Core" version="2.2.1" targetFramework="net472" />
  <package id="NAudio.Wasapi" version="2.2.1" targetFramework="net472" />
  <package id="NAudio.WinMM" version="2.2.1" targetFramework="net472" />
</packages>
```
并在 csproj `<ItemGroup>` 添加 References 指向包路径。

**为简化并避免 MSBuild packages.config 复杂路径问题，本计划改用 NuGet `PackageReference` 方式：**

编辑 `SimpleAutoDuck/SimpleAutoDuck.csproj`，在第一个 `</PropertyGroup>` 后插入：
```xml
<PropertyGroup>
  <RestorePackages>true</RestorePackages>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="NAudio" Version="2.2.1" />
</ItemGroup>
```

- [ ] **Step 2: 创建测试项目 `SimpleAutoDuck.Tests/SimpleAutoDuck.Tests.csproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>SimpleAutoDuck.Tests</RootNamespace>
    <AssemblyName>SimpleAutoDuck.Tests</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <Deterministic>true</Deterministic>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SimpleAutoDuck\SimpleAutoDuck.csproj">
      <Project>{827C525E-2830-4898-B630-2E62E75070DA}</Project>
      <Name>SimpleAutoDuck</Name>
    </ProjectReference>
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

- [ ] **Step 3: 把测试项目加入 sln**

用 `dotnet sln SimpleAutoDuck.sln add SimpleAutoDuck.Tests/SimpleAutoDuck.Tests.csproj`。

- [ ] **Step 4: 还原包并构建**

Run: `dotnet restore SimpleAutoDuck.sln`
Run: `dotnet build SimpleAutoDuck.sln /p:Configuration=Debug`
Expected: 两个项目构建成功（Form1 仍为空模板）。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "build: add NAudio package and xUnit test project"
```

---

## Task 1: 共享状态枚举

**Files:**
- Create: `SimpleAutoDuck/DuckingState.cs`

- [ ] **Step 1: 创建状态枚举**

```csharp
namespace SimpleAutoDuck
{
    public enum DuckingState
    {
        Monitoring,
        Ducking
    }
}
```

- [ ] **Step 2: 提交**

```bash
git add SimpleAutoDuck/DuckingState.cs
git commit -m "feat: add DuckingState enum"
```

---

## Task 2: DuckConfig + JSON 序列化（TDD）

**Files:**
- Create: `SimpleAutoDuck/Config/DuckConfig.cs`
- Test: `SimpleAutoDuck.Tests/ConfigTests.cs`

- [ ] **Step 1: 写失败测试**

`SimpleAutoDuck.Tests/ConfigTests.cs`:
```csharp
using System;
using System.IO;
using SimpleAutoDuck.Config;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void Defaults_AreSensible()
        {
            var cfg = new DuckConfig();
            Assert.Equal(0.02, cfg.Threshold, 4);
            Assert.Equal(0.3, cfg.DuckDepth, 4);
            Assert.Equal(200, cfg.AttackMs);
            Assert.Equal(800, cfg.ReleaseMs);
            Assert.Equal(50, cfg.HoldMs);
            Assert.Equal(300, cfg.ReleaseDelayMs);
            Assert.False(cfg.Enabled);
            Assert.Equal("Ctrl+Alt+D", cfg.Hotkey);
            Assert.Empty(cfg.MainAppProcessNames);
            Assert.Empty(cfg.BackgroundBlacklist);
        }

        [Fact]
        public void Clamp_KeepsValuesInRange()
        {
            var cfg = new DuckConfig { Threshold = -1, DuckDepth = 5, AttackMs = -10, ReleaseMs = 99999 };
            cfg.Clamp();
            Assert.Equal(0, cfg.Threshold, 4);
            Assert.Equal(1, cfg.DuckDepth, 4);
            Assert.Equal(1, cfg.AttackMs);
            Assert.Equal(5000, cfg.ReleaseMs);
        }

        [Fact]
        public void Json_RoundTrip_PreservesAllFields()
        {
            var cfg = new DuckConfig
            {
                Threshold = 0.05,
                DuckDepth = 0.4,
                AttackMs = 150,
                ReleaseMs = 1000,
                HoldMs = 100,
                ReleaseDelayMs = 500,
                Enabled = true,
                Hotkey = "Ctrl+Shift+M",
                MainAppProcessNames = { "discord.exe", "WeChat.exe" },
                BackgroundBlacklist = { "explorer.exe" }
            };
            var json = cfg.ToJson();
            var loaded = DuckConfig.FromJson(json);
            Assert.Equal(cfg.Threshold, loaded.Threshold, 4);
            Assert.Equal(cfg.DuckDepth, loaded.DuckDepth, 4);
            Assert.Equal(cfg.AttackMs, loaded.AttackMs);
            Assert.Equal(cfg.ReleaseMs, loaded.ReleaseMs);
            Assert.Equal(cfg.HoldMs, loaded.HoldMs);
            Assert.Equal(cfg.ReleaseDelayMs, loaded.ReleaseDelayMs);
            Assert.Equal(cfg.Enabled, loaded.Enabled);
            Assert.Equal(cfg.Hotkey, loaded.Hotkey);
            Assert.Equal(cfg.MainAppProcessNames, loaded.MainAppProcessNames);
            Assert.Equal(cfg.BackgroundBlacklist, loaded.BackgroundBlacklist);
        }

        [Fact]
        public void FromJson_Corrupt_ReturnsDefault()
        {
            var loaded = DuckConfig.FromJson("not json {{{");
            Assert.Equal(new DuckConfig().Threshold, loaded.Threshold, 4);
        }
    }
}
```

- [ ] **Step 2: 运行测试验证失败**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 编译失败（DuckConfig 不存在）。

- [ ] **Step 3: 实现 DuckConfig**

`SimpleAutoDuck/Config/DuckConfig.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SimpleAutoDuck.Config
{
    [DataContract]
    public class DuckConfig
    {
        [DataMember] public double Threshold { get; set; } = 0.02;
        [DataMember] public double DuckDepth { get; set; } = 0.3;
        [DataMember] public int AttackMs { get; set; } = 200;
        [DataMember] public int ReleaseMs { get; set; } = 800;
        [DataMember] public int HoldMs { get; set; } = 50;
        [DataMember] public int ReleaseDelayMs { get; set; } = 300;
        [DataMember] public bool Enabled { get; set; } = false;
        [DataMember] public string Hotkey { get; set; } = "Ctrl+Alt+D";
        [DataMember] public List<string> MainAppProcessNames { get; set; } = new List<string>();
        [DataMember] public List<string> BackgroundBlacklist { get; set; } = new List<string>();

        public void Clamp()
        {
            Threshold = Clamp(Threshold, 0, 1);
            DuckDepth = Clamp(DuckDepth, 0, 1);
            AttackMs = ClampInt(AttackMs, 1, 2000);
            ReleaseMs = ClampInt(ReleaseMs, 1, 5000);
            HoldMs = ClampInt(HoldMs, 0, 2000);
            ReleaseDelayMs = ClampInt(ReleaseDelayMs, 0, 5000);
        }

        private static double Clamp(double v, double min, double max) =>
            v < min ? min : (v > max ? max : v);
        private static int ClampInt(int v, int min, int max) =>
            v < min ? min : (v > max ? max : v);

        public string ToJson()
        {
            var ser = new DataContractJsonSerializer(typeof(DuckConfig));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, this);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static DuckConfig FromJson(string json)
        {
            try
            {
                var ser = new DataContractJsonSerializer(typeof(DuckConfig));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (DuckConfig)ser.ReadObject(ms);
                }
            }
            catch
            {
                return new DuckConfig();
            }
        }

        private static string AppDataDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleAutoDuck");

        private static string ConfigPath => Path.Combine(AppDataDir, "config.json");

        public void Save()
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(ConfigPath, ToJson());
        }

        public static DuckConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    return FromJson(File.ReadAllText(ConfigPath));
            }
            catch
            {
            }
            return new DuckConfig();
        }
    }
}
```

- [ ] **Step 4: 运行测试验证通过**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 4 个 ConfigTests 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(config): add DuckConfig with JSON serialization and clamping"
```

---

## Task 3: Ramp 插值器（TDD）

**Files:**
- Create: `SimpleAutoDuck/Audio/RampInterpolator.cs`
- Test: `SimpleAutoDuck.Tests/RampTests.cs`

- [ ] **Step 1: 写失败测试**

`SimpleAutoDuck.Tests/RampTests.cs`:
```csharp
using SimpleAutoDuck.Audio;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    public class RampTests
    {
        [Fact]
        public void Advance_Attack_MovesQuarterWayPerQuarterTime()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 200);
            ramp.Advance(50);
            Assert.Equal(1.0 - (1.0 - 0.3) * (50.0 / 200.0), ramp.Current, 4);
        }

        [Fact]
        public void Advance_Release_MovesSixteenthWayPerTick()
        {
            var ramp = new RampInterpolator(current: 0.3, target: 1.0, durationMs: 800);
            ramp.Advance(50);
            Assert.Equal(0.3 + (1.0 - 0.3) * (50.0 / 800.0), ramp.Current, 4);
        }

        [Fact]
        public void Advance_PastDuration_ClampsToTarget()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 200);
            ramp.Advance(300);
            Assert.Equal(0.3, ramp.Current, 4);
        }

        [Fact]
        public void IsAtTarget_TrueWhenCurrentEqualsTarget()
        {
            var ramp = new RampInterpolator(current: 0.5, target: 0.5, durationMs: 200);
            Assert.True(ramp.IsAtTarget);
        }

        [Fact]
        public void SetTarget_ResetsDurationAndPreservesCurrent()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 200);
            ramp.Advance(50);
            var midCurrent = ramp.Current;
            ramp.SetTarget(0.8, 1000);
            Assert.Equal(midCurrent, ramp.Current, 4);
            Assert.Equal(0.8, ramp.Target, 4);
            Assert.Equal(1000, ramp.DurationMs);
        }

        [Fact]
        public void Advance_ZeroDuration_JumpsToTarget()
        {
            var ramp = new RampInterpolator(current: 1.0, target: 0.3, durationMs: 0);
            ramp.Advance(50);
            Assert.Equal(0.3, ramp.Current, 4);
        }
    }
}
```

- [ ] **Step 2: 运行测试验证失败**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 编译失败（RampInterpolator 不存在）。

- [ ] **Step 3: 实现 RampInterpolator**

`SimpleAutoDuck/Audio/RampInterpolator.cs`:
```csharp
namespace SimpleAutoDuck.Audio
{
    public sealed class RampInterpolator
    {
        public double Current { get; private set; }
        public double Target { get; private set; }
        public int DurationMs { get; private set; }

        public RampInterpolator(double current, double target, int durationMs)
        {
            Current = current;
            Target = target;
            DurationMs = durationMs;
        }

        public void SetTarget(double target, int durationMs)
        {
            Target = target;
            DurationMs = durationMs;
        }

        public void Advance(int dtMs)
        {
            if (DurationMs <= 0 || dtMs <= 0)
            {
                Current = Target;
                return;
            }
            double ratio = (double)dtMs / DurationMs;
            if (ratio >= 1.0)
            {
                Current = Target;
                return;
            }
            double delta = (Target - Current) * ratio;
            Current += delta;
            if ((Target > Current && Current > Target) || (Target < Current && Current < Target))
                Current = Target;
        }

        public bool IsAtTarget => System.Math.Abs(Current - Target) < 0.0001;
    }
}
```

- [ ] **Step 4: 运行测试验证通过**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 6 个 RampTests 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(audio): add RampInterpolator for linear volume ramping"
```

---

## Task 4: DuckEngine 状态机（TDD，纯逻辑）

**Files:**
- Create: `SimpleAutoDuck/Audio/IDuckSession.cs` # 抽象会话接口便于测试
- Create: `SimpleAutoDuck/Audio/DuckEngine.cs`
- Test: `SimpleAutoDuck.Tests/DuckEngineTests.cs`

- [ ] **Step 1: 写失败测试**

`SimpleAutoDuck.Tests/DuckEngineTests.cs`:
```csharp
using System.Collections.Generic;
using SimpleAutoDuck;
using SimpleAutoDuck.Audio;
using SimpleAutoDuck.Config;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    internal class FakeSession : IDuckSession
    {
        public string ProcessName { get; set; }
        public bool IsMainApp { get; set; }
        public double PeakLevel { get; set; }
        public double CurrentVolume { get; private set; } = 1.0;
        public int SetVolumeCalls { get; private set; }
        public double LastSetVolume { get; private set; }
        public double UserVolumeSnapshot { get; private set; } = 1.0;
        public bool IsExcluded { get; set; }

        public double GetPeakLevel() => PeakLevel;
        public double GetVolume() => CurrentVolume;
        public void SetVolume(double v)
        {
            CurrentVolume = v;
            SetVolumeCalls++;
            LastSetVolume = v;
        }
        public void SnapshotUserVolume() => UserVolumeSnapshot = CurrentVolume;
        public double GetUserVolume() => UserVolumeSnapshot;
    }

    public class DuckEngineTests
    {
        private static DuckConfig Cfg(double threshold = 0.02, int holdMs = 50, int releaseDelayMs = 300,
            double duckDepth = 0.3, int attackMs = 200, int releaseMs = 800) =>
            new DuckConfig
            {
                Threshold = threshold, HoldMs = holdMs, ReleaseDelayMs = releaseDelayMs,
                DuckDepth = duckDepth, AttackMs = attackMs, ReleaseMs = releaseMs
            };

        [Fact]
        public void StartsIn_Monitoring()
        {
            var eng = new DuckEngine(Cfg());
            Assert.Equal(DuckingState.Monitoring, eng.State);
        }

        [Fact]
        public void MainAppAboveThreshold_BelowHoldMs_StaysMonitoring()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 50));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(40);
            Assert.Equal(DuckingState.Monitoring, eng.State);
        }

        [Fact]
        public void MainAppAboveThreshold_AtHoldMs_EntersDucking()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 50));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(40);
            eng.Tick(40);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void EnteringDucking_SnapshotsUserVolumeAndSetsTargetToDuckDepth()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, attackMs: 1000));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(10);
            Assert.Equal(1.0, bg.UserVolumeSnapshot, 4);
            Assert.Equal(0.3, bg.LastSetVolume, 4);
        }

        [Fact]
        public void ReleaseDelay_KeepsDucking_WhenBriefDip()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, releaseDelayMs: 300, releaseMs: 1000));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(10);
            main.PeakLevel = 0;
            eng.Tick(100);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void ReleaseDelay_Passed_ReturnsToMonitoring()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, releaseDelayMs: 300, releaseMs: 1000));
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(10);
            main.PeakLevel = 0;
            eng.Tick(350);
            Assert.Equal(DuckingState.Monitoring, eng.State);
        }

        [Fact]
        public void ThresholdBoundary_InclusiveAtOrAbove()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.02 };
            var eng = new DuckEngine(Cfg(threshold: 0.02, holdMs: 0));
            eng.Sessions = new List<IDuckSession> { main };
            eng.Tick(10);
            Assert.Equal(DuckingState.Ducking, eng.State);
        }

        [Fact]
        public void ExcludedBackground_NotDucked()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var excluded = new FakeSession { ProcessName = "x.exe", IsMainApp = false, IsExcluded = true, PeakLevel = 0 };
            var eng = new DuckEngine(Cfg(holdMs: 0, attackMs: 0));
            eng.Sessions = new List<IDuckSession> { main, excluded };
            eng.Tick(10);
            Assert.Equal(0, excluded.SetVolumeCalls);
        }

        [Fact]
        public void Disabled_DoesNothing()
        {
            var main = new FakeSession { ProcessName = "a.exe", IsMainApp = true, PeakLevel = 0.5 };
            var bg = new FakeSession { ProcessName = "b.exe", IsMainApp = false, PeakLevel = 0 };
            var cfg = Cfg(holdMs: 0);
            cfg.Enabled = false;
            var eng = new DuckEngine(cfg);
            eng.Sessions = new List<IDuckSession> { main, bg };
            eng.Tick(100);
            Assert.Equal(DuckingState.Monitoring, eng.State);
            Assert.Equal(0, bg.SetVolumeCalls);
        }
    }
}
```

- [ ] **Step 2: 运行测试验证失败**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 编译失败（IDuckSession/DuckEngine 不存在）。

- [ ] **Step 3: 实现 IDuckSession 和 DuckEngine**

`SimpleAutoDuck/Audio/IDuckSession.cs`:
```csharp
namespace SimpleAutoDuck.Audio
{
    public interface IDuckSession
    {
        string ProcessName { get; }
        bool IsMainApp { get; }
        bool IsExcluded { get; }
        double GetPeakLevel();
        double GetVolume();
        void SetVolume(double v);
        void SnapshotUserVolume();
        double GetUserVolume();
    }
}
```

`SimpleAutoDuck/Audio/DuckEngine.cs`:
```csharp
using System.Collections.Generic;
using SimpleAutoDuck.Config;

namespace SimpleAutoDuck.Audio
{
    public sealed class DuckEngine
    {
        private readonly DuckConfig _cfg;
        private int _holdAccumMs;
        private int _releaseAccumMs;

        public DuckingState State { get; private set; } = DuckingState.Monitoring;
        public IList<IDuckSession> Sessions { get; set; } = new List<IDuckSession>();

        public DuckEngine(DuckConfig cfg)
        {
            _cfg = cfg;
        }

        public void Tick(int dtMs)
        {
            if (!_cfg.Enabled) return;

            bool mainActive = AnyMainAppActive();

            if (State == DuckingState.Monitoring)
            {
                if (mainActive)
                {
                    _holdAccumMs += dtMs;
                    if (_holdAccumMs >= _cfg.HoldMs)
                        EnterDucking();
                }
                else
                {
                    _holdAccumMs = 0;
                }
            }
            else // Ducking
            {
                if (!mainActive)
                {
                    _releaseAccumMs += dtMs;
                    if (_releaseAccumMs >= _cfg.ReleaseDelayMs)
                        ExitDucking();
                }
                else
                {
                    _releaseAccumMs = 0;
                }
            }

            ApplyRampForState(dtMs);
        }

        private bool AnyMainAppActive()
        {
            foreach (var s in Sessions)
                if (s.IsMainApp && s.GetPeakLevel() >= _cfg.Threshold)
                    return true;
            return false;
        }

        private void EnterDucking()
        {
            State = DuckingState.Ducking;
            _holdAccumMs = 0;
            _releaseAccumMs = 0;
            foreach (var s in Sessions)
            {
                if (s.IsMainApp || s.IsExcluded) continue;
                s.SnapshotUserVolume();
            }
        }

        private void ExitDucking()
        {
            State = DuckingState.Monitoring;
            _releaseAccumMs = 0;
            _holdAccumMs = 0;
        }

        private void ApplyRampForState(int dtMs)
        {
            double target = State == DuckingState.Ducking ? _cfg.DuckDepth : 1.0;
            foreach (var s in Sessions)
            {
                if (s.IsMainApp || s.IsExcluded) continue;
                double userVol = s.GetUserVolume();
                double tgt = State == DuckingState.Ducking ? _cfg.DuckDepth : userVol;
                int dur = State == DuckingState.Ducking ? _cfg.AttackMs : _cfg.ReleaseMs;
                if (dur <= 0)
                {
                    s.SetVolume(tgt);
                    continue;
                }
                double ratio = (double)dtMs / dur;
                double current = s.GetVolume();
                double next = current + (tgt - current) * ratio;
                if ((tgt > current && next > tgt) || (tgt < current && next < tgt))
                    next = tgt;
                s.SetVolume(next);
            }
        }
    }
}
```

- [ ] **Step 4: 运行测试验证通过**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 8 个 DuckEngineTests 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(audio): add DuckEngine state machine with threshold+debounce logic"
```

---

## Task 5: SessionEnvelope（COM 封装，无 TDD）

**Files:**
- Create: `SimpleAutoDuck/Audio/SessionEnvelope.cs`

- [ ] **Step 1: 实现 SessionEnvelope**

`SimpleAutoDuck/Audio/SessionEnvelope.cs`:
```csharp
using System;
using NAudio.CoreAudioApi;
using SimpleAutoDuck.Config;

namespace SimpleAutoDuck.Audio
{
    public sealed class SessionEnvelope : IDuckSession, IDisposable
    {
        private readonly AudioSessionControl _session;
        private SimpleAudioVolume _simpleVol;
        private AudioMeterInformation _meter;
        private double _userVolumeSnapshot = 1.0;
        private double _currentVolume = 1.0;

        public string ProcessName { get; }
        public bool IsMainApp { get; set; }
        public bool IsExcluded { get; set; }

        public SessionEnvelope(AudioSessionControl session)
        {
            _session = session;
            try
            {
                _simpleVol = session.QueryInterface<SimpleAudioVolume>();
                _meter = session.QueryInterface<AudioMeterInformation>();
                ProcessName = TryGetProcessName();
                _currentVolume = GetVolume();
            }
            catch
            {
                ProcessName = "<unknown>";
            }
        }

        private string TryGetProcessName()
        {
            try
            {
                using (var proc = System.Diagnostics.Process.GetProcessById((int)_session.GetProcessID))
                    return proc.ProcessName + ".exe";
            }
            catch
            {
                return "<unknown>.exe";
            }
        }

        public double GetPeakLevel()
        {
            try { return _meter?.GetPeakValue() ?? 0; }
            catch { return 0; }
        }

        public double GetVolume() => _currentVolume;

        public void SetVolume(double v)
        {
            if (v < 0) v = 0; if (v > 1) v = 1;
            if (Math.Abs(v - _currentVolume) < 0.001) return;
            try
            {
                _simpleVol?.SetMasterVolume((float)v);
                _currentVolume = v;
            }
            catch
            {
            }
        }

        public void SnapshotUserVolume()
        {
            try { _userVolumeSnapshot = _simpleVol?.GetMasterVolume() ?? 1.0; }
            catch { _userVolumeSnapshot = 1.0; }
        }

        public double GetUserVolume() => _userVolumeSnapshot;

        public void Dispose()
        {
            try { _simpleVol?.Dispose(); } catch { }
            try { _meter?.Dispose(); } catch { }
        }
    }
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build SimpleAutoDuck/SimpleAutoDuck.csproj`
Expected: 编译通过。若 NAudio API 名不一致，修正属性名（如 `GetProcessID`/`QueryInterface`）。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(audio): add SessionEnvelope wrapping NAudio WASAPI session"
```

---

## Task 6: AudioSessionManager

**Files:**
- Create: `SimpleAutoDuck/Audio/AudioSessionManager.cs`

- [ ] **Step 1: 实现 AudioSessionManager**

`SimpleAutoDuck/Audio/AudioSessionManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace SimpleAutoDuck.Audio
{
    public sealed class AudioSessionManager : IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new MMDeviceEnumerator();
        private MMDevice _device;
        private AudioSessionManager2 _sessionManager;
        private readonly Dictionary<string, SessionEnvelope> _sessions = new Dictionary<string, SessionEnvelope>();

        public event Action<SessionEnvelope> SessionCreated;
        public event Action<SessionEnvelope> SessionEnded;

        public IReadOnlyCollection<SessionEnvelope> Sessions => _sessions.Values;

        public AudioSessionManager()
        {
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _sessionManager = _device.AudioSessionManager;
            _sessionManager.OnSessionCreated += OnSessionCreated;
        }

        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            var envelope = AddSession(e.Session);
            if (envelope != null) SessionCreated?.Invoke(envelope);
        }

        public void Refresh()
        {
            Clear();
            foreach (var s in _sessionManager.Sessions)
            {
                AddSession(s);
            }
        }

        private SessionEnvelope AddSession(AudioSessionControl session)
        {
            var envelope = new SessionEnvelope(session);
            if (string.IsNullOrEmpty(envelope.ProcessName) || envelope.ProcessName == "<unknown>.exe")
            {
                envelope.Dispose();
                return null;
            }
            if (_sessions.ContainsKey(envelope.ProcessName))
            {
                envelope.Dispose();
                return null;
            }
            envelope.SetVolume(1.0); // 初始确保满音量
            _sessions[envelope.ProcessName] = envelope;
            return envelope;
        }

        public void Clear()
        {
            foreach (var s in _sessions.Values)
                try { s.Dispose(); } catch { }
            _sessions.Clear();
        }

        public void Dispose()
        {
            Clear();
            try { _sessionManager?.Dispose(); } catch { }
            try { _device?.Dispose(); } catch { }
            try { _enumerator?.Dispose(); } catch { }
        }
    }
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build SimpleAutoDuck/SimpleAutoDuck.csproj`
Expected: 编译通过。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(audio): add AudioSessionManager for WASAPI session enumeration"
```

---

## Task 7: HotkeyDefinition 解析（TDD）

**Files:**
- Create: `SimpleAutoDuck/Hotkey/HotkeyDefinition.cs`
- Test: `SimpleAutoDuck.Tests/HotkeyDefinitionTests.cs`

- [ ] **Step 1: 写失败测试**

`SimpleAutoDuck.Tests/HotkeyDefinitionTests.cs`:
```csharp
using System.Windows.Forms;
using SimpleAutoDuck.Hotkey;
using Xunit;

namespace SimpleAutoDuck.Tests
{
    public class HotkeyDefinitionTests
    {
        [Fact]
        public void Parse_CtrlAltD()
        {
            var hk = HotkeyDefinition.Parse("Ctrl+Alt+D");
            Assert.True(hk.IsValid);
            Assert.Equal(Keys.D, hk.Key);
            Assert.True(hk.HasModifier(Keys.Control));
            Assert.True(hk.HasModifier(Keys.Alt));
        }

        [Fact]
        public void Parse_ShiftF5()
        {
            var hk = HotkeyDefinition.Parse("Shift+F5");
            Assert.True(hk.IsValid);
            Assert.Equal(Keys.F5, hk.Key);
            Assert.True(hk.HasModifier(Keys.Shift));
            Assert.False(hk.HasModifier(Keys.Control));
        }

        [Fact]
        public void Parse_LowercaseNormalizes()
        {
            var hk = HotkeyDefinition.Parse("ctrl+alt+d");
            Assert.True(hk.IsValid);
            Assert.Equal(Keys.D, hk.Key);
        }

        [Fact]
        public void Parse_Invalid_ReturnsNotValid()
        {
            var hk = HotkeyDefinition.Parse("");
            Assert.False(hk.IsValid);
        }

        [Fact]
        public void Modifiers_CombinedAsFlags()
        {
            var hk = HotkeyDefinition.Parse("Ctrl+Alt+D");
            Assert.Equal(Keys.Control | Keys.Alt | Keys.D, hk.Modifiers | hk.Key);
        }

        [Fact]
        public void Id_StableForSameDefinition()
        {
            var a = HotkeyDefinition.Parse("Ctrl+Alt+D");
            var b = HotkeyDefinition.Parse("Ctrl+Alt+D");
            Assert.Equal(a.Id, b.Id);
        }
    }
}
```

- [ ] **Step 2: 运行测试验证失败**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 编译失败。

- [ ] **Step 3: 实现 HotkeyDefinition**

`SimpleAutoDuck/Hotkey/HotkeyDefinition.cs`:
```csharp
using System.Windows.Forms;

namespace SimpleAutoDuck.Hotkey
{
    public struct HotkeyDefinition
    {
        public Keys Key { get; }
        public Keys Modifiers { get; }
        public bool IsValid { get; }

        public HotkeyDefinition(Keys key, Keys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
            IsValid = key != Keys.None;
        }

        public bool HasModifier(Keys mod) => (Modifiers & mod) == mod;

        public int Id => (int)Key | ((int)Modifiers << 16);

        public static HotkeyDefinition Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new HotkeyDefinition(Keys.None, Keys.None);

            Keys mods = Keys.None;
            Keys key = Keys.None;
            string[] parts = text.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                bool isLast = (i == parts.Length - 1);
                if (!isLast)
                {
                    switch (p.ToLowerInvariant())
                    {
                        case "ctrl":
                        case "control":
                            mods |= Keys.Control; break;
                        case "alt":
                            mods |= Keys.Alt; break;
                        case "shift":
                            mods |= Keys.Shift; break;
                        case "win":
                            mods |= Keys.LWin; break;
                    }
                }
                else
                {
                    key = ParseKey(p);
                }
            }
            return new HotkeyDefinition(key, mods);
        }

        private static Keys ParseKey(string s)
        {
            s = s.Trim();
            if (s.Length == 1 && char.IsLetterOrDigit(s[0]))
                return (Keys)char.ToUpper(s[0]);
            if (s.Length == 1 && char.IsDigit(s[0]))
                return (Keys)((int)Keys.D0 + (s[0] - '0'));
            switch (s.ToUpperInvariant())
            {
                case "F1": return Keys.F1;
                case "F2": return Keys.F2;
                case "F3": return Keys.F3;
                case "F4": return Keys.F4;
                case "F5": return Keys.F5;
                case "F6": return Keys.F6;
                case "F7": return Keys.F7;
                case "F8": return Keys.F8;
                case "F9": return Keys.F9;
                case "F10": return Keys.F10;
                case "F11": return Keys.F11;
                case "F12": return Keys.F12;
                case "SPACE": return Keys.Space;
                case "ENTER": return Keys.Enter;
                case "ESC": case "ESCAPE": return Keys.Escape;
                case "TAB": return Keys.Tab;
                case "HOME": return Keys.Home;
                case "END": return Keys.End;
                case "PGUP": return Keys.PageUp;
                case "PGDN": return Keys.PageDown;
                case "INS": case "INSERT": return Keys.Insert;
                case "DEL": case "DELETE": return Keys.Delete;
            }
            Keys k;
            return System.Enum.TryParse(s, true, out k) ? k : Keys.None;
        }
    }
}
```

- [ ] **Step 4: 运行测试验证通过**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 6 个 HotkeyDefinitionTests 全部 PASS。

- [ ] **Step 5: 提交**

```bash
git add -A
git commit -m "feat(hotkey): add HotkeyDefinition parser"
```

---

## Task 8: GlobalHotkeyRegistrar

**Files:**
- Create: `SimpleAutoDuck/Hotkey/GlobalHotkeyRegistrar.cs`

- [ ] **Step 1: 实现 GlobalHotkeyRegistrar**

`SimpleAutoDuck/Hotkey/GlobalHotkeyRegistrar.cs`:
```csharp
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleAutoDuck.Hotkey
{
    public sealed class GlobalHotkeyRegistrar : NativeWindow, IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int WM_HOTKEY = 0x0312;

        public event Action HotkeyPressed;
        public bool IsRegistered { get; private set; }
        public string LastError { get; private set; }

        private int _id;

        public GlobalHotkeyRegistrar()
        {
            var cp = new CreateParams();
            cp.Caption = "SimpleAutoDuckHotkeyHelper";
            cp.ClassName = "Static";
            CreateHandle(cp);
        }

        public bool Register(HotkeyDefinition hk)
        {
            if (IsRegistered) Unregister();
            if (!hk.IsValid) return false;

            uint mods = 0;
            if (hk.HasModifier(Keys.Control)) mods |= MOD_CONTROL;
            if (hk.HasModifier(Keys.Alt)) mods |= MOD_ALT;
            if (hk.HasModifier(Keys.Shift)) mods |= MOD_SHIFT;
            if (hk.HasModifier(Keys.LWin) || hk.HasModifier(Keys.RWin)) mods |= MOD_WIN;

            _id = hk.Id;
            if (RegisterHotKey(Handle, _id, mods, (uint)hk.Key))
            {
                IsRegistered = true;
                LastError = null;
                return true;
            }
            int err = Marshal.GetLastWin32Error();
            LastError = $"热键注册失败 (错误码 {err})，可能被其他程序占用";
            return false;
        }

        public void Unregister()
        {
            if (IsRegistered)
            {
                UnregisterHotKey(Handle, _id);
                IsRegistered = false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _id)
            {
                HotkeyPressed?.Invoke();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build SimpleAutoDuck/SimpleAutoDuck.csproj`
Expected: 编译通过。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(hotkey): add GlobalHotkeyRegistrar using RegisterHotKey"
```

---

## Task 9: TrayIcon

**Files:**
- Create: `SimpleAutoDuck/UI/TrayIcon.cs`

- [ ] **Step 1: 实现 TrayIcon**

`SimpleAutoDuck/UI/TrayIcon.cs`:
```csharp
using System;
using System.Windows.Forms;

namespace SimpleAutoDuck.UI
{
    public sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notify;
        private readonly ToolStripMenuItem _toggleItem;

        public event Action ShowRequested;
        public event Action ExitRequested;
        public event Action ToggleRequested;

        public TrayIcon()
        {
            _notify = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "SimpleAutoDuck"
            };
            var menu = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem("显示(&S)");
            showItem.Click += (s, e) => ShowRequested?.Invoke();
            _toggleItem = new ToolStripMenuItem("启用鸭子(&E)");
            _toggleItem.CheckOnClick = true;
            _toggleItem.Click += (s, e) => ToggleRequested?.Invoke();
            var exitItem = new ToolStripMenuItem("退出(&X)");
            exitItem.Click += (s, e) => ExitRequested?.Invoke();
            menu.Items.AddRange(new ToolStripItem[] { showItem, _toggleItem, new ToolStripSeparator(), exitItem });
            _notify.ContextMenuStrip = menu;
            _notify.DoubleClick += (s, e) => ShowRequested?.Invoke();
        }

        public void SetToggleState(bool enabled)
        {
            if (_toggleItem.Checked != enabled) _toggleItem.Checked = enabled;
        }

        public void Show() => _notify.Visible = true;
        public void Hide() => _notify.Visible = false;
        public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 2000)
            => _notify.ShowBalloonTip(timeout, title, text, icon);

        public void Dispose()
        {
            _notify?.Dispose();
        }
    }
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build SimpleAutoDuck/SimpleAutoDuck.csproj`
Expected: 编译通过。

- [ ] **Step 3: 提交**

```bash
git add -A
git commit -m "feat(ui): add TrayIcon with show/toggle/exit menu"
```

---

## Task 10: MainForm + Designer + Program

**Files:**
- Create: `SimpleAutoDuck/UI/MainForm.Designer.cs`
- Create: `SimpleAutoDuck/UI/MainForm.cs`
- Delete: `SimpleAutoDuck/Form1.cs`, `SimpleAutoDuck/Form1.Designer.cs`
- Modify: `SimpleAutoDuck/Program.cs`
- Modify: `SimpleAutoDuck/SimpleAutoDuck.csproj`

- [ ] **Step 1: 从 csproj 移除 Form1 引用，加入新文件**

修改 `SimpleAutoDuck/SimpleAutoDuck.csproj` 的 `<ItemGroup>`：删除 `Form1.cs`、`Form1.Designer.cs` 的 `<Compile>` 项，加入：
```xml
<Compile Include="UI\MainForm.cs">
  <SubType>Form</SubType>
</Compile>
<Compile Include="UI\MainForm.Designer.cs">
  <DependentUpon>MainForm.cs</DependentUpon>
</Compile>
<Compile Include="UI\TrayIcon.cs" />
<Compile Include="Audio\SessionEnvelope.cs" />
<Compile Include="Audio\AudioSessionManager.cs" />
<Compile Include="Audio\DuckEngine.cs" />
<Compile Include="Audio\IDuckSession.cs" />
<Compile Include="Audio\RampInterpolator.cs" />
<Compile Include="Config\DuckConfig.cs" />
<Compile Include="Hotkey\HotkeyDefinition.cs" />
<Compile Include="Hotkey\GlobalHotkeyRegistrar.cs" />
<Compile Include="DuckingState.cs" />
```

删除 `Form1.cs` 和 `Form1.Designer.cs`。

- [ ] **Step 2: MainForm.Designer.cs**

`SimpleAutoDuck/UI/MainForm.Designer.cs`:
```csharp
namespace SimpleAutoDuck.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private CheckedListBox clbSessions;
        private Button btnRefresh;
        private Label lblState;
        private ProgressBar pbMainLevel;
        private TrackBar tbThreshold;
        private TrackBar tbDuckDepth;
        private TrackBar tbAttack;
        private TrackBar tbRelease;
        private TrackBar tbHold;
        private TrackBar tbReleaseDelay;
        private Label lblThresholdVal;
        private Label lblDuckDepthVal;
        private Label lblAttackVal;
        private Label lblReleaseVal;
        private Label lblHoldVal;
        private Label lblReleaseDelayVal;
        private CheckBox chkEnabled;
        private Button btnSave;
        private System.Windows.Forms.Timer tickTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.clbSessions = new CheckedListBox();
            this.btnRefresh = new Button();
            this.lblState = new Label();
            this.pbMainLevel = new ProgressBar();
            this.tbThreshold = new TrackBar();
            this.tbDuckDepth = new TrackBar();
            this.tbAttack = new TrackBar();
            this.tbRelease = new TrackBar();
            this.tbHold = new TrackBar();
            this.tbReleaseDelay = new TrackBar();
            this.lblThresholdVal = new Label();
            this.lblDuckDepthVal = new Label();
            this.lblAttackVal = new Label();
            this.lblReleaseVal = new Label();
            this.lblHoldVal = new Label();
            this.lblReleaseDelayVal = new Label();
            this.chkEnabled = new CheckBox();
            this.btnSave = new Button();
            this.tickTimer = new System.Windows.Forms.Timer(this.components);

            // clbSessions
            this.clbSessions.FormattingEnabled = true;
            this.clbSessions.Location = new System.Drawing.Point(12, 33);
            this.clbSessions.Name = "clbSessions";
            this.clbSessions.Size = new System.Drawing.Size(250, 250);
            this.clbSessions.CheckOnClick = true;

            // btnRefresh
            this.btnRefresh.Location = new System.Drawing.Point(12, 8);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Text = "刷新会话";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // lblState
            this.lblState.Location = new System.Drawing.Point(270, 33);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(200, 23);
            this.lblState.Text = "状态: 监测中";

            // pbMainLevel
            this.pbMainLevel.Location = new System.Drawing.Point(270, 60);
            this.pbMainLevel.Name = "pbMainLevel";
            this.pbMainLevel.Size = new System.Drawing.Size(200, 20);
            this.pbMainLevel.Maximum = 1000;

            // tbThreshold (0..100 -> 0.00..1.00)
            this.tbThreshold.Location = new System.Drawing.Point(270, 90);
            this.tbThreshold.Size = new System.Drawing.Size(150, 45);
            this.tbThreshold.Minimum = 0; this.tbThreshold.Maximum = 100;
            this.tbThreshold.TickFrequency = 10;
            this.tbThreshold.Scroll += new System.EventHandler(this.tbThreshold_Scroll);

            this.lblThresholdVal.Location = new System.Drawing.Point(430, 90);
            this.lblThresholdVal.Size = new System.Drawing.Size(50, 20);

            // tbDuckDepth (0..100)
            this.tbDuckDepth.Location = new System.Drawing.Point(270, 140);
            this.tbDuckDepth.Size = new System.Drawing.Size(150, 45);
            this.tbDuckDepth.Minimum = 0; this.tbDuckDepth.Maximum = 100;
            this.tbDuckDepth.TickFrequency = 10;
            this.tbDuckDepth.Scroll += new System.EventHandler(this.tbDuckDepth_Scroll);
            this.lblDuckDepthVal.Location = new System.Drawing.Point(430, 140);

            // tbAttack (1..2000)
            this.tbAttack.Location = new System.Drawing.Point(270, 190);
            this.tbAttack.Size = new System.Drawing.Size(150, 45);
            this.tbAttack.Minimum = 1; this.tbAttack.Maximum = 2000;
            this.tbAttack.TickFrequency = 200;
            this.tbAttack.Scroll += new System.EventHandler(this.tbAttack_Scroll);
            this.lblAttackVal.Location = new System.Drawing.Point(430, 190);

            // tbRelease (1..5000)
            this.tbRelease.Location = new System.Drawing.Point(270, 240);
            this.tbRelease.Size = new System.Drawing.Size(150, 45);
            this.tbRelease.Minimum = 1; this.tbRelease.Maximum = 5000;
            this.tbRelease.TickFrequency = 500;
            this.tbRelease.Scroll += new System.EventHandler(this.tbRelease_Scroll);
            this.lblReleaseVal.Location = new System.Drawing.Point(430, 240);

            // tbHold (0..2000)
            this.tbHold.Location = new System.Drawing.Point(270, 290);
            this.tbHold.Size = new System.Drawing.Size(150, 45);
            this.tbHold.Minimum = 0; this.tbHold.Maximum = 2000;
            this.tbHold.TickFrequency = 100;
            this.tbHold.Scroll += new System.EventHandler(this.tbHold_Scroll);
            this.lblHoldVal.Location = new System.Drawing.Point(430, 290);

            // tbReleaseDelay (0..5000)
            this.tbReleaseDelay.Location = new System.Drawing.Point(270, 340);
            this.tbReleaseDelay.Size = new System.Drawing.Size(150, 45);
            this.tbReleaseDelay.Minimum = 0; this.tbReleaseDelay.Maximum = 5000;
            this.tbReleaseDelay.TickFrequency = 500;
            this.tbReleaseDelay.Scroll += new System.EventHandler(this.tbReleaseDelay_Scroll);
            this.lblReleaseDelayVal.Location = new System.Drawing.Point(430, 340);

            // chkEnabled
            this.chkEnabled.Location = new System.Drawing.Point(12, 290);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Text = "启用自动鸭子";
            this.chkEnabled.CheckedChanged += new System.EventHandler(this.chkEnabled_CheckedChanged);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(12, 320);
            this.btnSave.Name = "btnSave";
            this.btnSave.Text = "保存配置";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // tickTimer
            this.tickTimer.Interval = 50;
            this.tickTimer.Tick += new System.EventHandler(this.tickTimer_Tick);

            // MainForm
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.Controls.Add(this.clbSessions);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.pbMainLevel);
            this.Controls.Add(this.tbThreshold);
            this.Controls.Add(this.lblThresholdVal);
            this.Controls.Add(this.tbDuckDepth);
            this.Controls.Add(this.lblDuckDepthVal);
            this.Controls.Add(this.tbAttack);
            this.Controls.Add(this.lblAttackVal);
            this.Controls.Add(this.tbRelease);
            this.Controls.Add(this.lblReleaseVal);
            this.Controls.Add(this.tbHold);
            this.Controls.Add(this.lblHoldVal);
            this.Controls.Add(this.tbReleaseDelay);
            this.Controls.Add(this.lblReleaseDelayVal);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.btnSave);
            this.Name = "MainForm";
            this.Text = "SimpleAutoDuck";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
        }
    }
}
```

- [ ] **Step 3: MainForm.cs**

`SimpleAutoDuck/UI/MainForm.cs`:
```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SimpleAutoDuck.Audio;
using SimpleAutoDuck.Config;
using SimpleAutoDuck.Hotkey;

namespace SimpleAutoDuck.UI
{
    public partial class MainForm : Form
    {
        private readonly DuckConfig _config;
        private readonly AudioSessionManager _sessionManager;
        private readonly DuckEngine _engine;
        private readonly TrayIcon _tray;
        private readonly GlobalHotkeyRegistrar _hotkey;

        public MainForm()
        {
            InitializeComponent();
            _config = DuckConfig.Load();
            _config.Clamp();

            try
            {
                _sessionManager = new AudioSessionManager();
                _sessionManager.SessionCreated += OnSessionCreated;
                _sessionManager.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法初始化音频会话管理器: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _engine = new DuckEngine(_config);
            LinkSessionsToEngine();
            _tray = new TrayIcon();
            _tray.ShowRequested += () => ShowFromTray();
            _tray.ExitRequested += () => ExitApp();
            _tray.ToggleRequested += () => ToggleEnabled();
            _tray.Show();

            _hotkey = new GlobalHotkeyRegistrar();
            _hotkey.HotkeyPressed += ToggleEnabled;
            RegisterHotkey();

            BindConfigToControls();
            PopulateSessionList();
            tickTimer.Start();
        }

        private void LinkSessionsToEngine()
        {
            if (_sessionManager == null) return;
            var list = _sessionManager.Sessions.ToList();
            foreach (var s in list)
            {
                s.IsMainApp = _config.MainAppProcessNames.Contains(s.ProcessName);
                s.IsExcluded = _config.BackgroundBlacklist.Contains(s.ProcessName);
            }
            _engine.Sessions = list.Cast<IDuckSession>().ToList();
        }

        private void OnSessionCreated(SessionEnvelope s)
        {
            if (IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                s.IsMainApp = _config.MainAppProcessNames.Contains(s.ProcessName);
                s.IsExcluded = _config.BackgroundBlacklist.Contains(s.ProcessName);
                LinkSessionsToEngine();
                PopulateSessionList();
            }));
        }

        private void BindConfigToControls()
        {
            tbThreshold.Value = (int)(_config.Threshold * 100);
            tbDuckDepth.Value = (int)(_config.DuckDepth * 100);
            tbAttack.Value = _config.AttackMs;
            tbRelease.Value = _config.ReleaseMs;
            tbHold.Value = _config.HoldMs;
            tbReleaseDelay.Value = _config.ReleaseDelayMs;
            chkEnabled.Checked = _config.Enabled;
            _tray.SetToggleState(_config.Enabled);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            lblThresholdVal.Text = (_config.Threshold).ToString("0.00");
            lblDuckDepthVal.Text = _config.DuckDepth.ToString("0.00");
            lblAttackVal.Text = _config.AttackMs + " ms";
            lblReleaseVal.Text = _config.ReleaseMs + " ms";
            lblHoldVal.Text = _config.HoldMs + " ms";
            lblReleaseDelayVal.Text = _config.ReleaseDelayMs + " ms";
        }

        private void PopulateSessionList()
        {
            if (_sessionManager == null) return;
            var checks = new System.Collections.Generic.Dictionary<string, bool>();
            foreach (int i in clbSessions.CheckedIndices)
                checks[(string)clbSessions.Items[i]] = true;

            clbSessions.Items.Clear();
            foreach (var s in _sessionManager.Sessions)
            {
                int idx = clbSessions.Items.Add(s.ProcessName);
                clbSessions.SetItemChecked(idx, _config.MainAppProcessNames.Contains(s.ProcessName));
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try { _sessionManager?.Refresh(); LinkSessionsToEngine(); PopulateSessionList(); }
            catch (Exception ex) { MessageBox.Show("刷新失败: " + ex.Message); }
        }

        private void clbSessions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= clbSessions.Items.Count) return;
            string proc = (string)clbSessions.Items[e.Index];
            if (e.NewValue == CheckState.Checked)
            {
                if (!_config.MainAppProcessNames.Contains(proc)) _config.MainAppProcessNames.Add(proc);
            }
            else
            {
                _config.MainAppProcessNames.Remove(proc);
            }
            LinkSessionsToEngine();
        }

        private void tbThreshold_Scroll(object sender, EventArgs e)
        {
            _config.Threshold = tbThreshold.Value / 100.0;
            UpdateValueLabels();
        }
        private void tbDuckDepth_Scroll(object sender, EventArgs e)
        {
            _config.DuckDepth = tbDuckDepth.Value / 100.0;
            UpdateValueLabels();
        }
        private void tbAttack_Scroll(object sender, EventArgs e)
        {
            _config.AttackMs = tbAttack.Value;
            UpdateValueLabels();
        }
        private void tbRelease_Scroll(object sender, EventArgs e)
        {
            _config.ReleaseMs = tbRelease.Value;
            UpdateValueLabels();
        }
        private void tbHold_Scroll(object sender, EventArgs e)
        {
            _config.HoldMs = tbHold.Value;
            UpdateValueLabels();
        }
        private void tbReleaseDelay_Scroll(object sender, EventArgs e)
        {
            _config.ReleaseDelayMs = tbReleaseDelay.Value;
            UpdateValueLabels();
        }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            _config.Enabled = chkEnabled.Checked;
            _tray.SetToggleState(_config.Enabled);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _config.Clamp();
            _config.Save();
            MessageBox.Show("配置已保存", "SimpleAutoDuck", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tickTimer_Tick(object sender, EventArgs e)
        {
            _engine.Tick(50);
            lblState.Text = "状态: " + (_engine.State == DuckingState.Ducking ? "鸭子中" : "监测中");
            double maxLevel = 0;
            foreach (var s in _engine.Sessions)
            {
                if (s.IsMainApp)
                {
                    var lvl = s.GetPeakLevel();
                    if (lvl > maxLevel) maxLevel = lvl;
                }
            }
            pbMainLevel.Value = Math.Min(1000, (int)(maxLevel * 1000));
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                _tray.ShowBalloon("SimpleAutoDuck", "已最小化到托盘", ToolTipIcon.Info);
            }
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void ToggleEnabled()
        {
            chkEnabled.Checked = !chkEnabled.Checked;
        }

        private void RegisterHotkey()
        {
            var hk = HotkeyDefinition.Parse(_config.Hotkey);
            if (!_hotkey.Register(hk))
            {
                MessageBox.Show(_hotkey.LastError ?? "热键注册失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExitApp()
        {
            _tray.Hide();
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _tray.ShowBalloon("SimpleAutoDuck", "仍在后台运行", ToolTipIcon.Info);
            }
        }
    }
}
```

- [ ] **Step 4: 修改 Program.cs**

`SimpleAutoDuck/Program.cs`:
```csharp
using System;
using System.Windows.Forms;
using SimpleAutoDuck.UI;

namespace SimpleAutoDuck
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
```

- [ ] **Step 5: 构建验证**

Run: `dotnet build SimpleAutoDuck.sln /p:Configuration=Debug`
Expected: 主项目和测试项目都编译通过。

- [ ] **Step 6: 运行所有测试**

Run: `dotnet test SimpleAutoDuck.sln`
Expected: 所有单元测试通过。

- [ ] **Step 7: 提交**

```bash
git add -A
git commit -m "feat(ui): add MainForm with controls, tray, hotkey, timer integration"
```

---

## Task 11: 最终验证 + 手动验收清单

- [ ] **Step 1: 全量构建并测试**

Run: `dotnet build SimpleAutoDuck.sln /p:Configuration=Release`
Run: `dotnet test SimpleAutoDuck.sln`
Expected: 构建成功，所有测试通过。

- [ ] **Step 2: 手动验收清单（人工执行）**

1. 启动 `bin\Release\SimpleAutoDuck.exe`
2. 默认音频设备可识别，刷新按钮可列出当前发声应用
3. 启动 Discord/微信语音 + 背景音乐播放器
4. 勾选 Discord/微信为主应用
5. 启用自动鸭子（CheckBox 或托盘菜单）
6. 说话 → 背景音乐音量平滑下降至 DuckDepth
7. 停止说话 300+ms → 背景音乐平滑恢复
8. 最小化到托盘正常，双击托盘图标恢复
9. 热键 Ctrl+Alt+D 切换启停
10. 保存配置后重启，主应用勾选与参数恢复

- [ ] **Step 3: 提交最终状态**

```bash
git add -A
git commit -m "chore: final verification build and tests green"
```

---

## Self-Review 结果

**Spec 覆盖**：
- 4.1 主应用识别 / 自动鸭子 / 按应用控制 → Task 4, 6, 10 ✓
- 4.2 阈值/深度/Attack/Release/白名单黑名单 → Task 2, 10 ✓
- 4.3 WinForms 界面/列表/状态/滑块/启停/保存 → Task 10 ✓
- 4.4 Win10/11、托盘、低占用、热键 → Task 8, 9, 10 ✓
- 错误处理 → 各 COM 调用 try/catch ✓
- 测试 → Task 2, 3, 4, 7 ✓

**Placeholder 扫描**：无 TBD/TODO。

**类型一致性**：`IDuckSession.IsExcluded` 在 DuckEngine 与 SessionEnvelope 一致；`DuckConfig.Enabled` 在 Engine/UI 一致；`HotkeyDefinition.Id` 在 Registrar 一致。

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-07-04-auto-ducker-implementation.md`. Two execution options:**

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**