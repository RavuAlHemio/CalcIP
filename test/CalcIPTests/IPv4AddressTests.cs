using System;
using CalcIP;
using Xunit;

namespace CalcIPTests
{
    public class IPv4AddressTests
    {
        [Fact]
        public void TestToString() 
        {
            Assert.Equal("0.0.0.0", new IPv4Address(0x00000000).ToString());
            Assert.Equal("255.255.255.255", new IPv4Address(0xFFFFFFFF).ToString());
            Assert.Equal("18.52.86.120", new IPv4Address(0x12345678).ToString());
            Assert.Equal("127.0.0.1", new IPv4Address(0x7F000001).ToString());
        }

        [Fact]
        public void TestParse()
        {
            Assert.Equal(0x00000000u, IPv4Address.MaybeParse("0.0.0.0").Value.AddressValue);
            Assert.Equal(0x00000000u, IPv4Address.MaybeParse("00.000.00000.0").Value.AddressValue);
            Assert.Equal(0x01020304u, IPv4Address.MaybeParse("1.2.3.4").Value.AddressValue);
            Assert.Equal(0x01020304u, IPv4Address.MaybeParse("01.002.00003.4").Value.AddressValue);
            Assert.Equal(0xFFFFFFFFu, IPv4Address.MaybeParse("255.255.255.255").Value.AddressValue);
            Assert.Equal(0x12345678u, IPv4Address.MaybeParse("18.52.86.120").Value.AddressValue);
            Assert.Equal(0x7F000001u, IPv4Address.MaybeParse("127.0.0.1").Value.AddressValue);

            Assert.Null(IPv4Address.MaybeParse("."));
            Assert.Null(IPv4Address.MaybeParse("1.2.3"));
            Assert.Null(IPv4Address.MaybeParse("1.2.3.4.5"));
            Assert.Null(IPv4Address.MaybeParse("1.2.-3.4"));
            Assert.Null(IPv4Address.MaybeParse("255.255.256.255"));
            Assert.Null(IPv4Address.MaybeParse("0xFF.255.256.255"));
        }

        [Fact]
        public void TestBytes()
        {
            Assert.Equal(new byte[] {0, 0, 0, 0}, new IPv4Address(0x00000000).Bytes);
            Assert.Equal(new byte[] {1, 2, 3, 4}, new IPv4Address(0x01020304).Bytes);
            Assert.Equal(new byte[] {255, 255, 255, 255}, new IPv4Address(0xFFFFFFFF).Bytes);
            Assert.Equal(new byte[] {18, 52, 86, 120}, new IPv4Address(0x12345678).Bytes);
            Assert.Equal(new byte[] {127, 0, 0, 1}, new IPv4Address(0x7F000001).Bytes);
        }

        [Fact]
        public void TestFromBytes()
        {
            Assert.Equal(0x00000000u, IPv4Address.MaybeFromBytes(new byte[] {0, 0, 0, 0}).Value.AddressValue);
            Assert.Equal(0x01020304u, IPv4Address.MaybeFromBytes(new byte[] {1, 2, 3, 4}).Value.AddressValue);
            Assert.Equal(0xFFFFFFFFu, IPv4Address.MaybeFromBytes(new byte[] {255, 255, 255, 255}).Value.AddressValue);
            Assert.Equal(0x12345678u, IPv4Address.MaybeFromBytes(new byte[] {18, 52, 86, 120}).Value.AddressValue);
            Assert.Equal(0x7F000001u, IPv4Address.MaybeFromBytes(new byte[] {127, 0, 0, 1}).Value.AddressValue);

            Assert.Null(IPv4Address.MaybeFromBytes(new byte[] {1, 2, 3}));
            Assert.Null(IPv4Address.MaybeFromBytes(new byte[] {1, 2, 3, 4, 5}));
        }

        [Theory]
        [InlineData(0x00000000)]
        [InlineData(0xFFFFFFFF)]
        [InlineData(0x7F000001)]
        [InlineData(0x7F1234AB)]
        public void TestEquality(uint addressValue)
        {
            var left = new IPv4Address(addressValue);
            var right = new IPv4Address(addressValue);
            Assert.Equal(left, right);
        }

        [Theory]
        [InlineData(0x7F000000, 0x7F000001, 0xFF000000)]
        [InlineData(0xC0A8A900, 0xC0A8A917, 0xFFFFFF00)]
        public void TestAnd(uint expected, uint left, uint right)
        {
            var expectedAddress = new IPv4Address(expected);
            var leftAddress = new IPv4Address(left);
            var rightAddress = new IPv4Address(right);
            Assert.Equal(expectedAddress, leftAddress & rightAddress);
        }
    }
}
