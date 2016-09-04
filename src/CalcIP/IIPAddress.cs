using System;

namespace CalcIP
{
    public interface IIPAddress<TAddress> : IEquatable<TAddress>
        where TAddress : struct
    {
        byte[] Bytes { get; }

        TAddress BitwiseAnd(TAddress other);
        TAddress BitwiseNot();
        TAddress Add(TAddress other);
        TAddress Add(int offset);
        TAddress Subtract(TAddress other);
        TAddress Subtract(int offset);

        TAddress SubnetMaskFromCidrPrefix(int cidrPrefix);
        TAddress? MaybeFromBytes(byte[] bytes);
    }
}
