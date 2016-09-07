using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalcIP
{
    public static class Resize
    {
        public static int PerformResize(string[] args)
        {
            if (args.Length != 3)
            {
                Program.UsageAndExit();
            }

            var ipv4CidrMatch = Program.IPv4WithCidrRegex.Match(args[1]);
            var ipv4SubnetMatch = Program.IPv4WithSubnetRegex.Match(args[1]);
            if (ipv4CidrMatch.Success || ipv4SubnetMatch.Success)
            {
                IPv4Network initialNet = (ipv4CidrMatch.Success)
                    ? Program.ParseIPv4CidrSpec(ipv4CidrMatch)?.Item2
                    : Program.ParseIPv4SubnetSpec(ipv4SubnetMatch)?.Item2;
                if (initialNet == null)
                {
                    return 1;
                }

                int cidrPrefix;
                IPv4Address newSubnetMask;
                if (int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out cidrPrefix))
                {
                    if (cidrPrefix < 0 || cidrPrefix > 32)
                    {
                        Console.Error.WriteLine("IPv4 CIDR prefix {0} invalid; must be at last 0 and at most 32.", cidrPrefix);
                        return 1;
                    }

                    newSubnetMask = initialNet.BaseAddress.SubnetMaskFromCidrPrefix(cidrPrefix);
                }
                else
                {
                    IPv4Address? maybeSubnetMask = IPv4Address.MaybeParse(args[2]);
                    if (!maybeSubnetMask.HasValue)
                    {
                        Console.Error.WriteLine("Invalid IPv4 CIDR prefix or subnet mask spec: {0}", args[2]);
                        return 1;
                    }

                    newSubnetMask = maybeSubnetMask.Value;
                }

                int netComparison;
                List<IPNetwork<IPv4Address>> resized = ResizeNetwork(initialNet, newSubnetMask,
                    (addr, mask) => new IPv4Network(addr, mask), out netComparison);

                Console.WriteLine("Original network:");
                ShowNet.OutputIPv4Network(initialNet);
                Console.WriteLine();

                if (netComparison < 0)
                {
                    Console.WriteLine("Supernet:");
                    ShowNet.OutputIPv4Network(resized[0]);
                    Console.WriteLine();
                }
                else if (netComparison == 0)
                {
                    Console.WriteLine("Same-sized net:");
                    ShowNet.OutputIPv4Network(resized[0]);
                    Console.WriteLine();
                }
                else
                {
                    for (int i = 0; i < resized.Count; ++i)
                    {
                        Console.WriteLine("Subnet {0}:", i + 1);
                        ShowNet.OutputIPv4Network(resized[i]);
                        Console.WriteLine();
                    }
                }

                return 0;
            }

            var ipv6CidrMatch = Program.IPv6WithCidrRegex.Match(args[1]);
            var ipv6SubnetMatch = Program.IPv6WithCidrRegex.Match(args[1]);
            if (ipv6CidrMatch.Success || ipv6SubnetMatch.Success)
            {
                IPv6Network initialNet = (ipv6CidrMatch.Success)
                    ? Program.ParseIPv6CidrSpec(ipv6CidrMatch)?.Item2
                    : Program.ParseIPv6SubnetSpec(ipv6SubnetMatch)?.Item2;
                if (initialNet == null)
                {
                    return 1;
                }

                int cidrPrefix;
                IPv6Address newSubnetMask;
                if (int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out cidrPrefix))
                {
                    if (cidrPrefix < 0 || cidrPrefix > 128)
                    {
                        Console.Error.WriteLine("IPv6 CIDR prefix {0} invalid; must be at last 0 and at most 128.", cidrPrefix);
                        return 1;
                    }

                    newSubnetMask = initialNet.BaseAddress.SubnetMaskFromCidrPrefix(cidrPrefix);
                }
                else
                {
                    IPv6Address? maybeSubnetMask = IPv6Address.MaybeParse(args[2]);
                    if (!maybeSubnetMask.HasValue)
                    {
                        Console.Error.WriteLine("Invalid IPv6 CIDR prefix or subnet mask spec: {0}", args[2]);
                        return 1;
                    }

                    newSubnetMask = maybeSubnetMask.Value;
                }

                int netComparison;
                List<IPNetwork<IPv6Address>> resized = ResizeNetwork(initialNet, newSubnetMask,
                    (addr, mask) => new IPv6Network(addr, mask), out netComparison);

                Console.WriteLine("Original network:");
                ShowNet.OutputIPv6Network(initialNet);
                Console.WriteLine();

                if (netComparison < 0)
                {
                    Console.WriteLine("Supernet:");
                    ShowNet.OutputIPv6Network(resized[0]);
                    Console.WriteLine();
                }
                else if (netComparison == 0)
                {
                    Console.WriteLine("Same-sized net:");
                    ShowNet.OutputIPv6Network(resized[0]);
                    Console.WriteLine();
                }
                else
                {
                    for (int i = 0; i < resized.Count; ++i)
                    {
                        Console.WriteLine("Subnet {0}:", i + 1);
                        ShowNet.OutputIPv6Network(resized[i]);
                        Console.WriteLine();
                    }
                }

                return 0;
            }

            Console.Error.WriteLine("Could not detect network spec type of {0}.", args[1]);
            return 1;
        }

        public static List<IPNetwork<TAddress>> ResizeNetwork<TAddress>(IPNetwork<TAddress> initialNet,
            TAddress newSubnetMask, Func<TAddress, TAddress, IPNetwork<TAddress>> createSubnet, out int netComparison)
            where TAddress : struct, IIPAddress<TAddress>
        {
            int initialHostBits = initialNet.CiscoWildcard.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
            int newNetBits = newSubnetMask.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
            int newHostBits = newSubnetMask.BitwiseNot().Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);

            if (newHostBits > initialHostBits)
            {
                // supernet
                netComparison = -1;

                TAddress unraveledInitialBaseAddress = CalcIPUtils.UnravelAddress(initialNet.BaseAddress, initialNet.SubnetMask);
                IPNetwork<TAddress> unraveledShortenedNet = createSubnet(unraveledInitialBaseAddress, newSubnetMask.SubnetMaskFromCidrPrefix(newNetBits));
                TAddress wovenNewBaseAddress = CalcIPUtils.WeaveAddress(unraveledShortenedNet.BaseAddress, newSubnetMask);
                var newNet = createSubnet(wovenNewBaseAddress, newSubnetMask);

                return new List<IPNetwork<TAddress>> {newNet};
            }
            else if (newHostBits == initialHostBits)
            {
                // samenet
                netComparison = 0;

                TAddress unraveledBaseAddress = CalcIPUtils.UnravelAddress(initialNet.BaseAddress, initialNet.SubnetMask);
                TAddress wovenNewBaseAddress = CalcIPUtils.WeaveAddress(unraveledBaseAddress, newSubnetMask);
                var newNet = createSubnet(wovenNewBaseAddress, newSubnetMask);

                return new List<IPNetwork<TAddress>> {newNet};
            }
            else
            {
                // subnet(s)
                netComparison = 1;

                TAddress unraveledBaseAddress = CalcIPUtils.UnravelAddress(initialNet.BaseAddress, initialNet.SubnetMask);
                TAddress unraveledLastAddress = CalcIPUtils.UnravelAddress(initialNet.LastAddressOfSubnet, initialNet.SubnetMask);

                var ret = new List<IPNetwork<TAddress>>();

                TAddress currentUnraveledBaseAddress = unraveledBaseAddress;
                while (currentUnraveledBaseAddress.CompareTo(unraveledLastAddress) <= 0)
                {
                    TAddress wovenNewBaseAddress = CalcIPUtils.WeaveAddress(currentUnraveledBaseAddress, newSubnetMask);
                    var newNet = createSubnet(wovenNewBaseAddress, newSubnetMask);

                    ret.Add(newNet);

                    currentUnraveledBaseAddress = newNet.NextSubnetBaseAddress;
                }

                return ret;
            }
        }

        public static List<IPNetwork<TAddress>> ResizeNetwork<TAddress>(IPNetwork<TAddress> initialNet,
            int newCidrPrefix, Func<TAddress, TAddress, IPNetwork<TAddress>> createSubnet, out int netComparison)
            where TAddress : struct, IIPAddress<TAddress>
        {
            TAddress newSubnetMask = initialNet.BaseAddress.SubnetMaskFromCidrPrefix(newCidrPrefix);
            return ResizeNetwork(initialNet, newSubnetMask, createSubnet, out netComparison);
        }
    }
}
