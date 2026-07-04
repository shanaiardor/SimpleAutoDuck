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

    public static class Presets
    {
        public struct Preset
        {
            public string Name;
            public double Threshold;
            public double DuckDepth;
            public int AttackMs;
            public int ReleaseMs;
            public int HoldMs;
            public int ReleaseDelayMs;
        }

        public static readonly Preset[] All =
        {
            new Preset
            {
                Name = "温和（音乐/视频）",
                Threshold = 0.02, DuckDepth = 0.5,
                AttackMs = 400, ReleaseMs = 1200,
                HoldMs = 80, ReleaseDelayMs = 500
            },
            new Preset
            {
                Name = "标准（默认）",
                Threshold = 0.02, DuckDepth = 0.3,
                AttackMs = 200, ReleaseMs = 800,
                HoldMs = 50, ReleaseDelayMs = 300
            },
            new Preset
            {
                Name = "激进（游戏语音）",
                Threshold = 0.01, DuckDepth = 0.15,
                AttackMs = 80, ReleaseMs = 400,
                HoldMs = 20, ReleaseDelayMs = 200
            },
            new Preset
            {
                Name = "强力压低（直播）",
                Threshold = 0.03, DuckDepth = 0.08,
                AttackMs = 100, ReleaseMs = 1000,
                HoldMs = 30, ReleaseDelayMs = 400
            },
            new Preset
            {
                Name = "轻柔淡出（播客）",
                Threshold = 0.015, DuckDepth = 0.35,
                AttackMs = 600, ReleaseMs = 2000,
                HoldMs = 100, ReleaseDelayMs = 800
            },
        };

        public static void ApplyTo(DuckConfig cfg, Preset preset)
        {
            cfg.Threshold = preset.Threshold;
            cfg.DuckDepth = preset.DuckDepth;
            cfg.AttackMs = preset.AttackMs;
            cfg.ReleaseMs = preset.ReleaseMs;
            cfg.HoldMs = preset.HoldMs;
            cfg.ReleaseDelayMs = preset.ReleaseDelayMs;
        }
    }
}