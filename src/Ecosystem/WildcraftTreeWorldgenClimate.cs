using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Climate envelopes sourced from Wildcraft Trees' worldgen (treegenproperties patch).
    /// Rain/forest are normalized to 0..1.
    /// Stage B: used to provide better defaults for third-party tree participants.
    /// </summary>
    internal static class WildcraftTreeWorldgenClimate
    {
        internal readonly struct Entry
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly float MinRain;
            public readonly float MaxRain;
            public readonly float MinForest;
            public readonly float MaxForest;
            public readonly float Weight;

            public Entry(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float weight)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                MinForest = minForest;
                MaxForest = maxForest;
                Weight = weight;
            }
        }

        // Generated from _compatcheck/wildcrafttree/assets/wildcrafttree/patches/treegenproperties.json
        // via community/tools/generate_wildcrafttree_tree_table.py
        static readonly Dictionary<string, Entry> ByGenerator = new Dictionary<string, Entry>
        {
            ["alder"] = new Entry(-8, 6, 0.3529f, 0.8235f, 0.2745f, 0.6667f, 2.0000f),
            ["ash"] = new Entry(14, 24, 0.6078f, 0.7647f, 0.4706f, 1.0000f, 0.6000f),
            ["aspen"] = new Entry(-16, 10, 0.3922f, 0.7843f, 0.3137f, 0.7843f, 3.0000f),
            ["azobe"] = new Entry(27, 40, 0.7255f, 1.0000f, 0.4510f, 1.0000f, 2.0000f),
            ["banyan"] = new Entry(28, 37, 0.7255f, 1.0000f, 0.5294f, 1.0000f, 0.6000f),
            ["bearnut"] = new Entry(5, 20, 0.4314f, 0.6471f, 0.3922f, 0.7451f, 10.0000f),
            ["beech"] = new Entry(14, 20, 0.4314f, 0.7059f, 0.4314f, 1.0000f, 1.7000f),
            ["blackpoplar"] = new Entry(-2, 20, 0.3725f, 0.7451f, 0.3529f, 0.7843f, 12.0000f),
            ["bluemahoe"] = new Entry(23, 30, 0.6667f, 0.8627f, 0.4706f, 0.7843f, 0.6000f),
            ["bluespruce"] = new Entry(1, 2, 0.7059f, 0.7843f, 0.5490f, 1.0000f, 0.1000f),
            ["brideinwhite"] = new Entry(-4, 14, 0.5882f, 1.0000f, 0.0000f, 1.0000f, 2.0000f),
            ["catalpa"] = new Entry(18, 26, 0.6667f, 1.0000f, 0.4706f, 1.0000f, 2.0000f),
            ["cedar"] = new Entry(-10, 6, 0.2941f, 0.5882f, 0.3529f, 1.0000f, 3.0000f),
            ["coastdouglasfir"] = new Entry(12, 16, 0.7059f, 1.0000f, 0.2353f, 0.8627f, 15.0000f),
            ["crookedtuja"] = new Entry(-4, 12, 0.2157f, 0.5882f, 0.0000f, 0.9804f, 6.0000f),
            ["crownofthorns"] = new Entry(27, 40, 0.3137f, 0.5490f, 0.1961f, 0.9020f, 0.8000f),
            ["dalbergia"] = new Entry(28, 40, 0.3137f, 0.5490f, 0.1961f, 0.8627f, 0.2000f),
            ["elm"] = new Entry(13, 18, 0.3922f, 0.6863f, 0.3529f, 1.0000f, 1.0000f),
            ["empresstree"] = new Entry(27, 40, 0.3137f, 0.5490f, 0.1961f, 0.7843f, 0.5000f),
            ["eucalyptus"] = new Entry(28, 40, 0.6078f, 0.9412f, 0.4314f, 0.9020f, 1.4000f),
            ["ghostgum"] = new Entry(26, 40, 0.1569f, 0.3922f, 0.0000f, 0.4706f, 1.0000f),
            ["giantbanyan"] = new Entry(28, 37, 0.7255f, 1.0000f, 0.5294f, 1.0000f, 0.0700f),
            ["ginkgo"] = new Entry(5, 20, 0.3529f, 0.7843f, 0.0784f, 0.7843f, 2.0000f),
            ["guajacum"] = new Entry(27, 40, 0.7255f, 0.9804f, 0.4902f, 1.0000f, 0.8000f),
            ["honeylocust"] = new Entry(8, 21, 0.1961f, 0.5882f, 0.0000f, 0.3137f, 2.0000f),
            ["horsechestnut"] = new Entry(16, 23, 0.4314f, 0.7647f, 0.4314f, 1.0000f, 0.6000f),
            ["jacaranda"] = new Entry(27, 40, 0.7255f, 1.0000f, 0.4706f, 0.7843f, 0.7000f),
            ["kauri"] = new Entry(16, 26, 0.6667f, 0.7843f, 0.4706f, 0.9412f, 0.8000f),
            ["leadwood"] = new Entry(27, 40, 0.3137f, 0.5490f, 0.1961f, 0.8235f, 0.5000f),
            ["linden"] = new Entry(2, 16, 0.6275f, 0.7843f, 0.3922f, 0.8235f, 1.0000f),
            ["mahogany"] = new Entry(27, 40, 0.7451f, 1.0000f, 0.4902f, 1.0000f, 1.2000f),
            ["mangrove"] = new Entry(29, 40, 0.6863f, 1.0000f, 0.3922f, 0.6275f, 1.0000f),
            ["microbiota"] = new Entry(-12, 6, 0.1961f, 0.7059f, 0.0000f, 1.0000f, 6.0000f),
            ["mousetrap"] = new Entry(27, 40, 0.7451f, 1.0000f, 0.0000f, 0.3922f, 60.0000f),
            ["ohia"] = new Entry(28, 40, 0.6078f, 0.9020f, 0.4314f, 0.9020f, 0.7000f),
            ["pyramidalpoplar"] = new Entry(8, 12, 0.5882f, 0.7843f, 0.0392f, 0.2353f, 2.0000f),
            ["redcedar"] = new Entry(13, 17, 0.7059f, 1.0000f, 0.2353f, 0.8627f, 13.0000f),
            ["redwillow"] = new Entry(-20, -4, 0.5490f, 1.0000f, 0.2353f, 0.7843f, 4.0000f),
            ["sal"] = new Entry(27, 40, 0.7255f, 1.0000f, 0.4510f, 1.0000f, 0.8000f),
            ["sapele"] = new Entry(27, 40, 0.7059f, 1.0000f, 0.4706f, 1.0000f, 0.8000f),
            ["satinash"] = new Entry(20, 26, 0.6667f, 1.0000f, 0.4706f, 1.0000f, 0.3000f),
            ["saxaul"] = new Entry(18, 40, 0.0000f, 0.3137f, 0.0000f, 0.5490f, 1.0000f),
            ["sourwood"] = new Entry(14, 24, 0.5882f, 0.7647f, 0.2745f, 1.0000f, 1.0000f),
            ["spruce"] = new Entry(-14, 2, 0.3529f, 0.7843f, 0.4706f, 1.0000f, 2.0000f),
            ["spurgetree"] = new Entry(28, 40, 0.2745f, 0.7451f, 0.0000f, 0.3529f, 0.8000f),
            ["sycamore"] = new Entry(12, 24, 0.5882f, 0.7647f, 0.4706f, 1.0000f, 3.0000f),
            ["tamanu"] = new Entry(29, 40, 0.7059f, 1.0000f, 0.3922f, 1.0000f, 15.0000f),
            ["thickamore"] = new Entry(13, 23, 0.5882f, 0.7647f, 0.4706f, 1.0000f, 0.1000f),
            ["thickelm"] = new Entry(14, 16, 0.3922f, 0.6863f, 0.3529f, 1.0000f, 0.0600f),
            ["thickkauri"] = new Entry(19, 21, 0.6667f, 0.7843f, 0.4706f, 0.9412f, 0.1000f),
            ["thicktamanu"] = new Entry(29, 40, 0.7255f, 1.0000f, 0.4314f, 1.0000f, 0.7000f),
            ["thinbearnut"] = new Entry(5, 20, 0.4314f, 0.6471f, 0.3922f, 0.7451f, 12.0000f),
            ["thinbirch"] = new Entry(0, 18, 0.5882f, 1.0000f, 0.0000f, 1.0000f, 3.0000f),
            ["thinlarch"] = new Entry(-24, -6, 0.3529f, 1.0000f, 0.3137f, 1.0000f, 30.0000f),
            ["thinleadwood"] = new Entry(27, 40, 0.3137f, 0.5490f, 0.1961f, 0.8627f, 0.8000f),
            ["thinmahoe"] = new Entry(23, 30, 0.6667f, 0.8627f, 0.4706f, 0.7843f, 1.0000f),
            ["thinwillow"] = new Entry(-4, 14, 0.5490f, 1.0000f, 0.2353f, 0.7059f, 3.0000f),
            ["tigerwood"] = new Entry(26, 40, 0.7255f, 1.0000f, 0.0000f, 0.2745f, 10.0000f),
            ["treeheather"] = new Entry(25, 40, 0.3137f, 0.5098f, 0.1961f, 0.7059f, 2.0000f),
            ["truefir"] = new Entry(-10, 13, 0.2941f, 0.5882f, 0.0000f, 1.0000f, 15.0000f),
            ["tuja"] = new Entry(-4, 12, 0.2157f, 0.5882f, 0.0000f, 0.9804f, 15.0000f),
            ["umnini"] = new Entry(27, 40, 0.7255f, 0.9804f, 0.4902f, 1.0000f, 0.7000f),
            ["weepingwillow"] = new Entry(6, 14, 0.5098f, 1.0000f, 0.2353f, 0.6667f, 2.0000f),
            ["wiliwili"] = new Entry(28, 40, 0.6078f, 0.9020f, 0.1961f, 0.9020f, 0.5000f),
            ["willow"] = new Entry(-4, 14, 0.5882f, 1.0000f, 0.3137f, 0.7843f, 6.0000f),
            ["yew"] = new Entry(-4, 12, 0.3529f, 0.7451f, 0.3137f, 0.6275f, 2.0000f),
        };

        static readonly Dictionary<string, string> WoodToGenerator = new Dictionary<string, string>
        {
            // Treegen property names don't always match the wood id used in blocks.
            ["poplar"] = "blackpoplar",
            ["douglasfir"] = "coastdouglasfir",
            ["tuja"] = "crookedtuja",
        };

        public static bool TryGetForWood(string wood, out Entry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(wood)) return false;
            if (ByGenerator.TryGetValue(wood, out entry)) return true;
            if (WoodToGenerator.TryGetValue(wood, out string gen) && ByGenerator.TryGetValue(gen, out entry)) return true;
            return false;
        }
    }
}

