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

        public static void Main(string[] args)
        {
            try
            {
                RealMain(args);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static void RealMain(string[] args)
        {
            if (args.Length < 1)
            {
                UsageAndExit();
            }

            foreach (string spec in args)
            {
                // attempt to identify the input format

                var ipv4CidrMatch = IPv4WithCidrRegex.Match(spec);
                if (ipv4CidrMatch.Success)
                {
                    ProcessIPv4Cidr(ipv4CidrMatch);
                    continue;
                }

                var ipv4SubnetMatch = IPv4WithSubnetRegex.Match(spec);
                if (ipv4SubnetMatch.Success)
                {
                    ProcessIPv4Subnet(ipv4SubnetMatch);
                    continue;
                }

                var ipv6CidrMatch = IPv6WithCidrRegex.Match(spec);
                if (ipv6CidrMatch.Success)
                {
                    ProcessIPv6Cidr(ipv6CidrMatch);
                    continue;
                }

                var ipv6SubnetMatch = IPv6WithSubnetRegex.Match(spec);
                if (ipv6SubnetMatch.Success)
                {
                    ProcessIPv6Subnet(ipv6SubnetMatch);
                    continue;
                }

                Console.Error.WriteLine("Failed to identify {0} input type.", spec);
            }
        }

        private static void UsageAndExit(int exitCode = 1)
        {
            Console.Error.WriteLine(
                "Usage: CalcIP IPV4ADDRESS/IPV4SUBNETMASK\r\n" +
                "       CalcIP IPV4ADDRESS/CIDRPREFIX\r\n" +
                "       CalcIP IPV6ADDRESS/IPV6SUBNETMASK\r\n" +
                "       CalcIP IPV6ADDRESS/CIDRPREFIX\r\n"
            );
            Environment.Exit(exitCode);
        }

        private static void ProcessIPv4Cidr(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string cidrString = match.Groups["cidr"].Value;

            var address = IPv4Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv4 address {1}", match.Value, addressString);
                return;
            }

            int cidr;
            if (!int.TryParse(cidrString, NumberStyles.None, CultureInfo.InvariantCulture, out cidr))
            {
                Console.Error.WriteLine("{0}: Invalid CIDR prefix {1}", match.Value, cidrString);
                return;
            }
            if (cidr > 32)
            {
                Console.Error.WriteLine("{0}: CIDR prefix {1} is too large (32 is the maximum for IPv4)", match.Value, cidr);
                return;
            }

            var net = new IPv4Network(address.Value, cidr);
            OutputIPv4Network(address.Value, net);
        }

        private static void ProcessIPv4Subnet(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string maskString = match.Groups["mask"].Value;

            var address = IPv4Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv4 address {1}", match.Value, addressString);
                return;
            }

            var mask = IPv4Address.MaybeParse(maskString);
            if (!mask.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv4 subnet mask {1}", match.Value, maskString);
                return;
            }

            var net = new IPv4Network(address.Value, mask.Value);
            OutputIPv4Network(address.Value, net);
        }

        private static void ProcessIPv6Cidr(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string cidrString = match.Groups["cidr"].Value;

            var address = IPv6Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv6 address {1}", match.Value, addressString);
                return;
            }

            int cidr;
            if (!int.TryParse(cidrString, NumberStyles.None, CultureInfo.InvariantCulture, out cidr))
            {
                Console.Error.WriteLine("{0}: Invalid CIDR prefix {1}", match.Value, cidrString);
                return;
            }
            if (cidr > 128)
            {
                Console.Error.WriteLine("{0}: CIDR prefix {1} is too large (128 is the maximum for IPv6)", match.Value, cidr);
                return;
            }

            var net = new IPv6Network(address.Value, cidr);
            OutputIPv6Network(address.Value, net);
        }

        private static void ProcessIPv6Subnet(Match match)
        {
            string addressString = match.Groups["addr"].Value;
            string maskString = match.Groups["mask"].Value;

            var address = IPv6Address.MaybeParse(addressString);
            if (!address.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv6 address {1}", match.Value, addressString);
                return;
            }

            var mask = IPv6Address.MaybeParse(maskString);
            if (!mask.HasValue)
            {
                Console.Error.WriteLine("{0}: Invalid IPv6 subnet mask {1}", match.Value, maskString);
                return;
            }

            var net = new IPv6Network(address.Value, mask.Value);
            OutputIPv6Network(address.Value, net);
        }

        private static void OutputIPv4Network(IPv4Address addr, IPv4Network net)
        {
            const int labelWidth = 11;
            const int addressWidth = 21;

            Action<string, string> outputInitialColumns = (label, address) =>
            {
                Console.ForegroundColor = Color.Label;
                Console.Write(CalcIPUtils.PadRightTo(label, labelWidth));
                Console.ForegroundColor = Color.IPAddress;
                Console.Write(CalcIPUtils.PadRightTo(address, addressWidth));
            };

            outputInitialColumns("Address:", addr.ToString());
            OutputBinaryIPv4Address(addr, net.SubnetMask);
            Console.WriteLine();

            outputInitialColumns(
                "Netmask:",
                net.CidrPrefix.HasValue
                    ? string.Format("{0} = {1}", net.SubnetMask, net.CidrPrefix.Value)
                    : net.SubnetMask.ToString()
            );
            OutputBinaryIPv4Address(net.SubnetMask, overrideColor: Color.MaskBits);
            Console.WriteLine();

            outputInitialColumns("Wildcard:", net.CiscoWildcard.ToString());
            OutputBinaryIPv4Address(net.CiscoWildcard);
            Console.WriteLine();

            Console.ForegroundColor = Color.Label;
            Console.WriteLine("=>");

            outputInitialColumns(
                "Network:",
                net.CidrPrefix.HasValue
                    ? string.Format("{0}/{1}", net.BaseAddress, net.CidrPrefix.Value)
                    : net.SubnetMask.ToString()
            );
            OutputBinaryIPv4Address(net.BaseAddress, net.SubnetMask, colorClass: true);
            Console.WriteLine();

            if (net.FirstHostAddress.HasValue)
            {
                outputInitialColumns("HostMin:", net.FirstHostAddress.Value.ToString());
                OutputBinaryIPv4Address(net.FirstHostAddress.Value);
                Console.WriteLine();
                outputInitialColumns("HostMax:", net.LastHostAddress.Value.ToString());
                OutputBinaryIPv4Address(net.LastHostAddress.Value);
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = Color.Label;
                Console.WriteLine("no hosts");
            }

            if (net.BroadcastAddress.HasValue)
            {
                outputInitialColumns("Broadcast:", net.BroadcastAddress.Value.ToString());
                OutputBinaryIPv4Address(net.BroadcastAddress.Value);
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = Color.Label;
                Console.WriteLine("no broadcast");
            }

            if (net.HostCount.CompareTo(0) > 0)
            {
                outputInitialColumns("Hosts/Net:", net.HostCount.ToString());
                var topBits = CalcIPUtils.ByteToBinary(net.BaseAddress.Bytes[0]);
                var topMaskBits = CalcIPUtils.ByteToBinary(net.SubnetMask.Bytes[0]);
                Console.ForegroundColor = Color.ClassBits;
                if (topBits.StartsWith("0") && topMaskBits.StartsWith("1"))
                {
                    Console.Write("Class A");
                }
                else if (topBits.StartsWith("10") && topMaskBits.StartsWith("11"))
                {
                    Console.Write("Class B");
                }
                else if (topBits.StartsWith("110") && topMaskBits.StartsWith("111"))
                {
                    Console.Write("Class C");
                }
                else if (topMaskBits.StartsWith("1111"))
                {
                    if (topBits.StartsWith("1110"))
                    {
                        Console.Write("Class D (multicast)");
                    }
                    else if (topBits.StartsWith("1111"))
                    {
                        Console.Write("Class E (reserved)");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = Color.Label;
                Console.WriteLine("no hosts/net");
            }
        }

        private static void OutputIPv6Network(IPv6Address addr, IPv6Network net)
        {
            const int labelWidth = 11;
            const int addressWidth = 46;

            Action<string, string> outputInitialColumns = (label, address) =>
            {
                Console.ForegroundColor = Color.Label;
                Console.Write(CalcIPUtils.PadRightTo(label, labelWidth));
                Console.ForegroundColor = Color.IPAddress;
                Console.Write(CalcIPUtils.PadRightTo(address, addressWidth));
            };

            outputInitialColumns("Address:", addr.ToString());
            OutputBinaryIPv6Address(addr, net.SubnetMask);
            Console.WriteLine();

            outputInitialColumns(
                "Netmask:",
                net.CidrPrefix.HasValue
                    ? string.Format("{0} = {1}", net.SubnetMask, net.CidrPrefix.Value)
                    : net.SubnetMask.ToString()
            );
            OutputBinaryIPv6Address(net.SubnetMask, overrideColor: Color.MaskBits);
            Console.WriteLine();

            outputInitialColumns("Wildcard:", net.CiscoWildcard.ToString());
            OutputBinaryIPv6Address(net.CiscoWildcard);
            Console.WriteLine();

            Console.ForegroundColor = Color.Label;
            Console.WriteLine("=>");

            outputInitialColumns(
                "Network:",
                net.CidrPrefix.HasValue
                    ? string.Format("{0}/{1}", net.BaseAddress, net.CidrPrefix.Value)
                    : net.SubnetMask.ToString()
            );
            OutputBinaryIPv6Address(net.BaseAddress, net.SubnetMask);
            Console.WriteLine();

            if (net.FirstHostAddress.HasValue)
            {
                outputInitialColumns("HostMin:", net.FirstHostAddress.Value.ToString());
                OutputBinaryIPv6Address(net.FirstHostAddress.Value);
                Console.WriteLine();
                outputInitialColumns("HostMax:", net.LastHostAddress.Value.ToString());
                OutputBinaryIPv6Address(net.LastHostAddress.Value);
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = Color.Label;
                Console.WriteLine("no hosts");
            }

            if (net.BroadcastAddress.HasValue)
            {
                outputInitialColumns("Broadcast:", net.BroadcastAddress.Value.ToString());
                OutputBinaryIPv6Address(net.BroadcastAddress.Value);
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = Color.Label;
                Console.WriteLine("no broadcast");
            }

            if (net.HostCount.CompareTo(0) > 0)
            {
                outputInitialColumns("Hosts/Net:", net.HostCount.ToString());
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = Color.Label;
                Console.WriteLine("no hosts/net");
            }
        }

        private static void OutputBinaryIPv4Address(IPv4Address addr, IPv4Address? subnetMask = null, bool colorClass = false,
            ConsoleColor? overrideColor = null)
        {
            byte[] bytes = addr.Bytes;
            byte[] maskBytes = subnetMask?.Bytes;

            for (int i = 0; i < bytes.Length; ++i)
            {
                byte b = bytes[i];
                byte? m = maskBytes?[i];
                
                string bits = CalcIPUtils.ByteToBinary(b);
                string maskBits = m.HasValue
                    ? CalcIPUtils.ByteToBinary(m.Value)
                    : null;

                if (overrideColor.HasValue)
                {
                    // simply output the address
                    Console.ForegroundColor = overrideColor.Value;
                    Console.Write(bits);
                }
                else if (maskBits == null)
                {
                    // simple output here too
                    Console.ForegroundColor = Color.HostBits;
                    Console.Write(bits);
                }
                else
                {
                    // we must differentiate

                    if (i == 0 && colorClass)
                    {
                        // check if this is a classful network
                        if (maskBits[0] == '0')
                        {
                            // first bit isn't part of the network
                            colorClass = false;
                        }
                        else if (bits[0] == '1' && maskBits[1] == '0')
                        {
                            // first bit, 1, is part of the network, but second isn't
                            colorClass = false;
                        }
                        else if (bits[1] == '1' && maskBits[2] == '0')
                        {
                            // first two bits, both 1, are part of the network, but third isn't
                            colorClass = false;
                        }
                        else if (bits[2] == '1' && maskBits[3] == '0')
                        {
                            // first three bits, all 1, are part of the network, but fourth isn't
                            colorClass = false;
                        }
                    }

                    for (int bit = 0; bit < 8; ++bit)
                    {
                        // assign color
                        if (maskBits != null && maskBits[bit] == '1')
                        {
                            Console.ForegroundColor = Color.NetBits;
                        }
                        else
                        {
                            Console.ForegroundColor = Color.HostBits;
                        }

                        if (i == 0 && colorClass)
                        {
                            // the old-style class might be relevant
                            if (bit == 0)
                            {
                                Console.ForegroundColor = Color.ClassBits;
                            }
                            else if (bit == 1 && bits[0] == '1')
                            {
                                Console.ForegroundColor = Color.ClassBits;
                            }
                            else if (bit == 2 && bits.StartsWith("11"))
                            {
                                Console.ForegroundColor = Color.ClassBits;
                            }
                            else if (bit == 3 && bits.StartsWith("111"))
                            {
                                Console.ForegroundColor = Color.ClassBits;
                            }
                        }

                        Console.Write(bits[bit]);
                    }
                }

                if (i < bytes.Length - 1)
                {
                    // add separator (dot)
                    Console.ForegroundColor = Color.AddressSeparator;
                    Console.Write('.');
                }
            }
        }

        private static void OutputBinaryIPv6Address(IPv6Address addr, IPv6Address? subnetMask = null,
            ConsoleColor? overrideColor = null)
        {
            ushort[] chunks = addr.Chunks;
            ushort[] maskChunks = subnetMask?.Chunks;

            for (int i = 0; i < chunks.Length; ++i)
            {
                ushort b = chunks[i];
                ushort? m = maskChunks?[i];
                
                string bits = CalcIPUtils.UInt16ToBinary(b);
                string maskBits = m.HasValue
                    ? CalcIPUtils.UInt16ToBinary(m.Value)
                    : null;

                if (overrideColor.HasValue)
                {
                    // simply output the address
                    Console.ForegroundColor = overrideColor.Value;
                    Console.Write(bits);
                }
                else if (maskBits == null)
                {
                    // simple output here too
                    Console.ForegroundColor = Color.HostBits;
                    Console.Write(bits);
                }
                else
                {
                    // we must differentiate

                    for (int bit = 0; bit < 16; ++bit)
                    {
                        // assign color
                        if (maskBits != null && maskBits[bit] == '1')
                        {
                            Console.ForegroundColor = Color.NetBits;
                        }
                        else
                        {
                            Console.ForegroundColor = Color.HostBits;
                        }

                        Console.Write(bits[bit]);
                    }
                }

                if (i < chunks.Length - 1)
                {
                    // add separator (colon)
                    Console.ForegroundColor = Color.AddressSeparator;
                    Console.Write(':');
                }
            }
        }

        private static class Color
        {
            public const ConsoleColor Label = ConsoleColor.White;
            public const ConsoleColor IPAddress = ConsoleColor.Blue;
            public const ConsoleColor HostBits = ConsoleColor.Yellow;
            public const ConsoleColor NetBits = ConsoleColor.Green;
            public const ConsoleColor MaskBits = ConsoleColor.Red;
            public const ConsoleColor ClassBits = ConsoleColor.Magenta;
            public const ConsoleColor AddressSeparator = ConsoleColor.White;
        }
    }
}
