using System;
using System.Collections.Generic;

namespace CalcIP
{
    public static class Derange
    {
        public static int PerformDerange(string[] args)
        {
            if (args.Length != 3)
            {
                Program.UsageAndExit();
            }

            var ipv4AddressOne = IPv4Address.MaybeParse(args[1]);
            if (ipv4AddressOne.HasValue)
            {
                var ipv4AddressTwo = IPv4Address.MaybeParse(args[2]);
                if (!ipv4AddressTwo.HasValue)
                {
                    Console.Error.WriteLine("Parsed IPv4 address {0} but failed to parse {1}.", ipv4AddressOne, args[2]);
                    return 1;
                }

                List<IPNetwork<IPv4Address>> subnets = RangeToSubnets(ipv4AddressOne.Value, ipv4AddressTwo.Value,
                    (addr, cidr) => new IPv4Network(addr, cidr));

                foreach (IPNetwork<IPv4Address> subnet in subnets)
                {
                    Console.WriteLine("{0}/{1}", subnet.BaseAddress, subnet.CidrPrefix.Value);
                }

                return 0;
            }

            var ipv6AddressOne = IPv6Address.MaybeParse(args[1]);
            if (ipv6AddressOne.HasValue)
            {
                var ipv6AddressTwo = IPv6Address.MaybeParse(args[2]);
                if (!ipv6AddressTwo.HasValue)
                {
                    Console.Error.WriteLine("Parsed IPv6 address {0} but failed to parse {1}.", ipv6AddressOne, args[2]);
                    return 1;
                }

                List<IPNetwork<IPv6Address>> subnets = RangeToSubnets(ipv6AddressOne.Value, ipv6AddressTwo.Value,
                    (addr, cidr) => new IPv6Network(addr, cidr));

                foreach (IPNetwork<IPv6Address> subnet in subnets)
                {
                    Console.WriteLine("{0}/{1}", subnet.BaseAddress, subnet.CidrPrefix.Value);
                }

                return 0;
            }

            Console.Error.WriteLine("Failed to parse address {0}.", args[1]);
            return 1;
        }

        public static List<IPNetwork<TAddress>> RangeToSubnets<TAddress>(TAddress endOne, TAddress endTwo,
            Func<TAddress, int, IPNetwork<TAddress>> createCidr)
            where TAddress : struct, IIPAddress<TAddress>
        {
            var ret = new List<IPNetwork<TAddress>>();

            TAddress firstAddress = CalcIPUtils.MinAny(endOne, endTwo);
            TAddress lastAddress = CalcIPUtils.MaxAny(endOne, endTwo);

            IPNetwork<TAddress> currentSubnet = createCidr(firstAddress, lastAddress.Bytes.Length * 8);
            while (firstAddress.CompareTo(lastAddress) <= 0)
            {
                // try enlarging the subnet
                IPNetwork<TAddress> largerSubnet = createCidr(firstAddress, currentSubnet.CidrPrefix.Value - 1);
                if (!largerSubnet.BaseAddress.Equals(firstAddress) ||
                    largerSubnet.LastAddressOfSubnet.CompareTo(lastAddress) > 0)
                {
                    // we've gone beyond; store what we have and continue with the next chunk
                    ret.Add(currentSubnet);
                    firstAddress = currentSubnet.LastAddressOfSubnet.Add(1);
                    currentSubnet = createCidr(firstAddress, lastAddress.Bytes.Length * 8);
                }
                else
                {
                    // anchor the growth and continue
                    currentSubnet = largerSubnet;
                }
            }

            return ret;
        }
    }
}
