using PcapDotNet.Base;
using PcapDotNet.Packets.Ethernet;

namespace Leho
{
    public static class NetworkConstants
    {
        public static MacAddress BroadcastMacAddress { get; } = new MacAddress(UInt48.MaxValue);
    }
}
