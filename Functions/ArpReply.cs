using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using System;

namespace Leho.Functions
{
    public class ArpReply : IFunction
    {
        public ArpReply(MacAddress sourceMacAddress, IpV4Address sourceIpv4Address,
                        MacAddress targetMacAddress, IpV4Address targetIpv4Address)
        {
            SourceMacAddress = sourceMacAddress;
            SourceIpv4Address = sourceIpv4Address;
            TargetMacAddress = targetMacAddress;
            TargetIpv4Address = targetIpv4Address;

            EthernetLayer = new EthernetLayer
            {
                Source = SourceMacAddress,
                Destination = targetMacAddress,
                EtherType = EthernetType.Arp
            };

            ArpLayer = new ArpLayer
            {
                Operation = ArpOperation.Reply,
                ProtocolType = EthernetType.IpV4,
                SenderHardwareAddress = SourceMacAddress.GetNetworkBytes().AsReadOnly(),
                SenderProtocolAddress = SourceIpv4Address.GetNetworkBytes().AsReadOnly(),
                TargetHardwareAddress = TargetMacAddress.GetNetworkBytes().AsReadOnly(),
                TargetProtocolAddress = TargetIpv4Address.GetNetworkBytes().AsReadOnly()
            };

            PacketBuilder = new PacketBuilder(EthernetLayer, ArpLayer);
        }

        public MacAddress SourceMacAddress { get; }
        public IpV4Address SourceIpv4Address { get; }
        public MacAddress TargetMacAddress { get; }
        public IpV4Address TargetIpv4Address { get; }

        private EthernetLayer EthernetLayer { get; }
        private ArpLayer ArpLayer { get; }
        private PacketBuilder PacketBuilder { get; }

        public void Execute(PacketCommunicator packetCommunicator)
        {
            packetCommunicator.SendPacket(PacketBuilder.Build(DateTime.Now));
        }
    }
}
