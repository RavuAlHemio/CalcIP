using System;
using System.Collections.Generic;
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

                var unraveledBaseAddress = Unravel(BaseAddress, SubnetMask);
                var unraveledFirstHostAddress = unraveledBaseAddress.Add(1);
                return Weave(unraveledFirstHostAddress, SubnetMask);
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

                var unraveledBaseAddress = Unravel(BaseAddress, SubnetMask);
                var hostCountAddress = BaseAddress
                    .SubnetMaskFromCidrPrefix(BaseAddress.Bytes.Length * 8 - hostBitsAvailable)
                    .BitwiseNot();
                var unraveledBroadcastAddress = unraveledBaseAddress.Add(hostCountAddress);
                return Weave(unraveledBroadcastAddress, SubnetMask);
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

                var unraveledBaseAddress = Unravel(BaseAddress, SubnetMask);
                var hostCountAddress = BaseAddress
                    .SubnetMaskFromCidrPrefix(BaseAddress.Bytes.Length * 8 - hostBitsAvailable)
                    .BitwiseNot();
                var unraveledBroadcastAddress = unraveledBaseAddress.Add(hostCountAddress);
                var unraveledLastHostAddress = unraveledBroadcastAddress.Subtract(1);
                return Weave(unraveledLastHostAddress, SubnetMask);
            }
        }

        public TAddress LastAddressOfSubnet => BroadcastAddress ?? BaseAddress;

        public override int GetHashCode()
        {
            return 2*BaseAddress.GetHashCode() + 3*SubnetMask.GetHashCode();
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

        protected static TAddress Unravel(TAddress address, TAddress subnetMask)
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

        protected static TAddress Weave(TAddress address, TAddress subnetMask)
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
