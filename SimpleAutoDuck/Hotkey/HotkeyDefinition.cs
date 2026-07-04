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