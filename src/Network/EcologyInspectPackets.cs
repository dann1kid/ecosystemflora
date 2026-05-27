using ProtoBuf;

namespace WildFarming.Network
{
    public static class EcologyInspectChannel
    {
        public const string Name = "ecosystemflora-ecologyinspect";
    }

    [ProtoContract]
    public class EcologyInspectRequestPacket
    {
        [ProtoMember(1)]
        public int X;

        [ProtoMember(2)]
        public int Y;

        [ProtoMember(3)]
        public int Z;
    }

    [ProtoContract]
    public class EcologyInspectReportPacket
    {
        [ProtoMember(1)]
        public int X;

        [ProtoMember(2)]
        public int Y;

        [ProtoMember(3)]
        public int Z;

        [ProtoMember(4)]
        public string Species;

        [ProtoMember(5)]
        public bool InRegistry;

        [ProtoMember(6)]
        public string[] Lines;

        [ProtoMember(7)]
        public string[] ScanSpecies;

        [ProtoMember(8)]
        public int[] ScanCounts;

        [ProtoMember(9)]
        public int ScanTotal;

        [ProtoMember(10)]
        public int ScanRadius;
    }
}
