using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Text.Tests
{
    [TestFixture]
    public class NormalizedPathTests
    {
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
    }
}
