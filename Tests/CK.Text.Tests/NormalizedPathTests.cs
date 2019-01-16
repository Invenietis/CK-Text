using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Text.Tests
{
    [TestFixture]
    public class NormalizedPathTests
    {
        [TestCase( "", NormalizedPathRootKind.None, "" )]
        [TestCase( "a", NormalizedPathRootKind.None, "a" )]
        [TestCase( "/a", NormalizedPathRootKind.RootedBySeparator, "/a" )]
        [TestCase( "/a/b", NormalizedPathRootKind.RootedBySeparator, "/a/b" )]
        [TestCase( "/", NormalizedPathRootKind.RootedBySeparator, "/" )]
        [TestCase( "//a", NormalizedPathRootKind.RootedByDoubleSeparator, "//a" )]
        [TestCase( "//a/b", NormalizedPathRootKind.RootedByDoubleSeparator, "//a/b" )]
        [TestCase( "//", NormalizedPathRootKind.RootedByDoubleSeparator, "//" )]
        [TestCase( "c:/", NormalizedPathRootKind.RootedByFirstPart, "c:" )]
        [TestCase( "X:", NormalizedPathRootKind.RootedByFirstPart, "X:" )]
        [TestCase( ":", NormalizedPathRootKind.RootedByFirstPart, ":" )]
        [TestCase( "plop:", NormalizedPathRootKind.RootedByFirstPart, "plop:" )]
        [TestCase( "~", NormalizedPathRootKind.RootedByFirstPart, "~" )]
        [TestCase( "~/", NormalizedPathRootKind.RootedByFirstPart, "~" )]
        [TestCase( "~/a", NormalizedPathRootKind.RootedByFirstPart, "~/a" )]
        [TestCase( "~root", NormalizedPathRootKind.RootedByFirstPart, "~root" )]
        [TestCase( "~R/a", NormalizedPathRootKind.RootedByFirstPart, "~R/a" )]
        public void all_kind_of_root( string p, NormalizedPathRootKind o, string path )
        {
            var n = new NormalizedPath( p );
            n.RootKind.Should().Be( o );
            n.Path.Should().Be( path );
        }

        [TestCase( "", NormalizedPathRootKind.RootedByFirstPart, "ArgumentException" )]
        [TestCase( "/", NormalizedPathRootKind.RootedByFirstPart, "ArgumentException" )]
        [TestCase( "//", NormalizedPathRootKind.RootedByFirstPart, "ArgumentException" )]
        [TestCase( "", NormalizedPathRootKind.RootedBySeparator, "/" )]
        [TestCase( "", NormalizedPathRootKind.RootedByDoubleSeparator, "//" )]
        [TestCase( "c:", NormalizedPathRootKind.RootedByFirstPart, "c:" )]
        [TestCase( "c:", NormalizedPathRootKind.None, "c:" )]
        [TestCase( "c:", NormalizedPathRootKind.RootedBySeparator, "/c:" )]
        [TestCase( "c:", NormalizedPathRootKind.RootedByDoubleSeparator, "//c:" )]
        [TestCase( "a", NormalizedPathRootKind.RootedByFirstPart, "a" )]
        [TestCase( "/a", NormalizedPathRootKind.RootedByFirstPart, "a" )]
        [TestCase( "//a", NormalizedPathRootKind.RootedByFirstPart, "a" )]
        [TestCase( "a", NormalizedPathRootKind.RootedBySeparator, "/a" )]
        [TestCase( "a", NormalizedPathRootKind.RootedByDoubleSeparator, "//a" )]
        [TestCase( "//a", NormalizedPathRootKind.RootedBySeparator, "/a" )]
        [TestCase( "/a", NormalizedPathRootKind.RootedByDoubleSeparator, "//a" )]
        [TestCase( "/~a", NormalizedPathRootKind.RootedByDoubleSeparator, "//~a" )]
        [TestCase( "~a", NormalizedPathRootKind.RootedByDoubleSeparator, "//~a" )]
        public void changing_RootKind( string p, NormalizedPathRootKind newKind, string result )
        {
            if( result == "ArgumentException" )
            {
                new NormalizedPath( p ).Invoking( sut => sut.With( newKind ) )
                        .Should().Throw<ArgumentException>();

            }
            else
            {
                var r = new NormalizedPath( p ).With( newKind );
                r.RootKind.Should().Be( newKind );
                r.Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", '=', "" )]
        [TestCase( null, '=', null )]
        [TestCase( "", '=', null )]
        [TestCase( null, '=', "" )]
        [TestCase( "", '<', "a" )]
        [TestCase( "", '<', "/" )]
        [TestCase( "", '<', "//" )]
        [TestCase( "/", '<', "//" )]
        [TestCase( "/", '<', "/a" )]
        [TestCase( "//", '<', "/a" )]
        [TestCase( "/", '<', "a" )]
        [TestCase( "//", '<', "a" )]
        [TestCase( "a", '=', "a" )]
        [TestCase( "a/b", '>', "a" )]
        [TestCase( "A/B", '=', "a/B" )]
        [TestCase( "a/1", '=', "a/1" )]
        [TestCase( "a/1a", '>', "a/1" )]
        [TestCase( "a/1/b", '<', "a/1/c" )]
        [TestCase( "z", '>', "a" )]
        [TestCase( "z", '<', "a/b" )]
        [TestCase( "z:", '=', "z:/" )]
        [TestCase( "git:", '=', "git://" )]
        [TestCase( "/A", '<', "/B" )]
        public void equality_and_comparison_operators_at_work( string p1, char op, string p2 )
        {
            NormalizedPath n1 = p1;
            NormalizedPath n2 = p2;
            if( op == '=' )
            {
                n1.Equals( n2 ).Should().BeTrue();
                (n1 == n2).Should().BeTrue();
                (n1 != n2).Should().BeFalse();
                (n1 <= n2).Should().BeTrue();
                (n1 < n2).Should().BeFalse();
                (n1 >= n2).Should().BeTrue();
                (n1 > n2).Should().BeFalse();
            }
            else
            {
                bool isGT = op == '>';
                n1.Equals( n2 ).Should().BeFalse();
                (n1 == n2).Should().BeFalse();
                (n1 != n2).Should().BeTrue();
                (n1 <= n2).Should().Be( !isGT );
                (n1 < n2).Should().Be( !isGT );
                (n1 >= n2).Should().Be( isGT );
                (n1 > n2).Should().Be( isGT );
            }
        }

        [TestCase( "", "", false )]
        [TestCase( null, null, false )]
        [TestCase( "", "a", false )]
        [TestCase( "a", "a", false )]
        [TestCase( "a/b", "a", true )]
        [TestCase( "/a/b", "a", false )]
        [TestCase( "/a/b", "/a", true )]
        [TestCase( "a\\b", "a/b", false )]
        [TestCase( "a/b/c/", "a\\b", true )]
        [TestCase( "//a/b/c/", "\\\\A\\B", true )]
        [TestCase( "/a/b/c/", "a/b", false )]
        [TestCase( "a/b/c/", "a\\bc", false )]
        public void StartsWith_at_work( string start, string with, bool result )
        {
            new NormalizedPath( start ).StartsWith( with ).Should().Be( result );
        }

        [TestCase( "", "", true )]
        [TestCase( null, null, true )]
        [TestCase( "", "a", false )]
        [TestCase( "a", "a", true )]
        [TestCase( "a/b", "a", true )]
        [TestCase( "a\\b", "a/b", true )]
        [TestCase( "a/b/c/", "a\\b", true )]
        [TestCase( "a/b/c/", "a\\bc", false )]
        public void StartsWith_NOT_strict_at_work( string start, string with, bool result )
        {
            new NormalizedPath( start ).StartsWith( with, strict: false ).Should().Be( result );
        }

        [TestCase( "", "", false )]
        [TestCase( null, null, false )]
        [TestCase( "", "a", false )]
        [TestCase( "a", "a", false )]
        [TestCase( "a/b", "b", true )]
        [TestCase( "a\\b", "aa/b", false )]
        [TestCase( "a/b/c/", "b\\c", true )]
        [TestCase( "a/b/c/", "bb\\c", false )]
        public void EndsWith_at_work( string root, string end, bool result )
        {
            new NormalizedPath( root ).EndsWith( end ).Should().Be( result );
        }

        [TestCase( "", "", true )]
        [TestCase( null, null, true )]
        [TestCase( "", "a", false )]
        [TestCase( "a", "a", true )]
        [TestCase( "a/b", "b", true )]
        [TestCase( "a\\b", "a/b", true )]
        [TestCase( "a/b/c/", "b\\c", true )]
        public void EndsWith_NOT_strict_at_work( string root, string end, bool result )
        {
            new NormalizedPath( root ).EndsWith( end, strict: false ).Should().Be( result );
        }

        [TestCase( "", "", "" )]
        [TestCase( null, null, "" )]
        [TestCase( "", "a", "a" )]
        [TestCase( "a", "", "a" )]
        [TestCase( "", "a\\b", "a/b" )]
        [TestCase( "", "a\\b", "a/b" )]
        [TestCase( "r", "a\\b", "r/a/b" )]
        [TestCase( "//r", "a\\b", "//r/a/b" )]
        [TestCase( "r/x/", "a\\b", "r/x/a/b" )]
        [TestCase( "/r/x/", "\\a\\b\\", "/a/b" )]
        [TestCase( "/r", "\\a\\b\\", "/a/b" )]
        [TestCase( "/", "\\a\\b\\", "/a/b" )]
        [TestCase( "//", "\\a\\b\\", "/a/b" )]
        [TestCase( "//", "a/b/", "//a/b" )]
        [TestCase( "/", "a", "/a" )]
        [TestCase( "/", "", "/" )]
        public void Combine_at_work( string root, string suffix, string result )
        {
            new NormalizedPath( root ).Combine( suffix ).Should().Be( new NormalizedPath( result ) );
        }

        [TestCase( "", "", "ArgumentNullException" )]
        [TestCase( null, null, "ArgumentNullException" )]
        [TestCase( "", "a", "a" )]
        [TestCase( "first", "a\\b", "ArgumentException" )]
        [TestCase( "", "a/b", "a/b" )]
        [TestCase( "", "/a", "/a" )]
        [TestCase( "", "a/", "a" )]
        [TestCase( "r", "a", "r/a" )]
        [TestCase( "r/x/", "a.t", "r/x/a.t" )]
        [TestCase( "/r", "a", "/r/a" )]
        [TestCase( "//r", "a", "//r/a" )]
        [TestCase( "//", "a", "//a" )]
        [TestCase( "/", "a/b", "/a/b" )]
        // Edge case: AppendPart allows the empty path to be combined with a path.
        [TestCase( "", "a/b/c", "a/b/c" )]
        [TestCase( "", "//", "//" )]
        public void AppendPart_is_like_combine_but_with_part_not_a_path( string root, string suffix, string result )
        {
            if( result == "ArgumentNullException" )
            {
                new NormalizedPath( root ).Invoking( sut => sut.AppendPart( suffix ) )
                        .Should().Throw<ArgumentNullException>();

            }
            else if( result == "ArgumentException" )
            {
                new NormalizedPath( root ).Invoking( sut => sut.AppendPart( suffix ) )
                        .Should().Throw<ArgumentException>();

            }
            else
            {
                new NormalizedPath( root ).AppendPart( suffix )
                        .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", "" )]
        [TestCase( null, "" )]
        [TestCase( "a", "a" )]
        [TestCase( "a\\b", "a/b,a" )]
        [TestCase( "a/b/c", "a/b/c,a/b,a" )]
        public void Parents_does_not_contain_the_empty_root( string p, string result )
        {
            new NormalizedPath( p ).Parents.Select( a => a.ToString() )
                    .Should().BeEquivalentTo( NormalizeExpectedResultAsStrings( result ), o => o.WithStrictOrdering() );
        }

        [TestCase( "", "", "" )]
        [TestCase( "x/y", "", "" )]
        [TestCase( "", "part", "" )]
        [TestCase( "x/y", "part", "x/y/part,x/part" )]
        [TestCase( "x/y", "p1,p2", "x/y/p1,x/y/p2,x/p1,x/p2" )]
        public void PathsToFirstPart_with_null_subPaths_at_work( string root, string parts, string result )
        {
            var nParts = parts.Split( ',' ).Where( x => x.Length > 0 );
            new NormalizedPath( root ).PathsToFirstPart( null, nParts ).Select( a => a.ToString() )
                    .Should().BeEquivalentTo( NormalizeExpectedResultAsStrings( result ), o => o.WithStrictOrdering() );
        }

        [TestCase( "", "", "part", "" )]
        [TestCase( "", "subPath", "part", "" )]
        [TestCase( "x/y", "subPath", "", "" )]
        [TestCase( "/x/y", "subPath", "part", "/x/y/subPath/part,/x/subPath/part" )]
        [TestCase( "/x/y", "", "part", "/x/y/part,/x/part" )]
        [TestCase( "x/y", "a/b", "part", "x/y/a/b/part,x/a/b/part" )]
        [TestCase( "//x/y", "a/b", "part", "//x/y/a/b/part,//x/a/b/part" )]
        [TestCase( "x/y", "a/b,c/d", "p1,p2", "x/y/a/b/p1,x/y/a/b/p2,x/y/c/d/p1,x/y/c/d/p2,x/a/b/p1,x/a/b/p2,x/c/d/p1,x/c/d/p2" )]
        [TestCase( "c:/p", "a/b", "part", "c:/p/a/b/part,c:/a/b/part" )]
        public void PathsToFirstPart_with_paths_and_parts_at_work( string root, string paths, string parts, string result )
        {
            var nPaths = paths.Split( ',' ).Where( x => x.Length > 0 ).Select( x => new NormalizedPath( x ) );
            var nParts = parts.Split( ',' ).Where( x => x.Length > 0 );
            new NormalizedPath( root ).PathsToFirstPart( nPaths, nParts ).Select( a => a.ToString() )
                    .Should().BeEquivalentTo( NormalizeExpectedResultAsStrings( result ), o => o.WithStrictOrdering() );
        }

        [TestCase( "", "" )]
        [TestCase( ".", "" )]
        [TestCase( "..", "InvalidOperationException" )]
        [TestCase( "/..", "InvalidOperationException" )]
        [TestCase( "//..", "InvalidOperationException" )]
        [TestCase( "~/..", "InvalidOperationException" )]
        [TestCase( "c:/..", "InvalidOperationException" )]
        [TestCase( "plop:/..", "InvalidOperationException" )]
        [TestCase( "a/b/../x", "a/x" )]
        [TestCase( "./a/./b/./.././x/.", "a/x" )]
        [TestCase( "a/b/../x/../..", "" )]
        [TestCase( "a/b/../x/../../..", "InvalidOperationException" )]
        public void ResolveDots( string path, string result )
        {
            if( result == "InvalidOperationException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.ResolveDots() )
                        .Should().Throw<InvalidOperationException>();
            }
            else
            {
                new NormalizedPath( path ).ResolveDots()
                        .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "..", "" )]
        [TestCase( "a/b/../x/../../..", "" )]
        [TestCase( "/a/b/../x/../../..", "/" )]
        [TestCase( "//a/b/../x/../../..", "//" )]
        [TestCase( "X:/x/../..", "X:" )]
        [TestCase( "X:/x/../../../../A", "X:/A" )]
        public void ResolveDots_with_throwOnAboveRoot_false( string path, string result )
        {
            new NormalizedPath( path ).ResolveDots( throwOnAboveRoot: false )
                    .Should().Be( new NormalizedPath( result ) );
        }

        [TestCase( "", 1, "ArgumentOutOfRangeException" )]
        [TestCase( "a", 2, "ArgumentOutOfRangeException" )]
        [TestCase( ".", 1, "." )]
        [TestCase( "A/..", 1, "InvalidOperationException" )]
        [TestCase( "a/b/../x", 3, "a/b/../x" )]
        [TestCase( "./a/./b/./.././x/.", 2, "./a/x" )]
        [TestCase( "a/b/../x/../..", 1, "InvalidOperationException" )]
        [TestCase( "PRO/TECT/ED/a/b/../x/../../..", 3, "InvalidOperationException" )]
        public void ResolveDots_with_locked_root( string path, int rootPartsCount, string result )
        {
            if( result == "ArgumentOutOfRangeException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.ResolveDots( rootPartsCount: rootPartsCount ) )
                        .Should().Throw<ArgumentOutOfRangeException>();
            }
            else if( result == "InvalidOperationException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.ResolveDots( rootPartsCount: rootPartsCount ) )
                        .Should().Throw<InvalidOperationException>();
            }
            else
            {
                new NormalizedPath( path ).ResolveDots( rootPartsCount: rootPartsCount )
                        .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", 0, "ArgumentOutOfRangeException" )]
        [TestCase( "a", 1, "ArgumentOutOfRangeException" )]
        [TestCase( "a/b", 2, "ArgumentOutOfRangeException" )]
        [TestCase( "a", -1, "ArgumentOutOfRangeException" )]
        [TestCase( "a/b", -1, "ArgumentOutOfRangeException" )]
        [TestCase( "a", 0, "" )]
        [TestCase( "a/b", 0, "b" )]
        [TestCase( "a/b", 1, "a" )]
        [TestCase( "/a/b/c/", 1, "/a/c" )]
        [TestCase( "//a/b/c/", 0, "//b/c" )]
        public void RemovePart_at_work( string path, int index, string result )
        {
            if( result == "ArgumentOutOfRangeException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.RemovePart( index ) )
                        .Should().Throw<ArgumentOutOfRangeException>();
            }
            else
            {
                new NormalizedPath( path ).RemovePart( index )
                    .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", 0, 0, "ArgumentOutOfRangeException" )]
        [TestCase( "a", 0, 1, "" )]
        [TestCase( "a/b", 0, 2, "" )]
        [TestCase( "a", -1, 1, "ArgumentOutOfRangeException" )]
        [TestCase( "a/b", 1, 0, "a/b" )]
        [TestCase( "a/b", 2, 0, "ArgumentOutOfRangeException" )]
        [TestCase( "//a/b/c/d", 0, 1, "//b/c/d" )]
        [TestCase( "/a/b/c/d", 0, 2, "/c/d" )]
        [TestCase( "//a/b/c/d", 1, 2, "//a/d" )]
        [TestCase( "/a/b/c/d", 1, 2, "/a/d" )]
        [TestCase( "/a/b/c/d", 2, 2, "/a/b" )]
        public void RemoveParts_at_work( string path, int startIndex, int count, string result )
        {
            if( result == "ArgumentOutOfRangeException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.RemoveParts( startIndex, count ) )
                        .Should().Throw<ArgumentOutOfRangeException>();
            }
            else
            {
                new NormalizedPath( path ).RemoveParts( startIndex, count )
                    .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", -1, "ArgumentException" )]
        [TestCase( "", 0, "" )]
        [TestCase( "", 1, "ArgumentException" )]
        [TestCase( "A", -1, "ArgumentException" )]
        [TestCase( "A", 1, "" )]
        [TestCase( "A/B", 1, "A" )]
        [TestCase( "A/B", 2, "" )]
        [TestCase( "A/B/C", 1, "A/B" )]
        [TestCase( "A/B/C", 2, "A" )]
        [TestCase( "A/B/C", 3, "" )]
        [TestCase( "A/B/C", 4, "ArgumentException" )]
        [TestCase( "A/B/C/D", -1, "ArgumentException" )]
        [TestCase( "A/B/C/D", 0, "A/B/C/D" )]
        [TestCase( "A/B/C/D", 1, "A/B/C" )]
        [TestCase( "A/B/C/D", 2, "A/B" )]
        [TestCase( "A/B/C/D", 3, "A" )]
        [TestCase( "A/B/C/D", 4, "" )]
        [TestCase( "A/B/C/D", 5, "ArgumentException" )]
        [TestCase( @"C:\Dev\CK-Database-Projects\CK-Sqlite\CK.Sqlite.Setup.Runtime\bin\Debug\netcoreapp2.1\publish", 4, @"C:\Dev\CK-Database-Projects\CK-Sqlite\CK.Sqlite.Setup.Runtime" )]
        public void RemoveLastPart_at_work( string path, int count, string result )
        {
            if( result == "ArgumentException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.RemoveLastPart( count ) )
                        .Should().Throw<ArgumentException>();
            }
            else
            {
                new NormalizedPath( path ).RemoveLastPart( count )
                        .Should().Be( new NormalizedPath( result ) );
            }
        }
        [TestCase( "", -1, "ArgumentException" )]
        [TestCase( "", 0, "" )]
        [TestCase( "", 1, "ArgumentException" )]
        [TestCase( "A", -1, "ArgumentException" )]
        [TestCase( "A", 1, "" )]
        [TestCase( "A/B", 1, "B" )]
        [TestCase( "A/B", 2, "" )]
        [TestCase( "A/B/C", 1, "B/C" )]
        [TestCase( "A/B/C", 2, "C" )]
        [TestCase( "A/B/C", 3, "" )]
        [TestCase( "A/B/C", 4, "ArgumentException" )]
        [TestCase( "A/B/C/D", -1, "ArgumentException" )]
        [TestCase( "A/B/C/D", 0, "A/B/C/D" )]
        [TestCase( "A/B/C/D", 1, "B/C/D" )]
        [TestCase( "A/B/C/D", 2, "C/D" )]
        [TestCase( "A/B/C/D", 3, "D" )]
        [TestCase( "A/B/C/D", 4, "" )]
        [TestCase( "A/B/C/D", 5, "ArgumentException" )]
        [TestCase( @"C:\Dev\CK-Database-Projects\CK-Sqlite\CK.Sqlite.Setup.Runtime\bin\Debug\netcoreapp2.1\publish", 4, @"CK.Sqlite.Setup.Runtime\bin\Debug\netcoreapp2.1\publish" )]
        public void RemoveFirstPart_at_work( string path, int count, string result )
        {
            if( result == "ArgumentException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.RemoveFirstPart( count ) )
                        .Should().Throw<ArgumentException>();
            }
            else
            {
                new NormalizedPath( path ).RemoveFirstPart( count )
                        .Should().Be( new NormalizedPath( result ) );
            }
        }

        static IEnumerable<string> NormalizeExpectedResultAsStrings( string result ) => NormalizeExpectedResult( result ).Select( x => x.ToString() );

        static IEnumerable<NormalizedPath> NormalizeExpectedResult( string result )
        {
            return result.Split( ',' )
                                .Where( x => x.Length > 0 )
                                .Select( x => new NormalizedPath( x ) );
        }

    }
}
