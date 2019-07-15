using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using System;
using System.Diagnostics;
using System.Linq;

namespace Leho.Functions
{
    public class ArpRequest : IFunction
    {
        public ArpRequest(IpV4Address ipV4AddressToIdentify,
                          MacAddress requesterMacAddress, IpV4Address requesterIpV4Address,
                          Action<MacAddress> onIdentified)
        {
            if (requesterIpV4Address == ipV4AddressToIdentify)
            {
                throw new ArgumentException($"It doesn't make sense to ask {requesterIpV4Address} who {ipV4AddressToIdentify} is.");
            }

            IpV4AddressToIdentify = ipV4AddressToIdentify;
            RequesterMacAddress = requesterMacAddress;
            RequesterIpV4Address = requesterIpV4Address;
            OnIdentified = onIdentified;
            EthernetLayer = new EthernetLayer
            {
                Source = requesterMacAddress,
                Destination = NetworkConstants.BroadcastMacAddress,
                EtherType = EthernetType.Arp
            };

            ArpLayer = new ArpLayer
            {
                Operation = ArpOperation.Request,
                ProtocolType = EthernetType.IpV4,
                SenderHardwareAddress = requesterMacAddress.GetNetworkBytes().AsReadOnly(),
                SenderProtocolAddress = requesterIpV4Address.GetNetworkBytes().AsReadOnly(),
                TargetHardwareAddress = NetworkConstants.BroadcastMacAddress.GetNetworkBytes().AsReadOnly(),
                TargetProtocolAddress = ipV4AddressToIdentify.GetNetworkBytes().AsReadOnly()
            };

            PacketBuilder = new PacketBuilder(EthernetLayer, ArpLayer);
        }

        public IpV4Address IpV4AddressToIdentify { get; }
        public MacAddress RequesterMacAddress { get; }
        public IpV4Address RequesterIpV4Address { get; }

        private Action<MacAddress> OnIdentified { get; }
        private EthernetLayer EthernetLayer { get; }
        private ArpLayer ArpLayer { get; }
        private PacketBuilder PacketBuilder { get; }

        public void Execute(PacketCommunicator packetCommunicator)
        {
            MacAddress? requestedMacAddress = null;

            var timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += (sender, eventArgs) => packetCommunicator.SendPacket(PacketBuilder.Build(DateTime.Now));
            timer.Start();

            packetCommunicator.ReceivePackets(-1, packet =>
            {
                if (packet.Ethernet.EtherType == EthernetType.Arp
                && packet.Ethernet.Arp.IsValid
                && packet.Ethernet.Arp.Operation == ArpOperation.Reply
                && packet.Ethernet.Arp.SenderProtocolIpV4Address == IpV4AddressToIdentify)
                {
                    timer.Stop();

                    requestedMacAddress = new MacAddress(
                        packet.Ethernet.Arp.SenderHardwareAddress.ToArray().ReadUInt48(0, Endianity.Big)
                    );

                    packetCommunicator.Break();
                }
            });


            // TODO: What if it never arrives?
            while (requestedMacAddress == null);

            OnIdentified(requestedMacAddress.Value);
        }
    }
}
