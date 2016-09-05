using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CalcIP
{
    public static class Minimize
    {
        public static int PerformMinimize(string[] args)
        {
            if (args.Length < 2)
            {
                Program.UsageAndExit();
            }

            var firstIPv4SubnetMatch = Program.IPv4WithSubnetRegex.Match(args[1]);
            var firstIPv4CidrMatch = Program.IPv4WithCidrRegex.Match(args[1]);
            if (firstIPv4SubnetMatch.Success || firstIPv4CidrMatch.Success)
            {
                var subnets = new List<IPv4Network>();
                IPv4Network firstNet = (firstIPv4CidrMatch.Success)
                    ? Program.ParseIPv4CidrSpec(firstIPv4CidrMatch)?.Item2
                    : Program.ParseIPv4SubnetSpec(firstIPv4SubnetMatch)?.Item2;
                if (firstNet == null)
                {
                    return 1;
                }
                subnets.Add(firstNet);

                for (int i = 2; i < args.Length; ++i)
                {
                    var cidrMatch = Program.IPv4WithCidrRegex.Match(args[i]);
                    if (cidrMatch.Success)
                    {
                        var subnet = Program.ParseIPv4CidrSpec(cidrMatch)?.Item2;
                        if (subnet == null)
                        {
                            return 1;
                        }
                        subnets.Add(subnet);
                        continue;
                    }
                    
                    var subnetMatch = Program.IPv4WithSubnetRegex.Match(args[i]);
                    if (subnetMatch.Success)
                    {
                        var subnet = Program.ParseIPv4SubnetSpec(subnetMatch)?.Item2;
                        if (subnet == null)
                        {
                            return 1;
                        }
                        subnets.Add(subnet);
                        continue;
                    }

                    Console.Error.WriteLine("Could not detect IPv4 network spec type of {0}.", args[i]);
                    return 1;
                }

                MinimizeAndOutput(subnets, (addr, mask) => new IPv4Network(addr, mask));

                return 0;
            }

            var firstIPv6SubnetMatch = Program.IPv6WithSubnetRegex.Match(args[1]);
            var firstIPv6CidrMatch = Program.IPv6WithCidrRegex.Match(args[1]);
            if (firstIPv6SubnetMatch.Success || firstIPv6CidrMatch.Success)
            {
                var subnets = new List<IPv6Network>();
                IPv6Network firstNet = (firstIPv6CidrMatch.Success)
                    ? Program.ParseIPv6CidrSpec(firstIPv6CidrMatch)?.Item2
                    : Program.ParseIPv6SubnetSpec(firstIPv6SubnetMatch)?.Item2;
                if (firstNet == null)
                {
                    return 1;
                }
                subnets.Add(firstNet);

                for (int i = 2; i < args.Length; ++i)
                {
                    var cidrMatch = Program.IPv6WithCidrRegex.Match(args[i]);
                    if (cidrMatch.Success)
                    {
                        var subnet = Program.ParseIPv6CidrSpec(cidrMatch)?.Item2;
                        if (subnet == null)
                        {
                            return 1;
                        }
                        subnets.Add(subnet);
                        continue;
                    }

                    var subnetMatch = Program.IPv6WithSubnetRegex.Match(args[i]);
                    if (subnetMatch.Success)
                    {
                        var subnet = Program.ParseIPv6SubnetSpec(subnetMatch)?.Item2;
                        if (subnet == null)
                        {
                            return 1;
                        }
                        subnets.Add(subnet);
                        continue;
                    }

                    Console.Error.WriteLine("Could not detect IPv6 network spec type of {0}.", args[i]);
                    return 1;
                }

                MinimizeAndOutput(subnets, (addr, mask) => new IPv6Network(addr, mask));

                return 0;
            }

            Console.Error.WriteLine("Could not detect network spec type of {0}.", args[1]);
            return 1;
        }

        public static List<IPNetwork<TAddress>> MinimizeSubnets<TAddress>(IEnumerable<IPNetwork<TAddress>> subnets,
            Func<TAddress, TAddress, IPNetwork<TAddress>> createSubnet)
            where TAddress : struct, IIPAddress<TAddress>
        {
            List<IPNetwork<TAddress>> sortedSubnets = subnets
                .OrderBy(n => n.BaseAddress)
                .ThenBy(n => n.SubnetMask)
                .ToList();

            var filteredSubnets = new HashSet<IPNetwork<TAddress>>(sortedSubnets);

            // eliminate subsets
            for (int i = 0; i < sortedSubnets.Count; ++i)
            {
                for (int j = i + 1; j < sortedSubnets.Count; ++j)
                {
                    if (sortedSubnets[i].IsSupersetOf(sortedSubnets[j]) && !sortedSubnets[i].Equals(sortedSubnets[j]))
                    {
                        // i is a subset of j
                        filteredSubnets.Remove(sortedSubnets[j]);
                    }
                }
            }

            // try joining adjacent same-size subnets
            bool subnetsMerged = true;
            while (subnetsMerged)
            {
                subnetsMerged = false;
                sortedSubnets = filteredSubnets
                    .OrderBy(n => n.BaseAddress)
                    .ThenBy(n => n.SubnetMask)
                    .ToList();

                for (int i = 0; i < sortedSubnets.Count; ++i)
                {
                    for (int j = i + 1; j < sortedSubnets.Count; ++j)
                    {
                        if (!sortedSubnets[i].SubnetMask.Equals(sortedSubnets[j].SubnetMask))
                        {
                            // not the same size
                            continue;
                        }

                        var lastIPlusOne = sortedSubnets[i].LastAddressOfSubnet.Add(1);
                        if (!lastIPlusOne.Equals(sortedSubnets[j].BaseAddress))
                        {
                            // not adjacent
                            continue;
                        }

                        // adjacent!

                        // which bit do they differ in?
                        var differBitAddress = sortedSubnets[i].BaseAddress.BitwiseXor(sortedSubnets[j].BaseAddress);

                        // ensure it's only one bit
                        int differencePopCount = differBitAddress.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
                        if (differencePopCount > 1)
                        {
                            // not just a single-bit difference
                            continue;
                        }

                        // remove that bit from the subnet mask
                        var newSubnetMask = sortedSubnets[i].SubnetMask.BitwiseAnd(differBitAddress.BitwiseNot());

                        // create the new subnet
                        var newSubnet = createSubnet(sortedSubnets[i].BaseAddress, newSubnetMask);

                        // quick sanity check
                        Debug.Assert(newSubnet.IsSupersetOf(sortedSubnets[i]));
                        Debug.Assert(newSubnet.IsSupersetOf(sortedSubnets[j]));

                        // replace the lower subnets with the upper subnet
                        filteredSubnets.Remove(sortedSubnets[i]);
                        filteredSubnets.Remove(sortedSubnets[j]);
                        filteredSubnets.Add(newSubnet);

                        subnetsMerged = true;
                        break;
                    }

                    if (subnetsMerged)
                    {
                        break;
                    }
                }
            }

            return filteredSubnets
                .OrderBy(n => n.BaseAddress)
                .ThenBy(n => n.SubnetMask)
                .ToList();
        }

        private static void MinimizeAndOutput<TAddress>(IEnumerable<IPNetwork<TAddress>> subnets,
            Func<TAddress, TAddress, IPNetwork<TAddress>> createSubnet)
            where TAddress : struct, IIPAddress<TAddress>
        {
            List<IPNetwork<TAddress>> finalSubnets = MinimizeSubnets(subnets, createSubnet);

            foreach (IPNetwork<TAddress> subnet in finalSubnets)
            {
                Console.WriteLine("{0}/{1}", subnet.BaseAddress, subnet.CidrPrefix?.ToString() ?? subnet.SubnetMask.ToString());
            }
        }
    }
}
