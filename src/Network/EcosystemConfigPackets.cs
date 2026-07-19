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
}
