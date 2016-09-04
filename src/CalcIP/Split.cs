using System;
using System.Collections.Generic;
using System.Globalization;
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

                BigInteger[] splits = ParseHostCountSpecs(args);
                List<IPNetwork<IPv4Address>> subnets = SplitSubnet(addressAndNet.Item2, splits);
                foreach (IPNetwork<IPv4Address> subnet in subnets)
                {
                    Console.WriteLine("{0}/{1}", subnet.BaseAddress, subnet.CidrPrefix);
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

                BigInteger[] splits = ParseHostCountSpecs(args);
                List<IPNetwork<IPv6Address>> subnets = SplitSubnet(addressAndNet.Item2, splits);
                foreach (IPNetwork<IPv6Address> subnet in subnets)
                {
                    Console.WriteLine("{0}/{1}", subnet.BaseAddress, subnet.CidrPrefix);
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

        public static List<IPNetwork<TAddress>> SplitSubnet<TAddress>(IPNetwork<TAddress> subnet, BigInteger[] hostCounts)
            where TAddress : struct, IIPAddress<TAddress>
        {
            throw new NotImplementedException();
        }
    }
}
