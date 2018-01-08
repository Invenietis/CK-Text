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
        [TestCase( "", "", false )]
        [TestCase( null, null, false )]
        [TestCase( "", "a", false )]
        [TestCase( "a", "a", false )]
        [TestCase( "a/b", "a", true )]
        [TestCase( "a\\b", "a/b", false )]
        [TestCase( "a/b/c/", "a\\b", true )]
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
        public void StartsWith_NOT_strict_at_work( string start, string with, bool result )
        {
            new NormalizedPath( start ).StartsWith( with, strict: false ).Should().Be( result );
        }

        [TestCase( "", "", "" )]
        [TestCase( null, null, "" )]
        [TestCase( "", "a", "a" )]
        [TestCase( "a", "", "a" )]
        [TestCase( "", "a\\b", "a/b" )]
        [TestCase( "", "a\\b", "a/b" )]
        [TestCase( "r", "a\\b", "r/a/b" )]
        [TestCase( "r/x/", "a\\b", "r/x/a/b" )]
        [TestCase( "/r/x/", "\\a\\b\\", "r/x/a/b" )]
        public void Combine_at_work( string root, string suffix, string result )
        {
            new NormalizedPath( root ).Combine( suffix ).Should().Be( new NormalizedPath( result ) );
        }

        [TestCase( "", "", "ArgumentNullException" )]
        [TestCase( null, null, "ArgumentNullException" )]
        [TestCase( "", "a", "a" )]
        [TestCase( "", "a\\b", "ArgumentException" )]
        [TestCase( "", "a/b", "ArgumentException" )]
        [TestCase( "", "/a", "ArgumentException" )]
        [TestCase( "", "a/", "ArgumentException" )]
        [TestCase( "r", "a", "r/a" )]
        [TestCase( "r/x/", "a.t", "r/x/a.t" )]
        public void AppendPart_is_like_combine_but_with_part_not_a_path( string root, string suffix, string result )
        {
            if( result == "ArgumentNullException" )
            {
                new NormalizedPath( root ).Invoking( sut => sut.AppendPart( suffix ) )
                        .ShouldThrow<ArgumentNullException>();

            }
            else if( result == "ArgumentException" )
            {
                new NormalizedPath( root ).Invoking( sut => sut.AppendPart( suffix ) )
                        .ShouldThrow<ArgumentException>();

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
                    .ShouldBeEquivalentTo( NormalizeExpectedResultAsStrings( result ), o => o.WithStrictOrdering() );
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
                    .ShouldBeEquivalentTo( NormalizeExpectedResultAsStrings( result ), o => o.WithStrictOrdering() );
        }

        [TestCase( "", "", "part", "" )]
        [TestCase( "", "subPath", "part", "" )]
        [TestCase( "x/y", "subPath", "", "" )]
        [TestCase( "x/y", "a/b", "part", "x/y/a/b/part,x/a/b/part" )]
        [TestCase( "x/y", "a/b,c/d", "p1,p2", "x/y/a/b/p1,x/y/a/b/p2,x/y/c/d/p1,x/y/c/d/p2,x/a/b/p1,x/a/b/p2,x/c/d/p1,x/c/d/p2" )]
        public void PathsToFirstPart_with_paths_and_parts_at_work( string root, string paths, string parts, string result )
        {
            var nPaths = paths.Split( ',' ).Where( x => x.Length > 0 ).Select( x => new NormalizedPath( x ) );
            var nParts = parts.Split( ',' ).Where( x => x.Length > 0 );
            new NormalizedPath( root ).PathsToFirstPart( nPaths, nParts ).Select( a => a.ToString() )
                    .ShouldBeEquivalentTo( NormalizeExpectedResultAsStrings( result ), o => o.WithStrictOrdering() );
        }

        [TestCase( "", "" )]
        [TestCase( ".", "" )]
        [TestCase( "..", "InvalidOperationException" )]
        [TestCase( "a/b/../x", "a/x" )]
        [TestCase( "./a/./b/./.././x/.", "a/x" )]
        [TestCase( "a/b/../x/../..", "" )]
        [TestCase( "a/b/../x/../../..", "InvalidOperationException" )]
        public void ResolveDots( string path, string result )
        {
            if( result == "InvalidOperationException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.ResolveDots() )
                        .ShouldThrow<InvalidOperationException>();
            }
            else
            {
                new NormalizedPath( path ).ResolveDots()
                        .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "..", "" )]
        [TestCase( "a/b/../x/../../..", "" )]
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
                        .ShouldThrow<ArgumentOutOfRangeException>();
            }
            else if( result == "InvalidOperationException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.ResolveDots( rootPartsCount: rootPartsCount ) )
                        .ShouldThrow<InvalidOperationException>();
            }
            else
            {
                new NormalizedPath( path ).ResolveDots( rootPartsCount: rootPartsCount )
                        .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", 0, "IndexOutOfRangeException" )]
        [TestCase( "a", 1, "IndexOutOfRangeException" )]
        [TestCase( "a/b", 2, "IndexOutOfRangeException" )]
        [TestCase( "a", -1, "IndexOutOfRangeException" )]
        [TestCase( "a/b", -1, "IndexOutOfRangeException" )]
        [TestCase( "a", 0, "" )]
        [TestCase( "a/b", 0, "b" )]
        [TestCase( "a/b", 1, "a" )]
        [TestCase( "/a/b/c/", 1, "a/c" )]
        public void RemovePart_at_work( string path, int index, string result )
        {
            if( result == "IndexOutOfRangeException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.RemovePart( index ) )
                        .ShouldThrow<IndexOutOfRangeException>();
            }
            else
            {
                new NormalizedPath( path ).RemovePart( index )
                    .Should().Be( new NormalizedPath( result ) );
            }
        }

        [TestCase( "", 0, 0, "IndexOutOfRangeException" )]
        [TestCase( "a", 0, 1, "" )]
        [TestCase( "a/b", 0, 2, "" )]
        [TestCase( "a", -1, 1, "IndexOutOfRangeException" )]
        [TestCase( "a/b", 1, 0, "a/b" )]
        [TestCase( "a/b", 2, 0, "IndexOutOfRangeException" )]
        [TestCase( "/a/b/c/d", 0, 2, "c/d" )]
        [TestCase( "/a/b/c/d", 1, 2, "a/d" )]
        [TestCase( "/a/b/c/d", 2, 2, "a/b" )]
        public void RemoveParts_at_work( string path, int startIndex, int count, string result )
        {
            if( result == "IndexOutOfRangeException" )
            {
                new NormalizedPath( path ).Invoking( sut => sut.RemoveParts( startIndex, count ) )
                        .ShouldThrow<IndexOutOfRangeException>();
            }
            else
            {
                new NormalizedPath( path ).RemoveParts( startIndex, count )
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
