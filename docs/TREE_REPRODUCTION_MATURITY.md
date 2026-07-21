## Wild tree reproduction maturity (spread start age)

Wild tree spread is suppressed on **young trees**. Each species has an approximate **real-world** age of first effective seed production (first cones / acorns / fruit) that we use as the default **spread maturity age**.

Implementation:

- **Code**: `WildTreeGrowthProfiles.Profile.SpreadMaturityAgeYears`
- **Gate**: `YoungTreeSpreadGate` in `src/Ecosystem/SpreadGateChain.cs`
- **Bypass**: worldgen-sized trees set `TreeStructuralSpreadBypass` at registration; soft size bypass
  (trunk ≥ ~55% of species reference, size index ≥ 40%, or `TreeYoungSpreadBypassTrunkHeight`) applies
  only for those entries. Ecology seedlings start with the flag off and must wait for calendar maturity
  (or a full structure-age estimate), even after yearly growth.

### Defaults (years since ecology registration)

| Wood | Spread maturity (years) | Notes / sources |
|------|--------------------------|-----------------|
| birch | 15 | Birch seed-bearing commonly starts ~10–15y (USFS Betula; Forest Research silver birch). |
| oak | 25 | Oaks often start acorns ~20–30y in natural settings (extension literature). |
| maple | 20 | Maples vary widely; we model temperate maple as later-producing. |
| crimsonkingmaple | 10 | Norway maple seed production begins ~10y (species), cultivar yields vary. |
| pine | 15 | Many pines begin cones ~10–15y (Silvics references). |
| larch | 15 | European larch sexual maturity ~15y in open stands (EUFORGEN). |
| redwood | 20 | Cones can appear earlier, but “good seed-bearing” is often cited ~20y (Silvics). |
| baldcypress | 25 | Cone/seed production commonly ~20–30y. |
| greenspirecypress | 10 | Cupressus sempervirens female cones can start very early; stable monoecy ~10y. |
| acacia | 5 | Many Acacia spp. produce seed pods within a few years; strong crops later. |
| kapok | 6 | Ceiba pentandra flowers ~4–6y; stronger yields later. |
| ebony | 8 | Ebony (Diospyros) is slow-growing; managed stands can fruit earlier. |
| purpleheart | 10 | Tropical emergent hardwood; onset varies, mast behavior common. |
| walnut | 7 | Walnuts often begin small crops ~5–7y; larger crops later. |

### Config fallback

If a wood is unknown or has `SpreadMaturityAgeYears = 0`, the gate uses `TreeMinSpreadAgeYears` as fallback.

