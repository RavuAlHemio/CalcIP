using System.Collections.Generic;
using CalcIP;
using Xunit;

namespace CalcIPTests
{
    public class IPNetworkTests
    {
        [Fact]
        public void TestSubnetMaskConstruction()
        {
            var addr = new IPv4Address(0x12345678u);
            var net = new IPv4Network(addr, 11);

            Assert.Equal(0x12200000u, net.BaseAddress.AddressValue);
            Assert.Equal(0xFFE00000u, net.SubnetMask.AddressValue);
            Assert.Equal(0x001FFFFFu, net.CiscoWildcard.AddressValue);
            Assert.Equal(0x12200001u, net.FirstHostAddress.Value.AddressValue);
            Assert.Equal(0x123FFFFEu, net.LastHostAddress.Value.AddressValue);
            Assert.Equal(0x123FFFFFu, net.BroadcastAddress.Value.AddressValue);
            Assert.Equal(11, net.CidrPrefix.Value);
        }

        [Fact]
        public void TestCidrPrefixConstruction()
        {
            var addr = new IPv4Address(0x12345678u);
            var mask = new IPv4Address(0xFFE00000u);
            var net = new IPv4Network(addr, mask);

            Assert.Equal(0x12200000u, net.BaseAddress.AddressValue);
            Assert.Equal(0xFFE00000u, net.SubnetMask.AddressValue);
            Assert.Equal(0x001FFFFFu, net.CiscoWildcard.AddressValue);
            Assert.Equal(0x12200001u, net.FirstHostAddress.Value.AddressValue);
            Assert.Equal(0x123FFFFEu, net.LastHostAddress.Value.AddressValue);
            Assert.Equal(0x123FFFFFu, net.BroadcastAddress.Value.AddressValue);
            Assert.Equal(11, net.CidrPrefix.Value);
        }

        [Fact]
        public void TestNonCidrPrefixConstruction()
        {
            var addr = new IPv4Address(0x12345678u);
            var mask = new IPv4Address(0xFF0000FFu);
            var net = new IPv4Network(addr, mask);

            Assert.Equal(0x12000078u, net.BaseAddress.AddressValue);
            Assert.Equal(0xFF0000FFu, net.SubnetMask.AddressValue);
            Assert.Equal(0x00FFFF00u, net.CiscoWildcard.AddressValue);
            Assert.Equal(0x12000178u, net.FirstHostAddress.Value.AddressValue);
            Assert.Equal(0x12FFFE78u, net.LastHostAddress.Value.AddressValue);
            Assert.Equal(0x12FFFF78u, net.BroadcastAddress.Value.AddressValue);
            Assert.Null(net.CidrPrefix);
        }

        [Fact]
        public void TestCaseWhereTheNetworkWasSplitUpIntoSubnetsUnterTheInfluence()
        {
            var addr = new IPv4Address(0xDDA69010);
            var mask = new IPv4Address(0x55E0951D);
            var net = new IPv4Network(addr, mask);

            // addr is 1101_1101_1010_0110_1001_0000_0001_0000 = 0xDDA69010
            // mask is 0101_0101_1110_0000_1001_0101_0001_1101 = 0x55E0951D
            // ------------------------------------------------------------
            // cwcd is 1010_1010_0001_1111_0110_1010_1110_0010 = 0xAA1F6AE2
            // base is 0101_0101_1010_0000_1001_0000_0001_0000 = 0x55A09010
            // fhst is 0101_0101_1010_0000_1001_0000_0001_0010 = 0x55A09012
            // bcst is 1111_1111_1011_1111_1111_1010_1111_0010 = 0xFFBFFAF2
            // lhst is 1111_1111_1011_1111_1111_1010_1111_0000 = 0xFFBFFAF0

            // unraveled base is net 1111_1011_1001_000 host 0_0000_0000_0000_0000
            // unraveled base is 0xFB900000
            // unraveled first host is 0xFB900001
            // unraveled first host is net 1111_1011_1001_000 host 0_0000_0000_0000_0001
            // woven first host is 0101_0101_1010_0000_1001_0000_0001_0010
            // woven first host is 0x55A09012

            Assert.Equal(0x55A09010u, net.BaseAddress.AddressValue);
            Assert.Equal(0x55E0951Du, net.SubnetMask.AddressValue);
            Assert.Equal(0xAA1F6AE2u, net.CiscoWildcard.AddressValue);
            Assert.Equal(0x55A09012u, net.FirstHostAddress.Value.AddressValue);
            Assert.Equal(0xFFBFFAF0u, net.LastHostAddress.Value.AddressValue);
            Assert.Equal(0xFFBFFAF2u, net.BroadcastAddress.Value.AddressValue);
            Assert.Null(net.CidrPrefix);
        }

        [Fact]
        public void TestMinimizeTunet()
        {
            var netsMust = new IPv4Network[]
            {
                new IPv4Network(IPv4Address.Parse("128.130.0.0"), 15),
                new IPv4Network(IPv4Address.Parse("192.35.240.0"), 22),
                new IPv4Network(IPv4Address.Parse("192.35.244.0"), 24),
                new IPv4Network(IPv4Address.Parse("193.170.72.0"), 21),
                new IPv4Network(IPv4Address.Parse("193.170.72.0"), 22),
                new IPv4Network(IPv4Address.Parse("193.170.76.0"), 23),
                new IPv4Network(IPv4Address.Parse("193.170.78.0"), 24),
                new IPv4Network(IPv4Address.Parse("193.170.79.0"), 24),
            };

            List<IPNetwork<IPv4Address>> netsMinimized = Minimize.MinimizeSubnets(netsMust,
                (addr, mask) => new IPv4Network(addr, mask));

            foreach (IPNetwork<IPv4Address> minNet in netsMinimized)
            {
                for (IPv4Address addr = minNet.BaseAddress; addr.CompareTo(minNet.LastAddressOfSubnet) <= 0; addr = addr + 1)
                {
                    bool contained = false;
                    foreach (IPNetwork<IPv4Address> origNet in netsMust)
                    {
                        if (origNet.Contains(addr))
                        {
                            contained = true;
                            break;
                        }
                    }

                    if (!contained)
                    {
                        Assert.True(false, $"IP address {addr} in minimized net {minNet} not contained in any original net");
                    }
                }
            }

            foreach (IPNetwork<IPv4Address> origNet in netsMust)
            {
                for (IPv4Address addr = origNet.BaseAddress; addr.CompareTo(origNet.LastAddressOfSubnet) <= 0; addr = addr + 1)
                {
                    bool contained = false;
                    foreach (IPNetwork<IPv4Address> minNet in netsMinimized)
                    {
                        if (minNet.Contains(addr))
                        {
                            contained = true;
                            break;
                        }
                    }

                    if (!contained)
                    {
                        Assert.True(false, $"IP address {addr} in original net {origNet} not contained in any minimized net");
                    }
                }
            }
        }
    }
}
