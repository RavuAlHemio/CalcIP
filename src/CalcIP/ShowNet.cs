using System;
using System.Text.RegularExpressions;

namespace CalcIP
{
    public static class ShowNet
    {
        public static int PerformShowNet(string[] args)
        {
            Program.PerformOnSubnets(
                args,
                (a4, n4) => OutputIPv4Network(n4, a4),
                (a6, n6) => OutputIPv6Network(n6, a6)
            );
            return 0;
        }

        public static void OutputIPv4Network(IPNetwork<IPv4Address> net, IPv4Address? addr = null)
        {
            const int labelWidth = 11;
            const int addressWidth = 21;

            ConsoleColor originalColor = Console.ForegroundColor;

            Action<string, string> outputInitialColumns = (label, address) =>
            {
                Console.ForegroundColor = Color.Label;
                Console.Write(CalcIPUtils.PadRightTo(label, labelWidth));
                Console.ForegroundColor = Color.IPAddress;
                Console.Write(CalcIPUtils.PadRightTo(address, addressWidth));
            };

            if (addr.HasValue)
            {
                outputInitialColumns("Address:", addr.ToString());
                OutputBinaryIPv4Address(addr.Value, net.SubnetMask);
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
            }

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

            Console.ForegroundColor = originalColor;
        }

        public static void OutputIPv6Network(IPNetwork<IPv6Address> net, IPv6Address? addr = null)
        {
            const int labelWidth = 11;
            const int addressWidth = 46;

            ConsoleColor originalColor = Console.ForegroundColor;

            Action<string, string> outputInitialColumns = (label, address) =>
            {
                Console.ForegroundColor = Color.Label;
                Console.Write(CalcIPUtils.PadRightTo(label, labelWidth));
                Console.ForegroundColor = Color.IPAddress;
                Console.Write(CalcIPUtils.PadRightTo(address, addressWidth));
            };

            if (addr.HasValue)
            {
                outputInitialColumns("Address:", addr.ToString());
                OutputBinaryIPv6Address(addr.Value, net.SubnetMask);
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
            }

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

            Console.ForegroundColor = originalColor;
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
