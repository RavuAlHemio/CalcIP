using System;

namespace CalcIP
{
    public class IPv4Network : IPNetwork<IPv4Address>, IEquatable<IPv4Network>
    {
        public IPv4Network(IPv4Address address, IPv4Address subnetMask)
            : base(address, subnetMask)
        {
        }

        public IPv4Network(IPv4Address address, int cidrPrefix)
            : base(address, cidrPrefix)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.Equals((IPv4Network)obj);
        }

        public bool Equals(IPv4Network other)
        {
            return BaseAddress == other.BaseAddress && SubnetMask == other.SubnetMask;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
