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