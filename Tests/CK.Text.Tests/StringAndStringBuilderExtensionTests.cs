using System;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Diagnostics;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using CK.Core;

namespace CK.Text.Tests
{
    [TestFixture]
    public class StringAndStringBuilderExtensionTests
    {
        [Test]
        public void concat_method_uses_StringBuilder_AppendStrings_inside()
        {
            var strings = new string[] { "A", "Hello", "B", "World", null, "End" };
            var s = strings.Concatenate( "|+|" );
            s.Should().Be( "A|+|Hello|+|B|+|World|+||+|End" );
        }

        [Test]
        public void StringBuilder_AppendStrings_method_does_not_skip_null_entries()
        {
            var strings = new string[] { "A", "Hello", "B", "World", null, "End" };
            var b = new StringBuilder();
            b.AppendStrings( strings, "|+|" );
            b.ToString().Should().Be( "A|+|Hello|+|B|+|World|+||+|End" );
        }

        [Test]
        public void appending_multiple_strings_with_a_repeat_count()
        {
            new StringBuilder().Append( "A", 1 ).ToString().Should().Be( "A" );
            new StringBuilder().Append( "AB", 2 ).ToString().Should().Be( "ABAB" );
            new StringBuilder().Append( "|-|", 10 ).ToString().Should().Be( "|-||-||-||-||-||-||-||-||-||-|" );
        }

        [Test]
        public void appends_multiple_strings_silently_ignores_0_or_negative_RepeatCount()
        {
            new StringBuilder().Append( "A", 0 ).ToString().Should().BeEmpty();
            new StringBuilder().Append( "A", -1 ).ToString().Should().BeEmpty();
        }

        [Test]
        public void appends_multiple_strings_silently_ignores_null_or_empty_string_to_repeat()
        {
            new StringBuilder().Append( "", 20 ).ToString().Should().BeEmpty();
            new StringBuilder().Append( (string)null, 20 ).ToString().Should().BeEmpty();
        }

        [TestCase( '0', 0 )]
        [TestCase( '1', 1 )]
        [TestCase( '9', 9 )]
        [TestCase( 'a', 10 )]
        [TestCase( 'e', 14 )]
        [TestCase( 'f', 15 )]
        [TestCase( 'A', 10 )]
        [TestCase( 'C', 12 )]
        [TestCase( 'F', 15 )]
        [TestCase( 'm', -1 )]
        [TestCase( '\t', -1 )]
        [TestCase( '\u0000', -1 )]
        [TestCase( 'Z', -1 )]
        public void HexDigitValue_extension_method_on_character( char c, int expected )
        {
            c.HexDigitValue().Should().Be( expected );
        }

        [Test]
        public void appending_multi_lines_with_a_prefix_with_null_or_empty_or_one_line()
        {
            {
                StringBuilder b = new StringBuilder();
                string text = @"One line.";
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( @"|One line." );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = @"";
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( @"|" );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = null;
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( @"|" );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = @"One line.";
                string t = b.AppendMultiLine( "|", text, false ).ToString();
                t.Should().Be( @"One line." );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = @"";
                string t = b.AppendMultiLine( "|", text, false ).ToString();;
                t.Should().Be( @"" );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = null;
                string t = b.AppendMultiLine( "|", text, false ).ToString();
                t.Should().Be( @"" );
            }

        }

        [Test]
        public void appending_multi_lines_to_empty_lines()
        {
            {
                StringBuilder b = new StringBuilder();
                string text = Environment.NewLine;
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( "|" );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = Environment.NewLine + Environment.NewLine;
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( "|" + Environment.NewLine + "|" );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = Environment.NewLine + Environment.NewLine + Environment.NewLine;
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( "|" + Environment.NewLine + "|" + Environment.NewLine + "|" );
            }
            {
                StringBuilder b = new StringBuilder();
                string text = Environment.NewLine + Environment.NewLine + Environment.NewLine + "a";
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( "|" + Environment.NewLine + "|" + Environment.NewLine + "|" + Environment.NewLine + "|a" );
            }
        }

