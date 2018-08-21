using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CalcIP
{
    public static class Enumerate
    {
        public static int PerformEnumerate(string[] args)
        {
            Program.PerformOnSubnets(
                args.Skip(1),
                (a, n) => OutputEnumeratedNetwork(n),
                (a, n) => OutputEnumeratedNetwork(n)
            );
            return 0;
        }

        public static IEnumerable<TAddress> EnumerateNetwork<TAddress>(IPNetwork<TAddress> network)
            where TAddress : struct, IIPAddress<TAddress>
        {
            BigInteger hostCount = network.HostCount;
            TAddress currentAddress = CalcIPUtils.UnravelAddress(network.BaseAddress, network.SubnetMask);

            for (BigInteger i = BigInteger.Zero; i < hostCount; ++i)
            {
                yield return CalcIPUtils.WeaveAddress(currentAddress, network.SubnetMask);
                currentAddress = currentAddress.Add(1);
            }
        }

        public static void OutputEnumeratedNetwork<TAddress>(IPNetwork<TAddress> network)
            where TAddress : struct, IIPAddress<TAddress>
        {
            foreach (TAddress address in EnumerateNetwork(network))
            {
                Console.WriteLine(address);
            }
        }
    }
}
