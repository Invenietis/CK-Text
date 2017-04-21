using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CK.Text.Virtual.Tests
{
    [TestFixture]
    public class StringMatcherMatchJSONTests
    {
        [Test]
        public void match_JSON_objects()
        {
            {
                var j = @"{""A"":1,""B"":2}";
                VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString(j));
                m.MatchJSONObject( out object o ).Should().BeTrue();
                var list = o as List<KeyValuePair<string, object>>;
                list.Select( k => k.Key + '|' + k.Value ).Concatenate().Should().Be( "A|1, B|2" );
            }
            {
                var j = @"{ ""A"" : 1.0, ""B"" : 2 }";
                VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString(j));
                m.MatchJSONObject( out object o ).Should().BeTrue();
                var list = o as List<KeyValuePair<string, object>>;
                list.Select( k => k.Key + '|' + k.Value ).Concatenate().Should().Be( "A|1, B|2" );
            }
            {
                var j = @"{ ""A"" : [ ""a"" , 3 , null , 6], ""B"" : [ 2, 3, ""XX"" ] }";
                VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString(j));
                m.MatchJSONObject( out object o ).Should().BeTrue();
                var list = o as List<KeyValuePair<string, object>>;
                list.Select( k => k.Key
                                                + '|'
                                                + ((List<object>)k.Value).Select( v => v?.ToString() ).Concatenate( "+" ) )
                                  .Concatenate().Should().Be( "A|a+3++6, B|2+3+XX" );
            }
        }

        [Test]
        public void match_JSON_empty_array_or_objects()
        {
            {
                var j = @"{}";
                VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString(j));
                m.MatchJSONObject( out object o ).Should().BeTrue();
                var list = o as List<KeyValuePair<string, object>>;
                list.Should().BeEmpty();
            }
            {
                var j = @"[]";
                VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString(j));
                m.MatchJSONObject( out object o ).Should().BeTrue();
                var list = o as List<object>;
                list.Should().BeEmpty();
            }
        }

        [TestCase( "1.2" )]
        [TestCase( "/* ... */1.2// ..." )]
        [TestCase( "1.2/* ..." )]
        [TestCase( "/* ... */1.2// ..." )]
        [TestCase( @"/*
*/  // ...

/* 3 */ 1.2     
//...
/*" )]
        public void match_JSON_skips_JS_comments( string jsonWithComment )
        {
            VirtualStringMatcher m = new VirtualStringMatcher(new FakeVirtualString(jsonWithComment));
            m.MatchJSONObject( out object o ).Should().BeTrue();
            o.Should().Be( 1.2 );
        }
    }
}
