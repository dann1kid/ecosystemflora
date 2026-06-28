using System.Collections.Generic;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class SpacingFromSpeciesCodecTests
    {
        [Fact]
        public void Format_and_parse_roundtrip()
        {
            var map = new Dictionary<string, int>
            {
                ["wilddaisy"] = 3,
                ["catmint"] = 3,
                ["cornflower"] = 3,
            };

            string text = SpacingFromSpeciesCodec.Format(map);
            Dictionary<string, int> parsed = SpacingFromSpeciesCodec.Parse(text);

            Assert.Equal(3, parsed["wilddaisy"]);
            Assert.Equal(3, parsed["catmint"]);
            Assert.Equal(3, parsed["cornflower"]);
        }
    }
}
