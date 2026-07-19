using ProtoBuf;

namespace WildFarming.Network
{
    public static class EcosystemConfigChannel
    {
        public const string Name = "ecosystemflora-config";
    }

    [ProtoContract]
    public class EcosystemConfigSyncRequestPacket
    {
    }

    [ProtoContract]
    public class EcosystemConfigSyncResponsePacket
    {
        [ProtoMember(1)]
        public string ConfigJson;

        [ProtoMember(2)]
        public string ErrorLangKey;

        [ProtoMember(3)]
        public bool CanEditConfig;

        /// <summary>
        /// False when the server has not finished loading per-world config yet
        /// (client must not treat template defaults as authoritative, especially SetupWizardCompleted).
        /// </summary>
        [ProtoMember(4)]
        public bool WorldConfigReady;
    }

    [ProtoContract]
    public class EcosystemConfigSaveRequestPacket
    {
        [ProtoMember(1)]
        public string ConfigJson;
    }

    [ProtoContract]
    public class EcosystemConfigSaveResponsePacket
    {
        [ProtoMember(1)]
        public bool Ok;

        [ProtoMember(2)]
        public string ErrorLangKey;

        [ProtoMember(3)]
        public string ConfigJson;
    }

    /// <summary>Server → client: open the setup wizard GUI.</summary>
    [ProtoContract]
    public class EcosystemOpenSetupWizardPacket
    {
    }

    /// <summary>Server → client: open the U config dialog.</summary>
    [ProtoContract]
    public class EcosystemOpenConfigDialogPacket
    {
    }
}
