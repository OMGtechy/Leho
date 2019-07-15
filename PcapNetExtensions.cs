using PcapDotNet.Base;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using System;
using System.Diagnostics;
using System.Linq;

namespace Leho
{
    public static class PcapNetExtensions
    {
        public static byte[] GetBytes(this UInt48 uInt48)
        {
            Debug.Assert(BitConverter.IsLittleEndian);

            var ulongBytes = BitConverter.GetBytes((ulong)uInt48);
            var uInt48Bytes = ulongBytes.Take(6).ToArray();

            return uInt48Bytes;
        }

        public static byte[] GetBytes(this MacAddress macAddress)
        {
            return macAddress.ToValue().GetBytes();
        }

        public static byte[] GetNetworkBytes(this MacAddress macAddress)
        {
            return GetBytes(macAddress).Reverse().ToArray();
        }

        public static byte[] GetBytes(this IpV4Address ipV4Address)
        {
            Debug.Assert(BitConverter.IsLittleEndian);

            var value = ipV4Address.ToValue();
            var uintBytes = BitConverter.GetBytes(value);

            return uintBytes;
        }

        public static byte[] GetNetworkBytes(this IpV4Address ipV4Address)
        {
            return ipV4Address.GetBytes().Reverse().ToArray();
        }
    }
}
