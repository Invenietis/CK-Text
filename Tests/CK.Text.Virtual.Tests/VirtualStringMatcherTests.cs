using System;
using FluentAssertions;
using NUnit.Framework;

namespace CK.Text.Virtual.Tests
{
    [TestFixture]
    class VirtualStringMatcherTests
    {
        [Test]
        public void virtualstring_matching()
        {
            FakeVirtualString v = new FakeVirtualString("Hello world!");
            var m = new VirtualStringMatcher(v);
            m.Text.GetText( 0, (int)m.Length ).Should().Be( "Hello world!" );
            m.Text.GetText( 0, (int)m.Length - 1 ).Should().NotBe( "Hello world!" );
            m.Text[5].Should().Be( ' ' );
            m.Text[v.Length - 1].Should().Be( '!' );
            m.Text.GetText( 0, 5 ).Should().Be( "Hello" );
            m.Text.GetText( 6, 5 ).Should().Be( "world" );
        }

        [Test]
        public void simple_char_matching()
        {
            var m = new VirtualStringMatcher(new FakeVirtualString("ABCD"));
            m.MatchChar( 'a' ).Should().BeFalse();
            m.MatchChar( 'A' ).Should().BeTrue();
            m.StartIndex.Should().Be( 1 );
            m.MatchChar( 'A' ).Should().BeFalse();
            m.MatchChar( 'B' ).Should().BeTrue();
            m.MatchChar( 'C' ).Should().BeTrue();
            m.IsEnd.Should().BeFalse();
            m.MatchChar( 'D' ).Should().BeTrue();
            m.MatchChar( 'D' ).Should().BeFalse();
            m.IsEnd.Should().BeTrue();
        }

        [Test]
        public void matching_texts_and_whitespaces()
        {
            FakeVirtualString v = new FakeVirtualString(" AB  \t\r C");
            var m = new VirtualStringMatcher(v);
            Action a;
            m.MatchText( "A" ).Should().BeFalse();
            m.StartIndex.Should().Be( 0 );
            m.MatchWhiteSpaces().Should().BeTrue();
            m.StartIndex.Should().Be( 1 );
            m.MatchText( "A" ).Should().BeTrue();
            m.MatchText( "B" ).Should().BeTrue();
            m.StartIndex.Should().Be( 3 );
            m.MatchWhiteSpaces( 6 ).Should().BeFalse();
            m.MatchWhiteSpaces( 5 ).Should().BeTrue();
            m.StartIndex.Should().Be( 8 );
            m.MatchWhiteSpaces().Should().BeFalse();
            m.StartIndex.Should().Be( 8 );
            m.MatchText( "c" ).Should().BeTrue();
            m.StartIndex.Should().Be( v.Length );
            m.IsEnd.Should().BeTrue();

            a = () => m.MatchText( "c" ); a.ShouldNotThrow();
            a = () => m.MatchWhiteSpaces(); a.ShouldNotThrow();
            m.MatchText( "A" ).Should().BeFalse();
            m.MatchWhiteSpaces().Should().BeFalse();
        }

        [Test]
        public void matching_integers()
        {
            var m = new VirtualStringMatcher(new FakeVirtualString("X3712Y"));
            m.MatchChar( 'X' ).Should().BeTrue();
            m.MatchInt32( out int i ).Should().BeTrue();
            i.Should().Be( 3712 );
            m.MatchChar( 'Y' ).Should().BeTrue();
        }

        [Test]
        public void matching_integers_with_min_max_values()
        {
            var m = new VirtualStringMatcher(new FakeVirtualString("3712 -435 56"));
            m.MatchInt32( out int i, -500, -400 ).Should().BeFalse();
            m.MatchInt32( out i, 0, 3712 ).Should().BeTrue();
            i.Should().Be( 3712 );
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchInt32( out i, 0 ).Should().BeFalse();
            m.MatchInt32( out i, -500, -400 ).Should().BeTrue();
            i.Should().Be( -435 );
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchInt32( out i, 1000, 2000 ).Should().BeFalse();
            m.MatchInt32( out i, 56, 56 ).Should().BeTrue();
            i.Should().Be( 56 );
            m.IsEnd.Should().BeTrue();
        }

        [Test]
        public void match_methods_must_set_an_error()
        {
            var m = new VirtualStringMatcher(new FakeVirtualString("A"));

            CheckMatchError( m, () => m.MatchChar( 'B' ) );
            CheckMatchError( m, () => m.MatchInt32( out int i ) );
            CheckMatchError( m, () => m.MatchText( "PP" ) );
            CheckMatchError( m, () => m.MatchText( "B" ) );
            CheckMatchError( m, () => m.MatchWhiteSpaces() );
        }

        private static void CheckMatchError( VirtualStringMatcher m, Func<bool> fail )
        {
            long idx = m.StartIndex;
            long len = m.Length;
            fail().Should().BeFalse();
            m.IsError.Should().BeTrue();
            m.ErrorMessage.Should().NotBeNullOrEmpty();
            m.StartIndex.Should().Be( idx );
            m.Length.Should().Be( len );
            m.ClearError();
        }

        [Test]
        public void ToString_constains_the_text_and_the_error()
        {
            var m = new VirtualStringMatcher(new FakeVirtualString("The Text"));
            m.SetError( "Plouf..." );
            m.ToString().Contains( "The Text" );
            m.ToString().Contains( "Plouf..." );
        }