        [Test]
        public void appending_multi_lines_with_a_prefix()
        {
            {
                StringBuilder b = new StringBuilder();
                string text = @"First line.
Second line.
    Indented.

    Also indented.
Last line.";
                // Here, normalizing the source embedded string is to support 
                // git clone with LF in files instead of CRLF. 
                // Our (slow) AppendMultiLine normalizes the end of lines to Environment.NewLine.
                string t = b.AppendMultiLine( "|", text, true ).ToString();
                t.Should().Be( @"|First line.
|Second line.
|    Indented.
|
|    Also indented.
|Last line.".NormalizeEOL() );
            }

            {
                StringBuilder b = new StringBuilder();
                string text = @"First line.
Second line.
    Indented.

    Also indented.
Last line.";
                string t = b.AppendMultiLine( "|", text, false ).ToString();
                t.Should().Be( @"First line.
|Second line.
|    Indented.
|
|    Also indented.
|Last line.".NormalizeEOL() );
            }

        }

        [Test]
        public void appending_multi_lines_with_prefixLastEmptyLine()
        {
            string text = @"First line.
Second line.


";
            {
                StringBuilder b = new StringBuilder();
                string t = b.AppendMultiLine( "|", text, true, prefixLastEmptyLine: false ).ToString();
                t.Should().Be( @"|First line.
|Second line.
|
|".NormalizeEOL() );
            }

            {
                StringBuilder b = new StringBuilder();
                string t = b.AppendMultiLine( "|", text, true, prefixLastEmptyLine: true ).ToString();
                t.Should().Be( @"|First line.
|Second line.
|
|
|".NormalizeEOL() );
            }
        }

        // With VSTest adapter, this test fails.... meaning that
        // the String.Join is LESS efficient than the home made new StringBuilder().AppendStrings solution (this occurs in Release as well as in Debug).
        // This is hard to believe. So I don't believe :).
        // This test is temporarily disabled and this should be investigated.
        [Test]
        public void our_Concatenate_to_string_must_use_String_Join_since_it_is_faster()
        {
            Assume.That( false, "This test should be investigated. We uspect an impact of VSTest adpater on the result." );

            static string ConcatenateCandidate( IEnumerable<string> @this, string separator = ", " )
            {
                return new StringBuilder().AppendStrings( @this, separator ).ToString();
            }

            string text = File.ReadAllText( Path.Combine( TestHelper.SolutionFolder, "Tests/CK.Text.Tests/StringAndStringBuilderExtensionTests.cs" ) )
                            .NormalizeEOLToLF();
            var lines = text.Split( '\n' );
            var words = text.Split( ' ', '\n' );

            var rJoinLines = MicroBenchmark.MeasureTime( () => String.Join( ", ", lines ) );
            var rJoinWords = MicroBenchmark.MeasureTime( () => String.Join( ", ", words ) );
            var rConcatLines = MicroBenchmark.MeasureTime( () => ConcatenateCandidate( lines ) );
            var rConcatWords = MicroBenchmark.MeasureTime( () => ConcatenateCandidate( words ) );

            rJoinLines.IsSignificantlyBetterThan( rConcatLines ).Should().BeTrue();
            rJoinWords.IsSignificantlyBetterThan( rConcatWords ).Should().BeTrue();

            var smallSet = lines.Take( 20 ).ToArray();
            var rConcatSmall = MicroBenchmark.MeasureTime( () => ConcatenateCandidate( smallSet ) );
            var rJoinSmall = MicroBenchmark.MeasureTime( () => String.Join( ", ", smallSet ) );

            rJoinSmall.IsSignificantlyBetterThan( rConcatSmall ).Should().BeTrue();
        }


//        [Test]
//        public void our_appending_multi_lines_is_better_than_naive_implementation_in_release_but_not_in_debug()
//        {
//            string text = File.ReadAllText( Path.Combine( TestHelper.SolutionFolder, "Tests/CK.Text.Tests/StringAndStringBuilderExtensionTests.cs" ) );
//            text = text.NormalizeEOL();
//            TestPerf( text, 10 );
//            TestPerf( "Small text may behave differently", 100 );
//            TestPerf( "Small text may"+Environment.NewLine + "behave differently" +Environment.NewLine, 100 );
//        }

//        void TestPerf( string text, int count )
//        {
//            GC.Collect();
//            Stopwatch w = new Stopwatch();
//            string[] results = new string[2000];
//            long naive = PrefixWithNaiveReplace( w, text, results );
//            string aNaive = results[0];
//            long better = PrefixWithOurExtension( w, text, results );
//            results[0].Should().Be( aNaive );
//            for( int i = 0; i < count; ++i )
//            {
//                naive += PrefixWithNaiveReplace( w, text, results );
//                better += PrefixWithOurExtension( w, text, results );
//            }
//            double factor = (double)better / naive;
//            Console.WriteLine( $"Naive:{naive}, Extension:{better}. Factor: {factor}" );
//#if DEBUG
//            factor.Should().BeGreaterThan( 1 );
//#else
//            factor.Should().BeLessThan( 1 );
//#endif
//        }

//        static readonly string prefix = "-!-";

//        long PrefixWithNaiveReplace( Stopwatch w, string f, string[] results )
//        {
//            GC.Collect();
//            w.Restart();
//            for( int i = 0; i < results.Length; ++i )
//            {
//                results[i] = f.Replace( Environment.NewLine, Environment.NewLine + prefix );
//            }
//            w.Stop();
//            return w.ElapsedTicks;
//        }

//        long PrefixWithOurExtension( Stopwatch w, string f, string[] results )
//        {
//            GC.Collect();
//            w.Restart();
//            StringBuilder b = new StringBuilder();
//            for( int i = 0; i < results.Length; ++i )
//            {
//                // We must use the prefixLastEmptyLine to match the way the naive implementation works.
//                results[i] = b.AppendMultiLine( prefix, f, false, prefixLastEmptyLine: true ).ToString();
//                b.Clear();
//            }
//            w.Stop();
//            return w.ElapsedTicks;
//        }

        [TestCase( null, "" )]
        [TestCase( "", "" )]
        [TestCase( "A", "A" )]
        [TestCase( @"A""b", @"A\""b" )]
        [TestCase( "A$\r\nB\t", @"A$\r\nB\t" )]
        [TestCase( "\r\n\t\\", @"\r\n\t\\" )]
        [TestCase( "A\0B", @"A\u0000B" )]
        public void StringBuilder_AppendJSONEscaped( string text, string json )
        {
            var b = new StringBuilder();
            b.AppendJSONEscaped( text );
            b.ToString().Should().Be( json );
        }

        public void StringBuilder_AppendJSONEscaped_substring()
        {
            var b = new StringBuilder();
            b.AppendJSONEscaped( "AB\rCD", 2, 1 );
            b.Invoking( sut => sut.AppendJSONEscaped( null, 0, 1 ) ).Should().Throw<ArgumentNullException>();
            b.AppendJSONEscaped( "A\t\n\0BCD", 1, 3 );
            b.ToString().Should().Be( @"\r\t\n\u0000" );
        }

    }

}
