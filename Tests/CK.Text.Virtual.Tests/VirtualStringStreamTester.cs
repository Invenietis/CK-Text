using FluentAssertions;
using System;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace CK.Text.Virtual.Tests
{
    [TestFixture]
    class VirtualStringStreamTester
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
    }
}
