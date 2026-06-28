using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    public class SpeciesEcologyServerSystem : ModSystem
    {
        public const string ReloadCommandCode = "ecospeciesreload";

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.ChatCommands
                .GetOrCreate(ReloadCommandCode)
                .WithDescription(Lang.Get("ecosystemflora:species-reload-command-desc"))
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(args =>
                {
                    IServerPlayer player = args.Caller.Player as IServerPlayer;
                    if (player == null)
                    {
                        return TextCommandResult.Error(Lang.Get("ecosystemflora:species-reload-error-noplayer"));
                    }

                    if (!SpeciesEcologyLoadService.TryReload(player.Entity.Api, out int ecologyCount, out int seasonCount))
                    {
                        return TextCommandResult.Error(Lang.Get("ecosystemflora:species-reload-error-failed"));
                    }

                    return TextCommandResult.Success(
                        Lang.Get("ecosystemflora:species-reload-success", ecologyCount, seasonCount));
                });
        }
    }
}
