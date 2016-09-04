using System;

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
                throw new NotImplementedException();
            }

            var ipv6CidrMatch = Program.IPv6WithCidrRegex.Match(args[1]);
            var ipv6SubnetMatch = Program.IPv6WithCidrRegex.Match(args[1]);
            if (ipv6CidrMatch.Success || ipv6SubnetMatch.Success)
            {
                throw new NotImplementedException();
            }

            Console.Error.WriteLine("Could not detect network spec type of {0}.", args[1]);
            return 1;
        }
    }
}
