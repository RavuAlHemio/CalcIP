using System;

namespace CalcIP
{
    public class IPv6Network : IPNetwork<IPv6Address>, IEquatable<IPv6Network>
    {
        public IPv6Network(IPv6Address address, IPv6Address subnetMask)
            : base(address, subnetMask)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.Equals((IPv6Network)obj);
        }

        public bool Equals(IPv6Network other)
        {
            return BaseAddress == other.BaseAddress && SubnetMask == other.SubnetMask;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
