using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Text.Virtual.Tests
{
    public class JSONProperties : JSONVirtualVisitor
    {
        public List<string> Properties;
        public List<string> Paths;

        public JSONProperties( VirtualStringMatcher m )
            : base( m )
        {
            Properties = new List<string>();
            Paths = new List<string>();
        }

        protected override bool VisitObjectProperty( long startPropertyIndex, string propertyName, int propertyIndex )
        {
            Properties.Add( propertyName );
            Paths.Add( string.Join( "|", Path.Select( x => x.Index + "=" + x.PropertyName ) ) + " => " + propertyIndex + "=" + propertyName );
            return base.VisitObjectProperty( startPropertyIndex, propertyName, propertyIndex );
        }
    }

    public class JSONDoubleSum : JSONVirtualVisitor
    {
        public double Sum;

        public JSONDoubleSum( VirtualStringMatcher m ) : base( m ) { }

        protected override bool VisitTerminalValue()
        {
            Matcher.MatchWhiteSpaces( 0 );
            if( Matcher.TryMatchDoubleValue( out double d ) )
            {
                Sum += d;
                return true;
            }
            else return base.VisitTerminalValue();
        }
    }

    public class JSONDoubleRewriter : JSONVirtualVisitor
    {
        readonly StringBuilder _builder;
        readonly Func<double, string> _rewriter;
        long _lastWriteIdx;

        public JSONDoubleRewriter( VirtualStringMatcher m, Func<double, string> rewriter )
            : base( m )
        {
            _rewriter = rewriter;
            _builder = new StringBuilder();
        }

        public string Rewrite()
        {
            _lastWriteIdx = Matcher.StartIndex;
            _builder.Clear();
            Visit();
            Flush( Matcher.StartIndex );
            return _builder.ToString();
        }

        void Flush( long idx )
        {
            int len = (int)(idx - _lastWriteIdx);
            _builder.Append( Matcher.Text.GetText( _lastWriteIdx, len ) );
            _lastWriteIdx = idx;
        }

        protected override bool VisitTerminalValue()
        {
            Matcher.MatchWhiteSpaces( 0 );
            long idx = Matcher.StartIndex;
            if( Matcher.TryMatchDoubleValue( out double d ) )
            {
                Flush( idx );
                _builder.Append( _rewriter( d ) );
                _lastWriteIdx = Matcher.StartIndex;
                return true;
            }
            else return base.VisitTerminalValue();
        }
    }

    public class JSONMinifier : JSONVirtualVisitor
    {
        readonly StringBuilder _builder;
        long _lastWriteIdx;

        public JSONMinifier( VirtualStringMatcher m )
            : base( m )
        {
            _builder = new StringBuilder();
        }

        static public string Minify( VirtualStringMatcher m )
        {
            return new JSONMinifier( m ).Run();
        }

        string Run()
        {
            _lastWriteIdx = Matcher.StartIndex;
            Visit();
            Flush( Matcher.StartIndex );
            return _builder.ToString();
        }

        void Flush( long idx )
        {
            int len = (int)(idx - _lastWriteIdx);
            _builder.Append( Matcher.Text.GetText( _lastWriteIdx, len ) );
            _lastWriteIdx = idx;
        }

        protected override void SkipWhiteSpaces()
        {
            if( char.IsWhiteSpace( Matcher.Head ) )
            {
                Flush( Matcher.StartIndex );
                Matcher.MatchWhiteSpaces( 0 );
                _lastWriteIdx = Matcher.StartIndex;
            }
        }
    }
}
