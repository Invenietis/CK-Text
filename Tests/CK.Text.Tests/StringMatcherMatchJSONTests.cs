using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CK.Text.Tests
{
    [TestFixture]
    public class StringMatcherMatchJSONTests
    {
        [Test]
        public void match_JSON_objects()
        {
            {
                var j = @"{""A"":1,""B"":2}";
                StringMatcher m = new StringMatcher( j );
                object o;
                Assert.That( m.MatchJSONObject( out o ) );
                var list = o as List<KeyValuePair<string, object>>;
                Assert.That( list.Select( k => k.Key + '|' + k.Value ).Concatenate(), Is.EqualTo( "A|1, B|2" ) );
            }
            {
                var j = @"{ ""A"" : 1.0, ""B"" : 2 }";
                StringMatcher m = new StringMatcher( j );
                object o;
                Assert.That( m.MatchJSONObject( out o ) );
                var list = o as List<KeyValuePair<string, object>>;
                Assert.That( list.Select( k => k.Key + '|' + k.Value ).Concatenate(), Is.EqualTo( "A|1, B|2" ) );
            }
            {
                var j = @"{ ""A"" : [ ""a"" , 3 , null , 6], ""B"" : [ 2, 3, ""XX"" ] }";
                StringMatcher m = new StringMatcher( j );
                object o;
                Assert.That( m.MatchJSONObject( out o ) );
                var list = o as List<KeyValuePair<string, object>>;
                Assert.That( list.Select( k => k.Key
                                                + '|'
                                                + ((List<object>)k.Value).Select( v => v?.ToString() ).Concatenate( "+" ) )
                                  .Concatenate(), Is.EqualTo( "A|a+3++6, B|2+3+XX" ) );
            }
        }

        [Test]
        public void match_JSON_empty_array_or_objects()
        {
            {
                var j = @"{}";
                StringMatcher m = new StringMatcher( j );
                object o;
                Assert.That( m.MatchJSONObject( out o ) );
                var list = o as List<KeyValuePair<string, object>>;
                Assert.That( list, Is.Empty );
            }
            {
                var j = @"[]";
                StringMatcher m = new StringMatcher( j );
                object o;
                Assert.That( m.MatchJSONObject( out o ) );
                var list = o as List<object>;
                Assert.That( list, Is.Empty );
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
            StringMatcher m = new StringMatcher( jsonWithComment );
            object o;
            Assert.That( m.MatchJSONObject( out o ) );
            Assert.That( o, Is.EqualTo( 1.2 ) );
        }
    }
}