        [TestCase( @"null, true", null, ", true" )]
        [TestCase( @"""""X", "", "X" )]
        [TestCase( @"""a""X", "a", "X" )]
        [TestCase( @"""\\""X", @"\", "X" )]
        [TestCase( @"""A\\B""X", @"A\B", "X" )]
        [TestCase( @"""A\\B\r""X", "A\\B\r", "X" )]
        [TestCase( @"""A\\B\r\""""X", "A\\B\r\"", "X" )]
        [TestCase( @"""\u8976""X", "\u8976", "X" )]
        [TestCase( @"""\uABCD\u07FC""X", "\uABCD\u07FC", "X" )]
        [TestCase( @"""\uabCd\u07fC""X", "\uABCD\u07FC", "X" )]
        public void matching_JSONQUotedString( string s, string parsed, string textAfter )
        {
            FakeVirtualString v = new FakeVirtualString(s);
            var m = new VirtualStringMatcher(v);
            m.TryMatchJSONQuotedString( out string result, true ).Should().BeTrue();
            result.Should().Be( parsed );
            m.TryMatchText( textAfter ).Should().BeTrue();

            m = new VirtualStringMatcher( v );
            m.TryMatchJSONQuotedString( true ).Should().BeTrue();
            m.TryMatchText( textAfter ).Should().BeTrue();
        }

        [Test]
        public void simple_json_test()
        {
            string s = @"
{ 
    ""p1"": ""n"", 
    ""p2""  : 
    { 
        ""p3"": 
        [ 
            ""p4"": 
            { 
                ""p5"" : 0.989, 
                ""p6"": [],
                ""p7"": {}
            }
        ] 
    } 
}  ";
            var m = new VirtualStringMatcher(new FakeVirtualString(s));
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '{' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString( out string pName ).Should().BeTrue();
            pName.Should().Be( "p1" );
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString( out pName ).Should().BeTrue();
            pName.Should().Be( "n" );
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ',' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString( out pName ).Should().BeTrue();
            pName.Should().Be( "p2" );
            m.MatchWhiteSpaces( 2 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '{' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString( out pName ).Should().BeTrue();
            pName.Should().Be( "p3" );
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '[' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString( out pName ).Should().BeTrue();
            pName.Should().Be( "p4" );
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '{' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString().Should().BeTrue();
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchDoubleValue().Should().BeTrue();
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ',' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString( out pName ).Should().BeTrue();
            pName.Should().Be( "p6" );
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '[' ).Should().BeTrue();
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ']' ).Should().BeTrue();
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ',' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.TryMatchJSONQuotedString().Should().BeTrue();
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( ':' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '{' ).Should().BeTrue();
            m.MatchWhiteSpaces( 0 ).Should().BeTrue();
            m.MatchChar( '}' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '}' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( ']' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '}' ).Should().BeTrue();
            m.MatchWhiteSpaces().Should().BeTrue();
            m.MatchChar( '}' ).Should().BeTrue();
            m.MatchWhiteSpaces( 2 ).Should().BeTrue();
            m.IsEnd.Should().BeTrue();
        }

        [TestCase( "0", 0 )]
        [TestCase( "9876978", 9876978 )]
        [TestCase( "-9876978", -9876978 )]
        [TestCase( "0.0", 0 )]
        [TestCase( "0.00", 0 )]
        [TestCase( "0.34", 0.34 )]
        [TestCase( "4e5", 4e5 )]
        [TestCase( "4E5", 4E5 )]
        [TestCase( "29380.34e98", 29380.34e98 )]
        [TestCase( "29380.34E98", 29380.34E98 )]
        [TestCase( "-80.34e-98", -80.34e-98 )]
        [TestCase( "-80.34E-98", -80.34E-98 )]
        public void matching_double_values( string s, double d )
        {
            VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString("P" + s + "S"));

            m.MatchChar( 'P' ).Should().BeTrue();
            long idx = m.StartIndex;
            m.TryMatchDoubleValue().Should().BeTrue();
            m.UncheckedMove( idx - m.StartIndex );
            m.TryMatchDoubleValue( out double parsed ).Should().BeTrue();
            parsed.Should().BeApproximately( d, 1f );
            m.MatchChar( 'S' ).Should().BeTrue();
            m.IsEnd.Should().BeTrue();
        }

        [TestCase( "N" )]
        [TestCase( "D" )]
        [TestCase( "B" )]
        [TestCase( "P" )]
        [TestCase( "X" )]
        public void matching_the_5_forms_of_guid( string form )
        {
            var id = Guid.NewGuid();
            string sId = id.ToString(form);
            {
                var m = new VirtualStringMatcher(new FakeVirtualString(sId));
                m.TryMatchGuid( out Guid readId ).Should().BeTrue();
                readId.Should().Be( id );
            }
            {
                var m = new VirtualStringMatcher(new FakeVirtualString("S" + sId));
                m.TryMatchChar( 'S' ).Should().BeTrue();
                m.TryMatchGuid( out Guid readId ).Should().BeTrue();
                readId.Should().Be( id );
            }
            {
                var m = new VirtualStringMatcher(new FakeVirtualString("S" + sId + "T"));
                m.MatchChar( 'S' ).Should().BeTrue();
                m.TryMatchGuid( out Guid readId ).Should().BeTrue();
                readId.Should().Be( id );
                m.MatchChar( 'T' ).Should().BeTrue();
            }
            sId = sId.Remove( sId.Length - 1 );
            {
                var m = new VirtualStringMatcher(new FakeVirtualString(sId));
                m.TryMatchGuid( out Guid readId ).Should().BeFalse();
                m.StartIndex.Should().Be( 0 );
            }
            sId = id.ToString().Insert( 3, "K" ).Remove( 4 );
            {
                var m = new VirtualStringMatcher(new FakeVirtualString(sId));
                m.TryMatchGuid( out Guid readId ).Should().BeFalse();
                m.StartIndex.Should().Be( 0 );
            }
        }
    }
}
