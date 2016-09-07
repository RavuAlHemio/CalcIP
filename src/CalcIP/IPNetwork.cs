using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace CalcIP
{
    public abstract class IPNetwork<TAddress>
        where TAddress : struct, IIPAddress<TAddress>
    {
        public TAddress BaseAddress { get; }
        public TAddress SubnetMask { get; }
        public int? CidrPrefix { get; }

        public IPNetwork(TAddress address, TAddress subnetMask)
        {
            // calculate base address by ANDing address with subnet mask
            TAddress baseAddress = address.BitwiseAnd(subnetMask);
            BaseAddress = baseAddress;
            SubnetMask = subnetMask;
            CidrPrefix = CalcIPUtils.CidrPrefixFromSubnetMaskBytes(subnetMask.Bytes);
        }

        public IPNetwork(TAddress address, int cidrPrefix)
            : this(address, address.SubnetMaskFromCidrPrefix(cidrPrefix))
        {
            SubnetMask = address.SubnetMaskFromCidrPrefix(cidrPrefix);
            TAddress baseAddress = address.BitwiseAnd(SubnetMask);
            BaseAddress = baseAddress;
            CidrPrefix = cidrPrefix;
        }

        public TAddress CiscoWildcard => SubnetMask.BitwiseNot();

        public BigInteger HostCount
        {
            get
            {
                var ret = BigInteger.One;
                var two = new BigInteger(2);
                foreach (byte b in CiscoWildcard.Bytes)
                {
                    int popCount = CalcIPUtils.BytePopCount[b];
                    for (int i = 0; i < popCount; ++i)
                    {
                        ret *= two;
                    }
                }

                // minus network, minus broadcast
                ret -= two;

                return ret;
            }
        }

        public TAddress? FirstHostAddress
        {
            get
            {
                int hostBitsAvailable = CiscoWildcard.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
                if (hostBitsAvailable < 2)
                {
                    // all ones: the base address is the network
                    // all ones except one zero: 0 is the network, 1 is broadcast
                    // => at least two zeroes necessary for a non-degenerate subnet
                    return null;
                }

                var unraveledBaseAddress = CalcIPUtils.UnravelAddress(BaseAddress, SubnetMask);
                var unraveledFirstHostAddress = unraveledBaseAddress.Add(1);
                return CalcIPUtils.WeaveAddress(unraveledFirstHostAddress, SubnetMask);
            }
        }

        public TAddress? BroadcastAddress
        {
            get
            {
                int hostBitsAvailable = CiscoWildcard.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
                if (hostBitsAvailable < 1)
                {
                    // all ones: the base address is the network
                    // => at least one zero necessary for a subnet with a broadcast address
                    return null;
                }

                var unraveledBaseAddress = CalcIPUtils.UnravelAddress(BaseAddress, SubnetMask);
                var hostCountAddress = BaseAddress
                    .SubnetMaskFromCidrPrefix(BaseAddress.Bytes.Length * 8 - hostBitsAvailable)
                    .BitwiseNot();
                var unraveledBroadcastAddress = unraveledBaseAddress.Add(hostCountAddress);
                return CalcIPUtils.WeaveAddress(unraveledBroadcastAddress, SubnetMask);
            }
        }

        public TAddress? LastHostAddress
        {
            get
            {
                int hostBitsAvailable = CiscoWildcard.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
                if (hostBitsAvailable < 2)
                {
                    // all ones: the base address is the network
                    // all ones except one zero: 0 is the network, 1 is broadcast
                    // => at least two zeroes necessary for a non-degenerate subnet
                    return null;
                }

                var unraveledBaseAddress = CalcIPUtils.UnravelAddress(BaseAddress, SubnetMask);
                var hostCountAddress = BaseAddress
                    .SubnetMaskFromCidrPrefix(BaseAddress.Bytes.Length * 8 - hostBitsAvailable)
                    .BitwiseNot();
                var unraveledBroadcastAddress = unraveledBaseAddress.Add(hostCountAddress);
                var unraveledLastHostAddress = unraveledBroadcastAddress.Subtract(1);
                return CalcIPUtils.WeaveAddress(unraveledLastHostAddress, SubnetMask);
            }
        }

        public TAddress NextSubnetBaseAddress
        {
            get
            {
                int hostBitsAvailable = CiscoWildcard.Bytes.Sum(b => CalcIPUtils.BytePopCount[b]);
                var unraveledBaseAddress = CalcIPUtils.UnravelAddress(BaseAddress, SubnetMask);
                var hostCountAddress = BaseAddress
                    .SubnetMaskFromCidrPrefix(BaseAddress.Bytes.Length * 8 - hostBitsAvailable)
                    .BitwiseNot();
                var unraveledBroadcastAddress = unraveledBaseAddress.Add(hostCountAddress);
                var unraveledNextSubnetBaseAddress = unraveledBroadcastAddress.Add(1);
                return CalcIPUtils.WeaveAddress(unraveledNextSubnetBaseAddress, SubnetMask);
            }
        }

        public TAddress LastAddressOfSubnet => BroadcastAddress ?? BaseAddress;

        public override int GetHashCode()
        {
            return 2*BaseAddress.GetHashCode() + 3*SubnetMask.GetHashCode();
        }

        public override string ToString()
        {
            if (CidrPrefix.HasValue)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", BaseAddress, CidrPrefix.Value);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", BaseAddress, SubnetMask);
            }
        }

        public bool Contains(TAddress address)
        {
            return address.BitwiseAnd(SubnetMask).Equals(BaseAddress);
        }

        public bool IsSupersetOf(IPNetwork<TAddress> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // a network A is a superset of a network B if:
            // 1. the base address of B bitwise AND with the subnet mask of A returns the base address of A
            //    (B is contained in A)
            // 2. the subnet mask of A bitwise AND with the subnet mask of B returns the subnet mask of A
            //    (all host bits in B are host bits in A)
            return
                (other.BaseAddress.BitwiseAnd(this.SubnetMask).Equals(this.BaseAddress))
                && (other.SubnetMask.BitwiseAnd(this.SubnetMask).Equals(this.SubnetMask));
        }

        public bool IsSubsetOf(IPNetwork<TAddress> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return other.IsSupersetOf(this);
        }

        public bool Intersects(IPNetwork<TAddress> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            TAddress thisFirst = this.BaseAddress;
            TAddress thisLast = this.LastAddressOfSubnet;
            TAddress otherFirst = other.BaseAddress;
            TAddress otherLast = other.LastAddressOfSubnet;

            // thisFirst <= otherLast && otherFirst <= thisLast
            int comp1 = thisFirst.CompareTo(otherLast);
            int comp2 = otherFirst.CompareTo(thisLast);
            return comp1 <= 0 && comp2 <= 0;
        }
    }
}
