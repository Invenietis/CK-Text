using FluentAssertions;
using System;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace CK.Text.Virtual.Tests
{
    [TestFixture]
    partial class VirtualStringStreamTester
    {
        [Test]
        public void open_and_read_file()
        {
            string content = File.ReadAllText( Path.Combine( TestHelper.DataFolder, "basic.json" ) ).NormalizeEOLToCRLF();

            using( Stream stream = new MemoryStream( Encoding.UTF8.GetBytes( content ) ) )
            {
                VirtualStringMatcher m = new VirtualStringMatcher( new VirtualString( stream, 0, 20 ) );
                m.Text[0].Should().Be( '{' );
                m.Text[12].Should().Be( 'n' );
                m.Text[m.Length - 1].Should().Be( '}' );
                m.Text.GetText( 342, 19 ).Should().Be( "Name of the product" );
                m.Text.GetText( 343, 19 ).Should().NotBe( "Name of the product" );
                m.Text.GetText( 172, 65 ).Should().Be( "\"description\":\"Product identifier that responds to special needs\"" );
                m.Text[755].Should().Be( '"' );
                m.Text.GetText( 756, 34 ).Should().Be( "stringReallyReallyReallyReallyLong" );
            }
        }

        [Test]
        public void extracting_properties_from_a_JSON()
        {
            using( Stream fileStream = new FileStream( Path.Combine( TestHelper.DataFolder, "properties.json" ), FileMode.Open, FileAccess.Read ) )
            {
                JSONProperties p = new JSONProperties( new VirtualStringMatcher( new VirtualString( fileStream ) ) );
                p.Visit();
                p.Properties.Should().BeEquivalentTo( new[] { "p1", "p2", "p3", "p4Before", "pSub", "p4", "p5", "p6", "p7" } );
                p.Paths.Should().BeEquivalentTo( new[] {
                " => 0=p1",
                " => 1=p2",
                "1=p2 => 0=p3",
                "1=p2|0=p3|0= => 0=p4Before",
                "1=p2|0=p3|0=|0=p4Before|2= => 0=pSub",
                "1=p2|0=p3|0= => 1=p4",
                "1=p2|0=p3|0=|1=p4 => 0=p5",
                "1=p2|0=p3|0=|1=p4 => 1=p6",
                "1=p2|0=p3|0=|1=p4 => 2=p7" } );
            }
        }

        [Test]
        public void summing_all_doubles_in_a_json()
        {
            using( Stream fileStream = new FileStream( Path.Combine( TestHelper.DataFolder, "doubles.json" ), FileMode.Open, FileAccess.Read ) )
            {
                var v = new JSONDoubleSum( new VirtualStringMatcher( new VirtualString( fileStream ) ) );
                v.Visit();
                v.Sum.Should().Be( 9.87e2 + 8.65 + 45.98 + 12.786 + 874.6324 );
            }
        }

        [Test]
        public void using_JSONVisitor_to_transform_all_doubles_in_it()
        {
            using( Stream fileStream = new FileStream( Path.Combine( TestHelper.DataFolder, "doubles.json" ), FileMode.Open, FileAccess.Read ) )
            {
                var v = new JSONDoubleRewriter( new VirtualStringMatcher( new VirtualString( fileStream ) ), d =>
                     {
                         Console.WriteLine( "{0} => {1}", d, Math.Floor( d ).ToString() );
                         return Math.Floor( d ).ToString();
                     } );

                string rewritten = v.Rewrite();

                var summer = new JSONDoubleSum( new VirtualStringMatcher( new FakeVirtualString( rewritten ) ) );
                summer.Visit();
                summer.Sum.Should().Be( 987 + 8 + 45 + 12 + 874 );
            }
        }

        [Test]
        public void minifying_JSON()
        {
            using( Stream fileStream = new FileStream( Path.Combine( TestHelper.DataFolder, "minifying.json" ), FileMode.Open, FileAccess.Read ) )
            {
                string mini = JSONMinifier.Minify( new VirtualStringMatcher( new VirtualString( fileStream ) ) );
                mini.Should().Be( @"{""v"":9.87e2,""a"":[8.65,true,{},{""x"":null,""y"":0.0},874]}" );
            }
        }

        [Test]
        public void virtual_string_out_of_range_issue_7()
        {
            using( Stream stream = new StupidStream() )
            {
                var v = new VirtualString( stream, 0, 256 );
                {
                    // Reproduces: https://github.com/Invenietis/CK-Text/issues/7 
                    v.GetText( 6810, 1 ).Should().Be( StupidStream.CharAt( 6810 ).ToString() );
                    v.Invoking( _ => _.GetText( 7042, 24 ) ).Should().NotThrow();
                }
                // Since we are here, a little systematic stress test:
                for( int start = 0; start < 300; ++start )
                {
                    for( int width = 2; width < 300; ++width )
                    {
                        v.Invoking( _ => _.GetText( start, width ) ).Should().NotThrow();
                    }
                }
            }
        }
        
        [Test]
        public void virtual_string_does_not_support_multibyte_characters_at_buffer_edge()
        {
            Assume.That( false, "VirtualString does not support multi-byte characters." );
            var encoding = new UTF8Encoding( false );
    
            // i = 0 to 25: Single-byte
            // i = 26 and 27: Multi-byte
            // i = 28 to 53: Single-byte
            string testStr = @"ABCDEFGHIJKLMNOPQRSTUVWXYZüABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] utf8bytes = encoding.GetBytes( testStr );

            using( MemoryStream ms = new MemoryStream() )
            {
                ms.Write( utf8bytes );
                ms.Position = 0;

                var v = new VirtualString( ms, 0, 27, encoding );
                {
                    // Bug (?): Reading 57 bytes does not read 57 characters, depending on encoding and content
                    v.GetText( 0, 27 ).Should().Be( "ABCDEFGHIJKLMNOPQRSTUVWXYZü" ); // But is ABCDEFGHIJKLMNOPQRSTUVWXYZ?
                }
            }
        }

        [Test]
        public void virtual_string_does_not_support_multibyte_characters()
        {
            Assume.That( false, "VirtualString does not support multi-byte characters." );
            var encoding = new UTF8Encoding( false );

            // i = 0 to 25: Single-byte
            // i = 26 and 27: Multi-byte
            // i = 28 to 53: Single-byte
            string testStr = @"ABCDEFGHIJKLMNOPQRSTUVWXYZüABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] utf8bytes = encoding.GetBytes( testStr );

            using( MemoryStream ms = new MemoryStream() )
            {
                ms.Write( utf8bytes );
                ms.Position = 0;

                var v = new VirtualString( ms, 0, 256, encoding );
                {
                    v.Invoking( _ => _.GetText( 0, utf8bytes.Length  ) ).Should().NotThrow(); // ArgumentOutOfRangeException
                }
            }
        }
    }
}
