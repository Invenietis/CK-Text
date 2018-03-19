using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.Text.Virtual.Tests
{
    [TestFixture]
    public class JSONVirtualVisitorTests
    {
        [Test]
        public void extracting_properties_from_a_JSON()
        {
            string s = @"
{ 
    ""p1"": ""n"", 
    ""p2"": 
    { 
        ""p3"": 
        [ 
            {
                ""p4Before"": [""zero"", ""one"", { ""pSub"": [] }, ""three"" ]
                ""p4"": 
                { 
                    ""p5"" : 0.989, 
                    ""p6"": [],
                    ""p7"": {}
                }
            }
        ] 
    } 
}";
            JSONProperties p = new JSONProperties(new VirtualStringMatcher(new FakeVirtualString(s)));
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

        [Test]
        public void summing_all_doubles_in_a_json()
        {
            string data = @"
{
    ""v"": 9.87e2, 
    ""a"": [ 8.65, true, {}, {""x"" : 45.98, ""y"":12.786}, 874.6324 ]
}";
            var v = new JSONDoubleSum(new VirtualStringMatcher(new FakeVirtualString(data)));
            v.Visit();
            v.Sum.Should().Be( 9.87e2 + 8.65 + 45.98 + 12.786 + 874.6324 );
        }

        [Test]
        public void using_JSONVisitor_to_transform_all_doubles_in_it()
        {
            string data = @"
{
    ""v"": 9.87e2, 
    ""a"": [ 8.65, true, {}, {""x"" : 45.98, ""y"":12.786}, 874.6324 ]
}";
            var v = new JSONDoubleRewriter(new VirtualStringMatcher(new FakeVirtualString(data)), d =>
            {
                Console.WriteLine("{0} => {1}", d, Math.Floor(d).ToString());
                return Math.Floor(d).ToString();
            });

            string rewritten = v.Rewrite();

            var summer = new JSONDoubleSum(new VirtualStringMatcher(new FakeVirtualString(rewritten)));
            summer.Visit();
            summer.Sum.Should().Be( 987 + 8 + 45 + 12 + 874 );
        }



        [Test]
        public void minifying_JSON()
        {
            string data = @"
{
    ""v"": 9.87e2, 
    ""a"" : 
        [ 8.65, 
            true, 
            { } 
            , { ""x"" : null,           ""y"": 0.0      }
        , 874 
]
}";
            string mini = JSONMinifier.Minify(new VirtualStringMatcher(new FakeVirtualString(data)));
            mini.Should().Be( @"{""v"":9.87e2,""a"":[8.65,true,{},{""x"":null,""y"":0.0},874]}" );
        }
    }
}
