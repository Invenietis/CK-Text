using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CK.Text
{
    /// <summary>
    /// Extends <see cref="VirtualStringMatcher"/> with useful (yet basic) methods.
    /// </summary>
    public static class VirtualStringMatcherTextExtension
    {
        /// <summary>
        /// Matches a Guid. No error is set if match fails.
        /// </summary>
        /// <remarks>
        /// Any of the 5 forms of Guid can be matched:
        /// <list type="table">
        /// <item><term>N</term><description>00000000000000000000000000000000</description></item>
        /// <item><term>D</term><description>00000000-0000-0000-0000-000000000000</description></item>
        /// <item><term>B</term><description>{00000000-0000-0000-0000-000000000000}</description></item>
        /// <item><term>P</term><description>(00000000-0000-0000-0000-000000000000)</description></item>
        /// <item><term>X</term><description>{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}</description></item>
        /// </list>
        /// </remarks>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="id">The result Guid. <see cref="Guid.Empty"/> on failure.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool TryMatchGuid( this VirtualStringMatcher @this, out Guid id )
        {
            id = Guid.Empty;
            if( @this.Length < 32 ) return false;
            if( @this.Head == '{' )
            {
                // Form "B" or "X".
                if( @this.Length < 38 ) return false;
                if( @this.Text[@this.StartIndex + 37] == '}' )
                {
                    // The "B" form.
                    if( Guid.TryParseExact( @this.Text.GetText( @this.StartIndex, 38 ), "B", out id ) )
                    {
                        return @this.UncheckedMove( 38 );
                    }
                    return false;
                }
                // The "X" form.
                if( @this.Length >= 68 && Guid.TryParseExact( @this.Text.GetText( @this.StartIndex, 68 ), "X", out id ) )
                {
                    return @this.UncheckedMove( 68 );
                }
                return false;
            }
            if( @this.Head == '(' )
            {
                // Can only be the "P" form.
                if( @this.Length >= 38 && Guid.TryParseExact( @this.Text.GetText( @this.StartIndex, 38 ), "P", out id ) )
                {
                    return @this.UncheckedMove( 38 );
                }
                return false;
            }
            if( @this.Head.HexDigitValue() >= 0 )
            {
                // The "N" or "D" form.
                if( @this.Length >= 36 && @this.Text[@this.StartIndex + 8] == '-' )
                {
                    // The ""D" form.
                    if( Guid.TryParseExact( @this.Text.GetText( @this.StartIndex, 36 ), "D", out id ) )
                    {
                        return @this.UncheckedMove( 36 );
                    }
                    return false;
                }
                if( Guid.TryParseExact( @this.Text.GetText( @this.StartIndex, 32 ), "N", out id ) )
                {
                    return @this.UncheckedMove( 32 );
                }
            }
            return false;
        }

        /// <summary>
        /// Matches a JSON quoted string without setting an error if match fails.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="content">Extracted content.</param>
        /// <param name="allowNull">True to allow 'null'.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool TryMatchJSONQuotedString( this VirtualStringMatcher @this, out string content, bool allowNull = false )
        {
            content = null;
            if( @this.IsEnd ) return false;
            long i = @this.StartIndex;
            if( @this.Text[i++] != '"' ) return allowNull && @this.TryMatchText( "null" );
            long len = @this.Length - 1;
            StringBuilder b = null;
            while( len >= 0 )
            {
                if( len == 0 ) return false;
                char c = @this.Text[i++];
                --len;
                if( c == '"' ) break;
                if( c == '\\' )
                {
                    if( len == 0 ) return false;
                    if( b == null ) b = new StringBuilder( @this.Text.GetText( @this.StartIndex + 1, (int)(i - @this.StartIndex - 2) ) );
                    switch( (c = @this.Text[i++]) )
                    {
                        case 'r': c = '\r'; break;
                        case 'n': c = '\n'; break;
                        case 'b': c = '\b'; break;
                        case 't': c = '\t'; break;
                        case 'f': c = '\f'; break;
                        case 'u':
                            {
                                if( --len == 0 ) return false;
                                int cN = ReadHexDigit(@this.Text[i++]);
                                if( cN < 0 || cN > 15 ) return false;
                                int val = cN << 12;
                                if( --len == 0 ) return false;
                                cN = ReadHexDigit( @this.Text[i++] );
                                if( cN < 0 || cN > 15 ) return false;
                                val |= cN << 8;
                                if( --len == 0 ) return false;
                                cN = ReadHexDigit( @this.Text[i++] );
                                if( cN < 0 || cN > 15 ) return false;
                                val |= cN << 4;
                                if( --len == 0 ) return false;
                                cN = ReadHexDigit( @this.Text[i++] );
                                if( cN < 0 || cN > 15 ) return false;
                                val |= cN;
                                c = (char)val;
                                break;
                            }
                    }
                }
                if( b != null ) b.Append( c );
            }
            long lenS = i - @this.StartIndex;
            if( b != null ) content = b.ToString();
            else content = @this.Text.GetText( @this.StartIndex + 1, (int)lenS - 2 );
            return @this.UncheckedMove( lenS );
        }

        static int ReadHexDigit( char c )
        {
            int cN = c - '0';
            if( cN >= 49 ) cN -= 39;
            else if( cN >= 17 ) cN -= 7;
            return cN;
        }

        /// <summary>
        /// Matches a quoted string without extracting its content.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="allowNull">True to allow 'null'.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool TryMatchJSONQuotedString( this VirtualStringMatcher @this, bool allowNull = false )
        {
            if( @this.IsEnd ) return false;
            long i = @this.StartIndex;
            if( @this.Text[i++] != '"' ) return allowNull && @this.TryMatchText( "null" );
            long len = @this.Length - 1;
            while( len >= 0 )
            {
                if( len == 0 ) return false;
                char c = @this.Text[i++];
                --len;
                if( c == '"' ) break;
                if( c == '\\' )
                {
                    i++;
                    --len;
                }
            }
            return @this.UncheckedMove( i - @this.StartIndex );
        }

        /// <summary>
        /// The <see cref="Regex"/> that <see cref="TryMatchDoubleValue(VirtualStringMatcher)"/> uses to avoid
        /// calling <see cref="double.TryParse(string, out double)"/> when resolving the value is 
        /// useless.
        /// </summary>
        static public readonly Regex RegexDouble = new Regex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?((e|E)(\+|-)?[0-9]+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        const int maxDoubleLength = 24;

        /// <summary>
        /// Matches a double without getting its value nor setting an error if match fails.
        /// This uses <see cref="RegexDouble"/> with a string's length of 24.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool TryMatchDoubleValue( this VirtualStringMatcher @this )
        {
            long len = Math.Min(@this.Length, maxDoubleLength);
            Match m = RegexDouble.Match(@this.Text.GetText(@this.StartIndex, (int)len));
            if( !m.Success ) return false;
            return @this.UncheckedMove( m.Length );
        }

        /// <summary>
        /// Matches a double and gets its value. No error is set if match fails.
        /// This uses <see cref="RegexDouble"/> with a string's length of 24.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="value">The read value on success.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool TryMatchDoubleValue( this VirtualStringMatcher @this, out double value )
        {
            long len = Math.Min(@this.Length, maxDoubleLength);
            Match m = RegexDouble.Match(@this.Text.GetText(@this.StartIndex, (int)len));
            if( !m.Success )
            {
                value = 0;
                return false;
            }
            if( !double.TryParse( @this.Text.GetText( @this.StartIndex, m.Length ), NumberStyles.Float, CultureInfo.InvariantCulture, out value ) ) return false;
            return @this.UncheckedMove( m.Length );
        }

        /// <summary>
        /// Matches a JSON terminal value: a "string", null, a number (double value), true or false.
        /// This method ignores the actual value and does not set any error if match fails.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <returns>True if a JSON value has been matched, false otherwise.</returns>
        public static bool TryMatchJSONTerminalValue( this VirtualStringMatcher @this )
        {
            return @this.TryMatchJSONQuotedString( true )
                    || @this.TryMatchDoubleValue()
                    || @this.TryMatchText( "true" )
                    || @this.TryMatchText( "false" );
        }

        /// <summary>
        /// Matches a JSON terminal value: a "string", null, a number (double value), true or false.
        /// No error is set if match fails.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="value">The parsed value. Can be null.</param>
        /// <returns>True if a JSON value has been matched, false otherwise.</returns>
        public static bool TryMatchJSONTerminalValue( this VirtualStringMatcher @this, out object value )
        {
            string s;
            if( @this.TryMatchJSONQuotedString( out s, true ) )
            {
                value = s;
                return true;
            }
            double d;
            if( @this.TryMatchDoubleValue( out d ) )
            {
                value = d;
                return true;
            }
            if( @this.TryMatchText( "true" ) )
            {
                value = true;
                return true;
            }
            if( @this.TryMatchText( "false" ) )
            {
                value = false;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Matches a very simple version of a JSON object content: this match stops at the first closing }.
        /// Whitespaces and JS comments (//... or /* ... */) are skipped.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="o">The read object on success as a list of KeyValuePair.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool MatchJSONObjectContent( this VirtualStringMatcher @this, out List<KeyValuePair<string, object>> o )
        {
            o = null;
            while( !@this.IsEnd )
            {
                @this.SkipWhiteSpacesAndJSComments();
                string propName;
                object value;
                if( @this.TryMatchChar( '}' ) )
                {
                    if( o == null ) o = new List<KeyValuePair<string, object>>();
                    return true;
                }
                if( !@this.TryMatchJSONQuotedString( out propName ) )
                {
                    o = null;
                    return @this.SetError( "Quoted Property Name." );
                }
                @this.SkipWhiteSpacesAndJSComments();
                if( !@this.MatchChar( ':' ) || !MatchJSONObject( @this, out value ) )
                {
                    o = null;
                    return false;
                }
                if( o == null ) o = new List<KeyValuePair<string, object>>();
                o.Add( new KeyValuePair<string, object>( propName, value ) );
                @this.SkipWhiteSpacesAndJSComments();
                // This accepts e trailing comma at the end of a property list: ..."a":0,} is not an error.
                @this.TryMatchChar( ',' );
            }
            return @this.SetError( "JSON object definition but reached end of match." );
        }

        /// <summary>
        /// Matches a { "JSON" : "object" }, a ["JSON", "array"] or a terminal value (string, null, double, true or false) 
        /// and any combination of them.
        /// Whitespaces and JS comments (//... or /* ... */) are skipped.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="value">
        /// A list of objects (for array), a list of KeyValuePair&lt;string,object&gt; for object or
        /// a double, string, boolean or null (for null).</param>
        /// <returns>True on success, false on error.</returns>
        public static bool MatchJSONObject( this VirtualStringMatcher @this, out object value )
        {
            value = null;
            @this.SkipWhiteSpacesAndJSComments();
            if( @this.TryMatchChar( '{' ) )
            {
                List<KeyValuePair<string, object>> c;
                if( !MatchJSONObjectContent( @this, out c ) ) return false;
                value = c;
                return true;
            }
            if( @this.TryMatchChar( '[' ) )
            {
                List<object> t;
                if( !MatchJSONArrayContent( @this, out t ) ) return false;
                value = t;
                return true;
            }
            if( TryMatchJSONTerminalValue( @this, out value ) ) return true;
            return @this.SetError( "JSON value." );
        }

        /// <summary>
        /// Matches a JSON array content: the match ends with the first ].
        /// Whitespaces and JS comments (//... or /* ... */) are skipped.
        /// </summary>
        /// <param name="this">This <see cref="VirtualStringMatcher"/>.</param>
        /// <param name="value">The list of objects on success.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool MatchJSONArrayContent( this VirtualStringMatcher @this, out List<object> value )
        {
            value = null;
            while( !@this.IsEnd )
            {
                @this.SkipWhiteSpacesAndJSComments();
                if( @this.TryMatchChar( ']' ) )
                {
                    if( value == null ) value = new List<object>();
                    return true;
                }
                object cell;
                if( !MatchJSONObject( @this, out cell ) ) return false;
                if( value == null ) value = new List<object>();
                value.Add( cell );
                @this.SkipWhiteSpacesAndJSComments();
                // Allow trailing comma: ,] is valid.
                @this.TryMatchChar( ',' );
            }
            return @this.SetError( "JSON array definition but reached end of match." );
        }
    }
}
