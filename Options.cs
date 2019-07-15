using CommandLine;

namespace Leho
{
    public class Options
    {
        [Option(
            longName: "interface",
            Required = true,
            HelpText = "The network interface to use for all operations.")]
        public string RequestedNetworkInterface { get; set; }

        [Option(
            longName: "arp-mitm",
            HelpText = "The IP of the machine you want to execute a MITM attack on via ARP spoofing.")]
        public string ArpMitmVictimIpV4 { get; set; }
    }
}
