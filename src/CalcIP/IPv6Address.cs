using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalcIP
{
    public struct IPv6Address : IIPAddress<IPv6Address>, IComparable<IPv6Address>, IEquatable<IPv6Address>
    {
        public readonly ulong TopHalf;
        public readonly ulong BottomHalf;

        public IPv6Address(ulong topHalf, ulong bottomHalf)
        {
            TopHalf = topHalf;
            BottomHalf = bottomHalf;
        }

        // presets
        public static readonly IPv6Address Zero = new IPv6Address(0, 0);

        // derivation functions
        public IPv6Address SubnetMaskFromCidrPrefix(int cidrPrefix)
        {
            return IPv6Address.MaybeFromBytes(CalcIPUtils.SubnetMaskBytesFromCidrPrefix(16, cidrPrefix)).Value;
        }

        // string operations
        public static IPv6Address? MaybeParse(string addressString)
        {
            if (addressString.StartsWith("::"))
            {
                addressString = "0" + addressString;
            }

            if (addressString.EndsWith("::"))
            {
                addressString = addressString + "0";
            }

            string[] chunks = addressString.Split(':');
            if (chunks.Length > 8)
            {
                return null;
            }

            // how many shortening elements do we have?
            int shorteningCount = chunks.Count(ch => ch.Length == 0);
            if (shorteningCount > 1)
            {
                // "1234::5678::9abc" is invalid
                return null;
            }

            string[] actualChunks;
            if (shorteningCount == 0)
            {
                // full address "123:45:678:9:ab:cd:ef:21"
                if (chunks.Length != 8)
                {
                    // too few chunks
                    return null;
                }

                actualChunks = chunks;
            }
            else
            {
                // shortened address "123::456a"
                actualChunks = new string[8];

                // copy from front
                for (int i = 0; i < chunks.Length; ++i)
                {
                    if (chunks[i].Length == 0)
                    {
                        break;
                    }
                    actualChunks[i] = chunks[i];
                }

                // copy from back
                for (int i = 0; i < chunks.Length; ++i)
                {
                    if (chunks[chunks.Length - i - 1].Length == 0)
                    {
                        break;
                    }

                    actualChunks[actualChunks.Length - i - 1] = chunks[chunks.Length - i - 1];
                }

                // fill missing chunks
                for (int i = 0; i < actualChunks.Length; ++i)
                {
                    if (actualChunks[i] == null)
                    {
                        actualChunks[i] = "0";
                    }
                }
            }

            ulong topHalf = 0;
            ulong bottomHalf = 0;
            for (int i = 0; i < 8; ++i)
            {
                int shiftCount = 112 - (i*16);
                int shiftCountWithinHalf = shiftCount % 64;
                bool intoTopHalf = (i < 4);

                ushort chunkValue;
                if (!ushort.TryParse(actualChunks[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out chunkValue))
                {
                    // parsing failed
                    return null;
                }

                if (intoTopHalf)
                {
                    topHalf |= ((ulong)chunkValue << shiftCountWithinHalf);
                }
                else
                {
                    bottomHalf |= ((ulong)chunkValue << shiftCountWithinHalf);
                }
            }

            return new IPv6Address(topHalf, bottomHalf);
        }

        public static bool TryParse(string addressString, out IPv6Address address)
        {
            IPv6Address? ret = MaybeParse(addressString);
            if (ret.HasValue)
            {
                address = ret.Value;
                return true;
            }
            else
            {
                address = Zero;
                return false;
            }
        }

        public static IPv6Address Parse(string addressString)
        {
            IPv6Address? ret = MaybeParse(addressString);
            if (ret.HasValue)
            {
                return ret.Value;
            }
            throw new FormatException("Invalid IPv6 address.");
        }

        public override string ToString()
        {
            if (TopHalf == 0 && BottomHalf == 0)
            {
                return "::";
            }

            ushort[] chunks = Chunks;

            // attempt to shorten
            int i = 0;
            int zeroIndex = -1;
            int zeroLength = 0;
            while (i < 8)
            {
                if (chunks[i] != 0)
                {
                    ++i;
                    continue;
                }

                // zero chunk!
                int j;
                for (j = i + 1; j < 8; ++j)
                {
                    if (chunks[j] != 0)
                    {
                        break;
                    }
                }

                if (j - i > zeroLength)
                {
                    // new longest zero chunk found!
                    zeroIndex = i;
                    zeroLength = j - i;
                }

                // continue at j
                i = j;
            }

            var chunkStrings = new List<string>();
            for (i = 0; i < 8; ++i)
            {
                if (i == zeroIndex)
                {
                    if (i == 0)
                    {
                        // the initial part of the address is zero
                        chunkStrings.Add("");
                    }

                    // an empty chunk causes two adjacent colons
                    chunkStrings.Add("");

                    // jump past the length (don't forget we increase i at the top)
                    i += zeroLength - 1;

                    if (i == 7)
                    {
                        // the final part of the address is zero
                        chunkStrings.Add("");
                    }

                    continue;
                }

                chunkStrings.Add(chunks[i].ToString("x", CultureInfo.InvariantCulture));
            }

            return string.Join(":", chunkStrings);
        }

        public string ToFullString()
        {
            var chunks = Chunks;
            var chunkStrings = new string[chunks.Length];
            for (int i = 0; i < chunkStrings.Length; ++i)
            {
                chunkStrings[i] = chunks[i].ToString("x4", CultureInfo.InvariantCulture);
            }
            return string.Join(":", chunkStrings);
        }

        // byte operations
        public byte[] Bytes => new byte[] {
            (byte)((TopHalf >> 56) & 0xFF),
            (byte)((TopHalf >> 48) & 0xFF),
            (byte)((TopHalf >> 40) & 0xFF),
            (byte)((TopHalf >> 32) & 0xFF),
            (byte)((TopHalf >> 24) & 0xFF),
            (byte)((TopHalf >> 16) & 0xFF),
            (byte)((TopHalf >>  8) & 0xFF),
            (byte)((TopHalf >>  0) & 0xFF),
            (byte)((BottomHalf >> 56) & 0xFF),
            (byte)((BottomHalf >> 48) & 0xFF),
            (byte)((BottomHalf >> 40) & 0xFF),
            (byte)((BottomHalf >> 32) & 0xFF),
            (byte)((BottomHalf >> 24) & 0xFF),
            (byte)((BottomHalf >> 16) & 0xFF),
            (byte)((BottomHalf >>  8) & 0xFF),
            (byte)((BottomHalf >>  0) & 0xFF)
        };

        public static IPv6Address? MaybeFromBytes(byte[] bytes)
        {
            if (bytes.Length != 16)
            {
                return null;
            }

            ulong topHalf =
                ((ulong)bytes[0] << 56) |
                ((ulong)bytes[1] << 48) |
                ((ulong)bytes[2] << 40) |
                ((ulong)bytes[3] << 32) |
                ((ulong)bytes[4] << 24) |
                ((ulong)bytes[5] << 16) |
                ((ulong)bytes[6] <<  8) |
                ((ulong)bytes[7] <<  0)
            ;

            ulong bottomHalf =
                ((ulong)bytes[8] << 56) |
                ((ulong)bytes[9] << 48) |
                ((ulong)bytes[10] << 40) |
                ((ulong)bytes[11] << 32) |
                ((ulong)bytes[12] << 24) |
                ((ulong)bytes[13] << 16) |
                ((ulong)bytes[14] <<  8) |
                ((ulong)bytes[15] <<  0)
            ;

            return new IPv6Address(topHalf, bottomHalf);
        }

        IPv6Address? IIPAddress<IPv6Address>.MaybeFromBytes(byte[] bytes)
        {
            return MaybeFromBytes(bytes);
        }

        // chunk operations
        public ushort[] Chunks => new ushort[] {
            (ushort)((TopHalf >> 48) & 0xFFFF),
            (ushort)((TopHalf >> 32) & 0xFFFF),
            (ushort)((TopHalf >> 16) & 0xFFFF),
            (ushort)((TopHalf >>  0) & 0xFFFF),
            (ushort)((BottomHalf >> 48) & 0xFFFF),
            (ushort)((BottomHalf >> 32) & 0xFFFF),
            (ushort)((BottomHalf >> 16) & 0xFFFF),
            (ushort)((BottomHalf >>  0) & 0xFFFF)
        };

        public IPv6Address? MaybeFromChunks(ushort[] chunks)
        {
            if (chunks.Length != 8)
            {
                return null;
            }

            ulong topHalf =
                ((ulong)chunks[0] << 48) |
                ((ulong)chunks[1] << 32) |
                ((ulong)chunks[2] << 16) |
                ((ulong)chunks[3] <<  0)
            ;

            ulong bottomHalf =
                ((ulong)chunks[4] << 48) |
                ((ulong)chunks[5] << 32) |
                ((ulong)chunks[6] << 16) |
                ((ulong)chunks[7] <<  0)
            ;

            return new IPv6Address(topHalf, bottomHalf);
        }

        // arithmetic operators
        public static IPv6Address operator&(IPv6Address left, IPv6Address right)
        {
            return new IPv6Address(left.TopHalf & right.TopHalf, left.BottomHalf & right.BottomHalf);
        }

        public IPv6Address BitwiseAnd(IPv6Address other)
        {
            return this & other;
        }

        public static IPv6Address operator|(IPv6Address left, IPv6Address right)
        {
            return new IPv6Address(left.TopHalf | right.TopHalf, left.BottomHalf | right.BottomHalf);
        }

        public static IPv6Address operator^(IPv6Address left, IPv6Address right)
        {
            return new IPv6Address(left.TopHalf ^ right.TopHalf, left.BottomHalf ^ right.BottomHalf);
        }

        public static IPv6Address operator~(IPv6Address operand)
        {
            return new IPv6Address(~operand.TopHalf, ~operand.BottomHalf);
        }

        public IPv6Address BitwiseNot()
        {
            return ~this;
        }

        public static IPv6Address operator+(IPv6Address left, IPv6Address right)
        {
            ulong newTop, newBottom;
            AddWithCarry(left.TopHalf, left.BottomHalf, right.TopHalf, right.BottomHalf, out newTop, out newBottom);
            return new IPv6Address(newTop, newBottom);
        }

        public IPv6Address Add(IPv6Address other)
        {
            return this + other;
        }

        public static IPv6Address operator+(IPv6Address baseAddress, int offset)
        {
            ulong newTop, newBottom;
            if (offset < 0)
            {
                SubtractWithBorrow(baseAddress.TopHalf, baseAddress.BottomHalf, 0, (ulong)(-offset), out newTop, out newBottom);
            }
            else
            {
                AddWithCarry(baseAddress.TopHalf, baseAddress.BottomHalf, 0, (ulong)offset, out newTop, out newBottom);
            }
            return new IPv6Address(newTop, newBottom);
        }

        public IPv6Address Add(int offset)
        {
            return this + offset;
        }

        public static IPv6Address operator-(IPv6Address left, IPv6Address right)
        {
            ulong newTop, newBottom;
            SubtractWithBorrow(left.TopHalf, left.BottomHalf, right.TopHalf, right.BottomHalf, out newTop, out newBottom);
            return new IPv6Address(newTop, newBottom);
        }

        public IPv6Address Subtract(IPv6Address other)
        {
            return this - other;
        }

        public static IPv6Address operator-(IPv6Address baseAddress, int offset)
        {
            return baseAddress + (-offset);
        }

        public IPv6Address Subtract(int offset)
        {
            return this - offset;
        }

        // equality operators and comparisons
        public override bool Equals(object other)
        {
            return other is IPv6Address && this == (IPv6Address)other;
        }

        public bool Equals(IPv6Address other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return TopHalf.GetHashCode() ^ BottomHalf.GetHashCode();
        }

        public static bool operator ==(IPv6Address left, IPv6Address right)
        {
            return left.TopHalf == right.TopHalf && left.BottomHalf == right.BottomHalf;
        }

        public static bool operator !=(IPv6Address left, IPv6Address right)
        {
            return !(left == right);
        }

        public int CompareTo(IPv6Address other)
        {
            int topComparison = this.TopHalf.CompareTo(other.TopHalf);
            if (topComparison != 0)
            {
                return topComparison;
            }

            return this.BottomHalf.CompareTo(other.BottomHalf);
        }

        internal static void AddWithCarry(ulong leftTop, ulong leftBottom, ulong rightTop, ulong rightBottom, out ulong sumTop, out ulong sumBottom)
        {
            sumBottom = unchecked(leftBottom + rightBottom);
            ulong carry = 0;
            if (sumBottom < leftBottom)
            {
                carry = 1;
            }

            sumTop = leftTop + rightTop + carry;
        }

        internal static void SubtractWithBorrow(ulong minuendTop, ulong minuendBottom, ulong subtrahendTop, ulong subtrahendBottom, out ulong differenceTop, out ulong differenceBottom)
        {
            differenceBottom = unchecked(minuendBottom - subtrahendBottom);
            ulong borrow = 0;
            if (differenceBottom > minuendBottom)
            {
                borrow = 1;
            }

            differenceTop = minuendTop - subtrahendTop - borrow;
        }
    }
}
