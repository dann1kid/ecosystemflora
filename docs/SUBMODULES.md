# Git submodules

## `community/` — ecosystemfloracompat

Optional third-party ecology JSON patches (content mod). Separate repository:

`ssh://git@95.165.173.218:2222/danni1kid/ecosystemfloracompat.git`

Local sibling checkout (this machine): `D:/vintage_story_mods/ecosystemfloracompat`

### Clone main repo

```bash
git clone --recursive ssh://git@95.165.173.218:2222/danni1kid/wildlife-ecosystem.git
```

If you already cloned without submodules:

```bash
git submodule update --init community
```

### First-time publish of `ecosystemfloracompat` remote

1. Create an **empty** repo `ecosystemfloracompat` on the Git server (same account as `wildlife-ecosystem`).
2. From the submodule directory:

```bash
cd community   # or D:/vintage_story_mods/ecosystemfloracompat
git push -u origin master
```

3. Contributors clone with `--recursive` or run `git submodule update --init`.

### Work on patches

Edit files under `community/`, commit **inside** `community/` (submodule), then in the parent repo commit the updated submodule pointer:

```bash
cd community
git add -A && git commit -m "..."
cd ..
git add community
git commit -m "Bump community submodule (…)."
```

**Build:** `dotnet build community\ecosystemfloracompat\ecosystemfloracompat.csproj` or build full `wildfarming.sln` — see [`community/docs/BUILD.md`](../community/docs/BUILD.md).

## `public-config/` — vs-ecosystemflora-config

Public config reference pack (example JSON, species CSV docs). Separate repository:

`https://github.com/dann1kid/vs-ecosystemflora-config.git`

Synced into `examples/ecosystemflora-config/` for the monorepo; the live publish target is the submodule.

```bash
git submodule update --init public-config
```

## `examples/ecologysample-mynewplant`

Reference sample content mod (nested repo):

`https://github.com/dann1kid/ecologysample-mynewplant.git`

```bash
git submodule update --init examples/ecologysample-mynewplant
```
