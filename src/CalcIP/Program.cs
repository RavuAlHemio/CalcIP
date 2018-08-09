using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CalcIP
{
    public static class Program
    {
        public static readonly Regex IPv4WithSubnetRegex = new Regex("^(?<addr>[0-9]+(?:[.][0-9]+){3})/(?<wildcard>-)?(?<mask>[0-9]+(?:[.][0-9]+){3})$", RegexOptions.Compiled);
        public static readonly Regex IPv4WithCidrRegex = new Regex("^(?<addr>[0-9]+(?:[.][0-9]+){3})/(?<cidr>[0-9]+)$", RegexOptions.Compiled);
        public static readonly Regex IPv6WithSubnetRegex = new Regex("^(?<addr>[0-9a-f:]+)/(?<wildcard>-)?(?<mask>[0-9a-f:]+)$", RegexOptions.Compiled);
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

            if (args[0] == "--stdin")
            {
                return RunFromStdin();
            }

            return RunSingleArgs(args);
        }

        public static int RunFromStdin()
        {
            int retCode = 0;

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                // split on whitespace, removing empty entries
                string[] args = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0)
                {
                    Console.WriteLine();
                    continue;
                }

                int lineRetCode = RunSingleArgs(args);
                if (retCode < lineRetCode)
                {
                    retCode = lineRetCode;
                }
            }

            return retCode;
        }

        public static int RunSingleArgs(string[] args)
        {
            if (args[0] == "-m" || args[0] == "--minimize")
            {
                return Minimize.PerformMinimize(args);
            }
            else if (args[0] == "-d" || args[0] == "--derange")
            {
                return Derange.PerformDerange(args);
            }
            else if (args[0] == "-s" || args[0] == "--split")
            {
                return Split.PerformSplit(args);
            }
            else if (args[0] == "-r" || args[0] == "--resize")
            {
                return Resize.PerformResize(args);
            }
            else if (args[0] == "-e" || args[0] == "--enumerate")
            {
                return Enumerate.PerformEnumerate(args);
            }
            else
            {
                return ShowNet.PerformShowNet(args);
            }
        }

        public static void UsageAndExit(int exitCode = 1)
        {
            Console.Error.WriteLine(
                "Usage: CalcIP IPADDRESS/SUBNET...\r\n" +
                "       CalcIP -m|--minimize IPADDRESS/SUBNET...\r\n" +
                "       CalcIP -d|--derange IPADDRESS IPADDRESS\r\n" +
                "       CalcIP -s|--split IPADDRESS/CIDRPREFIX HOSTCOUNT...\r\n" +
                "       CalcIP -r|--resize IPADDRESS/SUBNET SUBNET\r\n" +
                "       CalcIP -e|--enumerate IPADDRESS/SUBNET\r\n" +
                "       CalcIP --stdin\r\n" +
                "\r\n" +
                "SUBNET is one of: SUBNETMASK\r\n" +
                "                  CIDRPREFIX\r\n" +
                "                  -WILDCARD\r\n" +
                "\r\n" +
                "IPv4 and IPv6 are supported, but cannot be mixed within an invocation.\r\n"
            );
            Environment.Exit(exitCode);
        }

        public static Tuple<IPv4Address, IPv4Network> ParseIPv4SubnetSpec(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string maskString = match.Groups["mask"].Value;
            bool isWildcard = match.Groups["wildcard"].Success;

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

            if (isWildcard)
            {
                mask = ~mask.Value;
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
            bool isWildcard = match.Groups["wildcard"].Success;

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

            if (isWildcard)
            {
                mask = ~mask.Value;
            }

            return Tuple.Create(address.Value, new IPv6Network(address.Value, mask.Value));
        }

        public static void PerformOnSubnets(IEnumerable<string> subnetSpecs,
            Action<IPv4Address, IPv4Network> ipv4Action, Action<IPv6Address, IPv6Network> ipv6Action)
        {
            foreach (string spec in subnetSpecs.Skip(1))
            {
                // attempt to identify the input format

                // scope
                {
                    Match ipv4CidrMatch = Program.IPv4WithCidrRegex.Match(spec);
                    if (ipv4CidrMatch.Success)
                    {
                        Tuple<IPv4Address, IPv4Network> ipv4Tuple = ParseIPv4CidrSpec(ipv4CidrMatch);
                        if (ipv4Tuple != null)
                        {
                            ipv4Action.Invoke(ipv4Tuple.Item1, ipv4Tuple.Item2);
                        }
                        continue;
                    }
                }

                // scope
                {
                    Match ipv4SubnetMatch = Program.IPv4WithSubnetRegex.Match(spec);
                    if (ipv4SubnetMatch.Success)
                    {
                        Tuple<IPv4Address, IPv4Network> ipv4Tuple = ParseIPv4SubnetSpec(ipv4SubnetMatch);
                        if (ipv4Tuple != null)
                        {
                            ipv4Action.Invoke(ipv4Tuple.Item1, ipv4Tuple.Item2);
                        }
                        continue;
                    }
                }

                // scope
                {
                    Match ipv6CidrMatch = Program.IPv6WithCidrRegex.Match(spec);
                    if (ipv6CidrMatch.Success)
                    {
                        Tuple<IPv6Address, IPv6Network> ipv6Tuple = ParseIPv6CidrSpec(ipv6CidrMatch);
                        if (ipv6Tuple != null)
                        {
                            ipv6Action.Invoke(ipv6Tuple.Item1, ipv6Tuple.Item2);
                        }
                        continue;
                    }
                }

                // scope
                {
                    Match ipv6SubnetMatch = Program.IPv6WithSubnetRegex.Match(spec);
                    if (ipv6SubnetMatch.Success)
                    {
                        Tuple<IPv6Address, IPv6Network> ipv6Tuple = ParseIPv6SubnetSpec(ipv6SubnetMatch);
                        if (ipv6Tuple != null)
                        {
                            ipv6Action.Invoke(ipv6Tuple.Item1, ipv6Tuple.Item2);
                        }
                        continue;
                    }
                }

                Console.Error.WriteLine("Failed to identify {0} input type.", spec);
            }
        }
    }
}
