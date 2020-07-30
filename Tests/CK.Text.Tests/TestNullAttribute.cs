#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace CK.Text.Tests
{
    interface TestNullAttribute
    {
        [MemberNotNullWhen( false, nameof( NullableObject ) )]
        bool IsNull { get; }

        object? NullableObject { get; }

    }
    class Test
    {
        void TestMethod( TestNullAttribute test )
        {
            if( !test.IsNull )
            {
                object notNull = test.NullableObject;
            }
        }
    }
}
