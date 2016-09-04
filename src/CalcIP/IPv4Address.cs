using System;
using System.Globalization;

namespace CalcIP
{
    public struct IPv4Address : IIPAddress<IPv4Address>
    {
        public readonly uint AddressValue;

        public IPv4Address(uint addressValue)
        {
            AddressValue = addressValue;
        }

        // presets
        public static readonly IPv4Address Zero = new IPv4Address(0);

        // derivation functions
        public IPv4Address SubnetMaskFromCidrPrefix(int cidrPrefix)
        {
            return IPv4Address.MaybeFromBytes(CalcIPUtils.SubnetMaskBytesFromCidrPrefix(4, cidrPrefix)).Value;
        }

        // string operations
        public static IPv4Address? MaybeParse(string addressString)
        {
            string[] chunks = addressString.Split('.');
            if (chunks.Length != 4)
            {
                return null;
            }

            uint addressValue = 0;
            for (int i = 0; i < 4; ++i)
            {
                int shiftCount = 24 - (i*8);
                int chunkValue;
                if (!int.TryParse(chunks[i], NumberStyles.None, CultureInfo.InvariantCulture, out chunkValue))
                {
                    return null;
                }

                if (chunkValue < 0 || chunkValue > 255)
                {
                    return null;
                }

                addressValue |= ((uint)chunkValue << shiftCount);
            }

            return new IPv4Address(addressValue);
        }

        public static bool TryParse(string addressString, out IPv4Address address)
        {
            IPv4Address? ret = MaybeParse(addressString);
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

        public static IPv4Address Parse(string addressString)
        {
            IPv4Address? ret = MaybeParse(addressString);
            if (ret.HasValue)
            {
                return ret.Value;
            }
            throw new FormatException("Invalid IPv4 address.");
        }

        public override string ToString()
        {
            byte[] bytes = Bytes;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}.{2}.{3}",
                (int)bytes[0],
                (int)bytes[1],
                (int)bytes[2],
                (int)bytes[3]
            );
        }

        // byte operations
        public byte[] Bytes => new byte[] {
            (byte)((AddressValue >> 24) & 0xFF),
            (byte)((AddressValue >> 16) & 0xFF),
            (byte)((AddressValue >>  8) & 0xFF),
            (byte)((AddressValue >>  0) & 0xFF)
        };

        public static IPv4Address? MaybeFromBytes(byte[] bytes)
        {
            if (bytes.Length != 4)
            {
                return null;
            }

            uint addressValue =
                ((uint)bytes[0] << 24) |
                ((uint)bytes[1] << 16) |
                ((uint)bytes[2] <<  8) |
                ((uint)bytes[3] <<  0)
            ;
            
            return new IPv4Address(addressValue);
        }

        IPv4Address? IIPAddress<IPv4Address>.MaybeFromBytes(byte[] bytes)
        {
            return MaybeFromBytes(bytes);
        }

        // arithmetic operators
        public static IPv4Address operator&(IPv4Address left, IPv4Address right)
        {
            return new IPv4Address(left.AddressValue & right.AddressValue);
        }

        public IPv4Address BitwiseAnd(IPv4Address other)
        {
            return this & other;
        }

        public static IPv4Address operator|(IPv4Address left, IPv4Address right)
        {
            return new IPv4Address(left.AddressValue | right.AddressValue);
        }

        public static IPv4Address operator^(IPv4Address left, IPv4Address right)
        {
            return new IPv4Address(left.AddressValue ^ right.AddressValue);
        }

        public IPv4Address BitwiseXor(IPv4Address other)
        {
            return this ^ other;
        }

        public static IPv4Address operator~(IPv4Address operand)
        {
            return new IPv4Address(~operand.AddressValue);
        }

        public IPv4Address BitwiseNot()
        {
            return ~this;
        }

        public static IPv4Address operator+(IPv4Address left, IPv4Address right)
        {
            return new IPv4Address(left.AddressValue + right.AddressValue);
        }

        public IPv4Address Add(IPv4Address other)
        {
            return this + other;
        }

        public static IPv4Address operator+(IPv4Address baseAddress, int offset)
        {
            return new IPv4Address((uint)(baseAddress.AddressValue + offset));
        }

        public IPv4Address Add(int offset)
        {
            return this + offset;
        }

        public static IPv4Address operator-(IPv4Address left, IPv4Address right)
        {
            return new IPv4Address(left.AddressValue - right.AddressValue);
        }

        public IPv4Address Subtract(IPv4Address other)
        {
            return this - other;
        }

        public static IPv4Address operator-(IPv4Address baseAddress, int offset)
        {
            return baseAddress + (-offset);
        }

        public IPv4Address Subtract(int offset)
        {
            return this - offset;
        }

        // equality operators and comparisons
        public override bool Equals(object other)
        {
            return other is IPv4Address && this == (IPv4Address)other;
        }

        public bool Equals(IPv4Address other)
        {
            return this == (IPv4Address)other;
        }

        public override int GetHashCode()
        {
            return AddressValue.GetHashCode();
        }

        public static bool operator ==(IPv4Address left, IPv4Address right)
        {
            return left.AddressValue == right.AddressValue;
        }

        public static bool operator !=(IPv4Address left, IPv4Address right)
        {
            return !(left == right);
        }

        public int CompareTo(IPv4Address other)
        {
            return this.AddressValue.CompareTo(other.AddressValue);
        }
    }
}
