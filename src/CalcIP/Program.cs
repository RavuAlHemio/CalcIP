using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CalcIP
{
    public static class Program
    {
        public static readonly Regex IPv4WithSubnetRegex = new Regex("^(?<addr>[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+)/(?<mask>[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+)$", RegexOptions.Compiled);
        public static readonly Regex IPv4WithCidrRegex = new Regex("^(?<addr>[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+)/(?<cidr>[0-9]+)$", RegexOptions.Compiled);
        public static readonly Regex IPv6WithSubnetRegex = new Regex("^(?<addr>[0-9a-f:]+)/(?<mask>[0-9a-f:]+)$", RegexOptions.Compiled);
        public static readonly Regex IPv6WithCidrRegex = new Regex("^(?<addr>[0-9a-f:]+)/(?<cidr>[0-9]+)$", RegexOptions.Compiled);

        public static int Main(string[] args)
        {
            try
            {
                return RealMain(args);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static int RealMain(string[] args)
        {
            if (args.Length < 1)
            {
                UsageAndExit();
            }

            if (args[0] == "-m" || args[0] == "--minimize")
            {
                return Minimize.PerformMinimize(args);
            }
            else
            {
                return ShowNet.PerformShowNet(args);
            }
        }

        public static void UsageAndExit(int exitCode = 1)
        {
            Console.Error.WriteLine(
                "Usage: CalcIP SPEC...\r\n" +
                "       CalcIP -m|--minimize SPEC...\r\n" +
                "\r\n" +
                "SPEC is one of: IPV4ADDRESS/IPV4SUBNETMASK\r\n" +
                "                IPV4ADDRESS/CIDRPREFIX\r\n" +
                "                IPV6ADDRESS/IPV6SUBNETMASK\r\n" +
                "                IPV6ADDRESS/CIDRPREFIX\r\n"
            );
            Environment.Exit(exitCode);
        }

        public static Tuple<IPv4Address, IPv4Network> ParseIPv4SubnetSpec(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string maskString = match.Groups["mask"].Value;

            var address = IPv4Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv4 address {1}", match.Value, addressString);
                return null;
            }

            var mask = IPv4Address.MaybeParse(maskString);
            if (!mask.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv4 subnet mask {1}", match.Value, maskString);
                return null;
            }

            return Tuple.Create(address.Value, new IPv4Network(address.Value, mask.Value));
        }

        public static Tuple<IPv4Address, IPv4Network> ParseIPv4CidrSpec(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string cidrString = match.Groups["cidr"].Value;

            var address = IPv4Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv4 address {1}", match.Value, addressString);
                return null;
            }

            int cidr;
            if (!int.TryParse(cidrString, NumberStyles.None, CultureInfo.InvariantCulture, out cidr))
            {
                Console.Error.WriteLine("{0}: Invalid CIDR prefix {1}", match.Value, cidrString);
                return null;
            }
            if (cidr > 32)
            {
                Console.Error.WriteLine("{0}: CIDR prefix {1} is too large (32 is the maximum for IPv4)", match.Value, cidr);
                return null;
            }

            return Tuple.Create(address.Value, new IPv4Network(address.Value, cidr));
        }

        public static Tuple<IPv6Address, IPv6Network> ParseIPv6CidrSpec(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string cidrString = match.Groups["cidr"].Value;

            var address = IPv6Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv6 address {1}", match.Value, addressString);
                return null;
            }

            int cidr;
            if (!int.TryParse(cidrString, NumberStyles.None, CultureInfo.InvariantCulture, out cidr))
            {
                Console.Error.WriteLine("{0}: Invalid CIDR prefix {1}", match.Value, cidrString);
                return null;
            }
            if (cidr > 128)
            {
                Console.Error.WriteLine("{0}: CIDR prefix {1} is too large (128 is the maximum for IPv6)", match.Value, cidr);
                return null;
            }

            return Tuple.Create(address.Value, new IPv6Network(address.Value, cidr));
        }

        public static Tuple<IPv6Address, IPv6Network> ParseIPv6SubnetSpec(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string maskString = match.Groups["mask"].Value;

            var address = IPv6Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv6 address {1}", match.Value, addressString);
                return null;
            }

            var mask = IPv6Address.MaybeParse(maskString);
            if (!mask.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv6 subnet mask {1}", match.Value, maskString);
                return null;
            }

            return Tuple.Create(address.Value, new IPv6Network(address.Value, mask.Value));
        }
    }
}
