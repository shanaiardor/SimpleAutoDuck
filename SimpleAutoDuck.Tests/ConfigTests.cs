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