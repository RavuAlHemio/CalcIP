using System;
using System.Collections.Immutable;

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
    }
}
