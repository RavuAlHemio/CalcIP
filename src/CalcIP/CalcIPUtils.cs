using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CalcIP
{
    public static class CalcIPUtils
    {
        public static ImmutableDictionary<byte, int> CidrBytes { get; }
        public static ImmutableDictionary<byte, int> BytePopCount { get; }

        static CalcIPUtils()
        {
            var cidrBuilder = ImmutableDictionary.CreateBuilder<byte, int>();
            cidrBuilder.Add(0x80, 1);
            cidrBuilder.Add(0xC0, 2);
            cidrBuilder.Add(0xE0, 3);
            cidrBuilder.Add(0xF0, 4);
            cidrBuilder.Add(0xF8, 5);
            cidrBuilder.Add(0xFC, 6);
            cidrBuilder.Add(0xFE, 7);
            CidrBytes = cidrBuilder.ToImmutable();

            var popCountBuilder = ImmutableDictionary.CreateBuilder<byte, int>();
            for (int b = 0x00; b < 0x100; ++b)
            {
                byte popCount = 0;
                for (int bit = 0; bit < 8; ++bit)
                {
                    if ((b & (1 << bit)) != 0)
                    {
                        ++popCount;
                    }
                }
                popCountBuilder.Add((byte)b, popCount);
            }
            BytePopCount = popCountBuilder.ToImmutable();
        }

        public static byte[] SubnetMaskBytesFromCidrPrefix(int byteCount, int cidrPrefix)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount,
                    nameof(byteCount) + " must be at least 0");
            }

            if (cidrPrefix < 0 || cidrPrefix > 8*byteCount)
            {
                throw new ArgumentOutOfRangeException(nameof(cidrPrefix), cidrPrefix,
                    nameof(cidrPrefix) + " must be at least 0 and at most " + (8*byteCount));
            }

            byte[] subnetMaskBytes = new byte[byteCount];
            for (int i = 0; i < subnetMaskBytes.Length; ++i)
            {
                switch (cidrPrefix)
                {
                    case 0:
                        subnetMaskBytes[i] = 0x00;
                        break;
                    case 1:
                        subnetMaskBytes[i] = 0x80;
                        break;
                    case 2:
                        subnetMaskBytes[i] = 0xC0;
                        break;
                    case 3:
                        subnetMaskBytes[i] = 0xE0;
                        break;
                    case 4:
                        subnetMaskBytes[i] = 0xF0;
                        break;
                    case 5:
                        subnetMaskBytes[i] = 0xF8;
                        break;
                    case 6:
                        subnetMaskBytes[i] = 0xFC;
                        break;
                    case 7:
                        subnetMaskBytes[i] = 0xFE;
                        break;
                    default:
                        subnetMaskBytes[i] = 0xFF;
                        break;
                }

                if (cidrPrefix < 9)
                {
                    break;
                }
                else
                {
                    cidrPrefix -= 8;
                }
            }

            return subnetMaskBytes;
        }

        public static int? CidrPrefixFromSubnetMaskBytes(byte[] subnetMaskBytes)
        {
            bool nonFullByteSeen = false;
            int prefixLength = 0;

            foreach (byte b in subnetMaskBytes)
            {
                if (b == 0xFF)
                {
                    if (nonFullByteSeen)
                    {
                        // something like FF 00 FF FF or FF 80 FF FF
                        return null;
                    }

                    // OK
                    prefixLength += 8;
                }
                else if (b == 0x00)
                {
                    // disallow FF 00 FF FF but allow FF 00 00 00
                    nonFullByteSeen = true;
                }
                else
                {
                    if (nonFullByteSeen)
                    {
                        // disalllow FF F0 FF FF
                        return null;
                    }

                    // is this a CIDR-y byte?
                    int byteLength;
                    if (!CidrBytes.TryGetValue(b, out byteLength))
                    {
                        // nope
                        // disallow FF FF 0C
                        return null;
                    }

                    // disallow FF F0 FF FF
                    nonFullByteSeen = true;

                    // OK otherwise
                    prefixLength += byteLength;
                }
            }

            return prefixLength;
        }

        public static string ByteToBinary(byte b)
        {
            var ret = new char[8];
            for (int i = 0; i < ret.Length; ++i)
            {
                ret[i] = ((b & (1 << (ret.Length-i-1))) != 0)
                    ? '1'
                    : '0';
            }
            return new string(ret);
        }

        public static string UInt16ToBinary(ushort b)
        {
            var ret = new char[16];
            for (int i = 0; i < ret.Length; ++i)
            {
                ret[i] = ((b & (1 << (ret.Length-i-1))) != 0)
                    ? '1'
                    : '0';
            }
            return new string(ret);
        }

        public static string PadRightTo(string s, int count, char padChar = ' ')
        {
            if (s.Length >= count)
            {
                return s;
            }

            int diff = count - s.Length;
            var padding = new string(padChar, diff);
            return s + padding;
        }

        public static T MaxAny<T>(T one, T other)
            where T : IComparable<T>
        {
            return (one.CompareTo(other) >= 0) ? one : other;
        }

        public static T MinAny<T>(T one, T other)
            where T : IComparable<T>
        {
            return (one.CompareTo(other) <= 0) ? one : other;
        }

        public static TAddress UnravelAddress<TAddress>(TAddress address, TAddress subnetMask)
            where TAddress : struct, IIPAddress<TAddress>
        {
            if (CalcIPUtils.CidrPrefixFromSubnetMaskBytes(subnetMask.Bytes).HasValue)
            {
                // nothing to unravel :)
                return address;
            }

            // given an address ABCDEFGH with subnet mask 11001001, turn it into ABEHCDFG (i.e. with subnet mask 11110000)
            byte[] addressBytes = address.Bytes;
            byte[] maskBytes = subnetMask.Bytes;

            var netBits = new List<bool>(addressBytes.Length);
            var hostBits = new List<bool>(addressBytes.Length);

            // find the bits
            for (int i = 0; i < addressBytes.Length; ++i)
            {
                for (int bit = 7; bit >= 0; --bit)
                {
                    bool addressBit = ((addressBytes[i] & (1 << bit)) != 0);
                    bool isNet = ((maskBytes[i] & (1 << bit)) != 0);

                    if (isNet)
                    {
                        netBits.Add(addressBit);
                    }
                    else
                    {
                        hostBits.Add(addressBit);
                    }
                }
            }

            var unraveledBits = new List<bool>(netBits.Count + hostBits.Count);
            unraveledBits.AddRange(netBits);
            unraveledBits.AddRange(hostBits);

            var retBytes = new byte[addressBytes.Length];
            for (int i = 0; i < retBytes.Length; ++i)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; ++bit)
                {
                    if (unraveledBits[8*i+bit])
                    {
                        b |= (byte)(1 << (7-bit));
                    }
                }
                retBytes[i] = b;
            }

            return address.MaybeFromBytes(retBytes).Value;
        }

        public static TAddress WeaveAddress<TAddress>(TAddress address, TAddress subnetMask)
            where TAddress : struct, IIPAddress<TAddress>
        {
            if (CalcIPUtils.CidrPrefixFromSubnetMaskBytes(subnetMask.Bytes).HasValue)
            {
                // nothing to weave :)
                return address;
            }

            // given an address ABCDEFGH with subnet mask 11001001, convert from subnet mask 11110000 turning it into ABEFCGHD

            byte[] addressBytes = address.Bytes;
            byte[] maskBytes = subnetMask.Bytes;
            int cidrPrefix = subnetMask.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);

            var netBits = new List<bool>(addressBytes.Length);
            var hostBits = new List<bool>(addressBytes.Length);
            var maskBits = new List<bool>(maskBytes.Length);

            // find the bits
            for (int i = 0; i < addressBytes.Length; ++i)
            {
                for (int bit = 0; bit < 8; ++bit)
                {
                    int totalBitIndex = 8*i + bit;
                    bool addressBit = ((addressBytes[i] & (1 << (7-bit))) != 0);
                    bool isNet = (totalBitIndex < cidrPrefix);

                    if (isNet)
                    {
                        netBits.Add(addressBit);
                    }
                    else
                    {
                        hostBits.Add(addressBit);
                    }

                    bool maskBit = ((maskBytes[i] & (1 << (7-bit))) != 0);
                    maskBits.Add(maskBit);
                }
            }

            var retBytes = new byte[addressBytes.Length];
            int netIndex = 0;
            int hostIndex = 0;
            for (int i = 0; i < retBytes.Length; ++i)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; ++bit)
                {
                    bool shouldSetBit;
                    if (maskBits[8*i+bit])
                    {
                        shouldSetBit = netBits[netIndex];
                        ++netIndex;
                    }
                    else
                    {
                        shouldSetBit = hostBits[hostIndex];
                        ++hostIndex;
                    }

                    if (shouldSetBit)
                    {
                        b |= (byte)(1 << (7-bit));
                    }
                }
                retBytes[i] = b;
            }

            return address.MaybeFromBytes(retBytes).Value;
        }
    }
}
