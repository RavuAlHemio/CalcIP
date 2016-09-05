using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace CalcIP
{
    public static class Split
    {
        public static int PerformSplit(string[] args)
        {
            if (args.Length < 3)
            {
                Program.UsageAndExit();
            }

            var ipv4Match = Program.IPv4WithCidrRegex.Match(args[1]);
            if (ipv4Match.Success)
            {
                Tuple<IPv4Address, IPv4Network> addressAndNet = Program.ParseIPv4CidrSpec(ipv4Match);
                if (addressAndNet == null)
                {
                    return 1;
                }

                Console.WriteLine("Subnet to split:");
                ShowNet.OutputIPv4Network(addressAndNet.Item2.BaseAddress, addressAndNet.Item2);
                Console.WriteLine();

                BigInteger[] splits = ParseHostCountSpecs(args);
                if (splits == null)
                {
                    return 1;
                }

                IPNetwork<IPv4Address>[] subnets = SplitSubnet(addressAndNet.Item2, splits,
                    (addr, cidr) => new IPv4Network(addr, cidr));
                if (subnets == null)
                {
                    Console.WriteLine("Not enough addresses available for this split.");
                    return 0;
                }

                foreach (Tuple<BigInteger, IPNetwork<IPv4Address>> splitAndSubnet in splits.Zip(subnets, Tuple.Create))
                {
                    Console.WriteLine("Subnet for {0} hosts:", splitAndSubnet.Item1);
                    ShowNet.OutputIPv4Network(splitAndSubnet.Item2.BaseAddress, splitAndSubnet.Item2);
                    Console.WriteLine();
                }

                Console.WriteLine("Unused networks:");
                var maxUsedAddress = subnets
                    .Select(s => s.LastAddressOfSubnet)
                    .Max();
                var nextUnusedAddress = maxUsedAddress.Add(1);
                List<IPNetwork<IPv4Address>> unusedSubnets = Derange.RangeToSubnets(nextUnusedAddress,
                    addressAndNet.Item2.LastAddressOfSubnet, (addr, cidr) => new IPv4Network(addr, cidr));

                foreach (IPNetwork<IPv4Address> unusedSubnet in unusedSubnets)
                {
                    Console.WriteLine("{0}/{1}", unusedSubnet.BaseAddress, unusedSubnet.CidrPrefix.Value);
                }

                return 0;
            }

            var ipv6Match = Program.IPv6WithCidrRegex.Match(args[1]);
            if (ipv6Match.Success)
            {
                Tuple<IPv6Address, IPv6Network> addressAndNet = Program.ParseIPv6CidrSpec(ipv6Match);
                if (addressAndNet == null)
                {
                    return 1;
                }

                Console.WriteLine("Subnet to split:");
                ShowNet.OutputIPv6Network(addressAndNet.Item2.BaseAddress, addressAndNet.Item2);

                BigInteger[] splits = ParseHostCountSpecs(args);
                if (splits == null)
                {
                    return 1;
                }

                IPNetwork<IPv6Address>[] subnets = SplitSubnet(addressAndNet.Item2, splits,
                    (addr, cidr) => new IPv6Network(addr, cidr));
                if (subnets == null)
                {
                    Console.WriteLine("Not enough addresses available for this split.");
                    return 0;
                }

                foreach (Tuple<BigInteger, IPNetwork<IPv6Address>> splitAndSubnet in splits.Zip(subnets, Tuple.Create))
                {
                    Console.WriteLine("Subnet for {0} hosts:", splitAndSubnet.Item1);
                    ShowNet.OutputIPv6Network(addressAndNet.Item2.BaseAddress, addressAndNet.Item2);
                    Console.WriteLine();
                }

                Console.WriteLine("Unused networks:");
                var maxUsedAddress = subnets
                    .Select(s => s.LastAddressOfSubnet)
                    .Max();
                var nextUnusedAddress = maxUsedAddress.Add(1);
                List<IPNetwork<IPv6Address>> unusedSubnets = Derange.RangeToSubnets(nextUnusedAddress,
                    addressAndNet.Item2.SubnetMask, (addr, cidr) => new IPv6Network(addr, cidr));

                foreach (IPNetwork<IPv6Address> unusedSubnet in unusedSubnets)
                {
                    Console.WriteLine("{0}/{1}", unusedSubnet.BaseAddress, unusedSubnet.CidrPrefix.Value);
                }

                return 0;
            }

            Console.Error.WriteLine("Failed to parse {0} as a subnet specification.", args[1]);
            return 1;
        }

        private static BigInteger[] ParseHostCountSpecs(string[] args)
        {
            var ret = new BigInteger[args.Length - 2];
            for (int i = 0; i < ret.Length; ++i)
            {
                if (!BigInteger.TryParse(args[i+2], NumberStyles.None, CultureInfo.InvariantCulture, out ret[i]))
                {
                    Console.Error.WriteLine("Failed to parse {0} as a number.", args[i+2]);
                    return null;
                }
                if (ret[i] < 0)
                {
                    Console.Error.WriteLine("Host counts must be greater than zero, got {0}.", ret[i]);
                    return null;
                }
            }
            return ret;
        }

        public static IPNetwork<TAddress>[] SplitSubnet<TAddress>(IPNetwork<TAddress> subnet, BigInteger[] hostCounts,
            Func<TAddress, int, IPNetwork<TAddress>> createSubnet)
            where TAddress : struct, IIPAddress<TAddress>
        {
            var ret = new IPNetwork<TAddress>[hostCounts.Length];

            // sort descending by size
            var indexesAndHostCounts = hostCounts
                .Select((count, i) => Tuple.Create(i, count))
                .OrderByDescending(hc => hc.Item2)
                .ToList();

            IPNetwork<TAddress> currentNet = createSubnet(subnet.BaseAddress, 8*subnet.SubnetMask.Bytes.Length);
            foreach (Tuple<int, BigInteger> indexAndHostCount in indexesAndHostCounts)
            {
                while (currentNet.HostCount < indexAndHostCount.Item2 && currentNet.CidrPrefix.Value >= 0)
                {
                    currentNet = createSubnet(currentNet.BaseAddress, currentNet.CidrPrefix.Value - 1);
                }

                if (currentNet.CidrPrefix.Value == 0)
                {
                    // this won't fit
                    return null;
                }

                // we fit!
                ret[indexAndHostCount.Item1] = currentNet;

                currentNet = createSubnet(currentNet.NextSubnetBaseAddress, 8*subnet.SubnetMask.Bytes.Length);
            }

            return ret;
        }
    }
}
