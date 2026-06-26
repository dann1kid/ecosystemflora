using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FloraSymbiosisTests
    {
        [Theory]
        [InlineData("eaglefern")]
        [InlineData("cinnamonfern")]
        [InlineData("deerfern")]
        [InlineData("bluebell")]
        [InlineData("lilyofthevalley")]
        public void TreeSymbionts_RequireTreeHost(string species)
        {
            Assert.True(FloraSymbiosis.TryGetRule(species, out FloraSymbiosis.Rule rule));
            Assert.Contains(FloraSymbiosis.TreeHostToken, rule.HostKeys);
        }

        [Fact]
        public void Hartstongue_HasNoSymbiosisHostRule()
        {
            Assert.False(FloraSymbiosis.TryGetRule("hartstongue", out _));
        }
    }
}
