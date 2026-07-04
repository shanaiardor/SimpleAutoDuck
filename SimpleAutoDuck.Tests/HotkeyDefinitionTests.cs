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