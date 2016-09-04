using System;
using CalcIP;
using Xunit;

namespace CalcIPTests
{
    public class IPv6AddressTests
    {
        [Fact]
        public void TestToString() 
        {
            Assert.Equal("::", new IPv6Address(0x0, 0x0).ToString());
            Assert.Equal("::1", new IPv6Address(0x0, 0x1).ToString());
            Assert.Equal("::123:4567", new IPv6Address(0x0000000000000000, 0x0000000001234567).ToString());
            Assert.Equal("12:34::", new IPv6Address(0x0012003400000000, 0x0000000000000000).ToString());
            Assert.Equal("abcd:123::256", new IPv6Address(0xABCD012300000000, 0x0000000000000256).ToString());
            Assert.Equal("abcd::123:256", new IPv6Address(0xABCD000000000000, 0x0000000001230256).ToString());
            Assert.Equal("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", new IPv6Address(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF).ToString());
            Assert.Equal("fec0:abcd:1234:defa:1337:8008:1224:2323", new IPv6Address(0xFEC0ABCD1234DEFA, 0x1337800812242323).ToString());
        }

        [Fact]
        public void TestParse()
        {
            Action<ulong, ulong, string> testParse = (topHalf, bottomHalf, input) =>
            {
                var addr = IPv6Address.MaybeParse(input).Value;
                Assert.Equal(topHalf, addr.TopHalf);
                Assert.Equal(bottomHalf, addr.BottomHalf);
            };

            testParse(0x0000000000000000ul, 0x0000000000000000ul, "0000:0000:0000:0000:0000:0000:0000:0000");
            testParse(0x0000000000000000ul, 0x0000000000000000ul, "0:0:0:0:0:0:0:0");
            testParse(0x0000000000000000ul, 0x0000000000000000ul, "::");
            testParse(0x0000000000000000ul, 0x0000000000000000ul, "0:00:000:0000:000:0:0000:00");
            testParse(0x0000000000000000ul, 0x0000000000000000ul, "0:00:000::0:0000:00");

            testParse(0x0000000000000000ul, 0x0000000000000001ul, "0000:0000:0000:0000:0000:0000:0000:0001");
            testParse(0x0000000000000000ul, 0x0000000000000001ul, "0:0:0:0:0:0:0:1");
            testParse(0x0000000000000000ul, 0x0000000000000001ul, "::1");
            testParse(0x0000000000000000ul, 0x0000000000000001ul, "0:00:000:0000:000:0:0000:01");
            testParse(0x0000000000000000ul, 0x0000000000000001ul, "0:00:000::0:0000:01");

            testParse(0xFE80000000000000ul, 0xA55E55ED0B501E7Eul, "fe80:0000:0000:0000:a55e:55ed:0b50:1e7e");
            testParse(0xFE80000000000000ul, 0xA55E55ED0B501E7Eul, "fe80:0:0:0:a55e:55ed:b50:1e7e");
            testParse(0xFE80000000000000ul, 0xA55E55ED0B501E7Eul, "fe80::a55e:55ed:0b50:1e7e");
            testParse(0xFE80000000000000ul, 0xA55E55ED0B501E7Eul, "fe80::a55e:55ed:b50:1e7e");

            Assert.Null(IPv6Address.MaybeParse(":"));
            Assert.Null(IPv6Address.MaybeParse("a:"));
            Assert.Null(IPv6Address.MaybeParse(":a"));
            Assert.Null(IPv6Address.MaybeParse(":::"));
            Assert.Null(IPv6Address.MaybeParse("fe80::a55e:55ed::0bso:1e7e"));
            Assert.Null(IPv6Address.MaybeParse("fe80::a55e:55ed:0bso:1ete"));
        }

        [Fact]
        public void TestBytes()
        {
            Action<byte[], ulong, ulong> testToBytes = (input, topHalf, bottomHalf) =>
            {
                var addr = new IPv6Address(topHalf, bottomHalf);
                Assert.Equal(input, addr.Bytes);
            };

            testToBytes(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}, 0x0000000000000000ul, 0x0000000000000000ul);
            testToBytes(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01}, 0x0000000000000000ul, 0x0000000000000001ul);
            testToBytes(new byte[] {0xFE, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA5, 0x5E, 0x55, 0xED, 0x0B, 0x50, 0x1E, 0x7E}, 0xFE80000000000000ul, 0xA55E55ED0B501E7Eul);
            testToBytes(new byte[] {0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10}, 0x123456789ABCDEF0ul, 0xFEDCBA9876543210ul);
        }

        [Fact]
        public void TestFromBytes()
        {
            Action<ulong, ulong, byte[]> testFromBytes = (topHalf, bottomHalf, input) =>
            {
                var addr = IPv6Address.MaybeFromBytes(input).Value;
                Assert.Equal(topHalf, addr.TopHalf);
                Assert.Equal(bottomHalf, addr.BottomHalf);
            };

            testFromBytes(0x0000000000000000ul, 0x0000000000000000ul, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00});
            testFromBytes(0x0000000000000000ul, 0x0000000000000001ul, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01});
            testFromBytes(0xFE80000000000000ul, 0xA55E55ED0B501E7Eul, new byte[] {0xFE, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA5, 0x5E, 0x55, 0xED, 0x0B, 0x50, 0x1E, 0x7E});
            testFromBytes(0x123456789ABCDEF0ul, 0xFEDCBA9876543210ul, new byte[] {0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10});

            Assert.Null(IPv6Address.MaybeFromBytes(new byte[] {0xFE, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA5, 0x5E, 0x55, 0xED, 0x0B, 0x50, 0x1E}));
            Assert.Null(IPv6Address.MaybeFromBytes(new byte[] {0xFE, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA5, 0x5E, 0x55, 0xED, 0x0B, 0x50, 0x1E, 0x7E, 0x99}));
        }

        [Theory]
        [InlineData(0x0000000000000000ul, 0x0000000000000000ul)]
        [InlineData(0x0000000000000000ul, 0x0000000000000001ul)]
        [InlineData(0xFE80000000000000ul, 0xA55E55ED0B501E7Eul)]
        [InlineData(0x123456789ABCDEF0ul, 0xFEDCBA9876543210ul)]
        public void TestEquality(ulong topHalf, ulong bottomHalf)
        {
            var left = new IPv6Address(topHalf, bottomHalf);
            var right = new IPv6Address(topHalf, bottomHalf);
            Assert.Equal(left, right);
        }

        [Theory]
        [InlineData(0x1214121812141210ul, 0x1214121812141210ul, 0x123456789ABCDEF0ul, 0xFEDCBA9876543210ul, 0xFEDCBA9876543210ul, 0x123456789ABCDEF0ul)]
        public void TestAnd(ulong expectedTop, ulong expectedBottom, ulong leftTop, ulong leftBottom, ulong rightTop, ulong rightBottom)
        {
            var expectedAddress = new IPv6Address(expectedTop, expectedBottom);
            var leftAddress = new IPv6Address(leftTop, leftBottom);
            var rightAddress = new IPv6Address(rightTop, rightBottom);
            Assert.Equal(expectedAddress, leftAddress & rightAddress);
        }
    }
}
