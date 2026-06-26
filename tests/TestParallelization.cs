using Xunit;

// Many tests assign the shared static EcosystemConfig.Loaded (and some exercise RNG-seeded
// shuffles). Under xUnit's default per-collection parallelism these writers race, causing
// intermittent failures (e.g. seed-dispersal scale, shuffle determinism). Disabling collection
// parallelization makes the suite deterministic; the full run is still well under a second.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
