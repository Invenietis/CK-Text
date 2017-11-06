using System;
using System.Globalization;

namespace CK.Text
{
    /// <summary>
    /// Extends <see cref="IStringMatcher"/> with useful (yet basic) methods.
    /// </summary>
    public static class IStringMatcherExtension
    {
        internal static int ReadHexDigit( char c )
        {
            int cN = c - '0';
            if( cN >= 49 ) cN -= 39;
            else if( cN >= 17 ) cN -= 7;
            return cN;
        }

        /// <summary>
        /// Matches hexadecimal values: between 1 and 16 0-9, A-F or a-f digits.
        /// </summary>
        /// <param name="this">This <see cref="IStringMatcher"/>.</param>
        /// <param name="value">Resulting value on success.</param>
        /// <param name="minDigit">Minimal digit count. Must be between 1 and 16 and smaller or equal to <paramref name="maxDigit"/>.</param>
        /// <param name="maxDigit">Maximal digit count. Must be between 1 and 16.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool TryMatchHexNumber( this IStringMatcher @this, out ulong value, int minDigit = 1, int maxDigit = 16 )
        {
            if( minDigit <= 0 ) throw new ArgumentException( "Must be at least 1 digit.", nameof( minDigit ) );
            if( maxDigit > 16 ) throw new ArgumentException( "Must be at most 16 digits.", nameof( maxDigit ) );
            if( minDigit > maxDigit ) throw new ArgumentException( "Must be smaller than maxDigit.", nameof( minDigit ) );
            value = 0;
            if( @this.IsEnd ) return false;
            long i = @this.StartIndex;
            long len = @this.Length;
            while( --maxDigit >= 0 && --len >= 0 )
            {
                int cN = ReadHexDigit( @this.Head );
                if( cN < 0 || cN > 15 ) break;
                @this.UncheckedMove( 1 );
                value <<= 4;
                value |= (uint)cN;
            }
            if( (@this.StartIndex - i) >= minDigit ) return true;
            @this.UncheckedMove( i - @this.StartIndex );
            return false;
        }

        /// <summary>
        /// Matches Int32 values that must not start with '0' ('0' is valid but '0d', where d is any digit, is not).
        /// A signed integer starts with a '-'. '-0' is valid but '-0d' (where d is any digit) is not.
        /// If the value is too big for an Int32, it fails.
        /// </summary>
        /// <param name="this">This <see cref="IStringMatcher"/>.</param>
        /// <param name="i">The result integer. 0 on failure.</param>
        /// <param name="minValue">Optional minimal value.</param>
        /// <param name="maxValue">Optional maximal value.</param>
        /// <returns><c>true</c> when matched, <c>false</c> otherwise.</returns>
        public static bool MatchInt32( this IStringMatcher @this, out int i, int minValue = int.MinValue, int maxValue = int.MaxValue )
        {
            i = 0;
            long savedIndex = @this.StartIndex;
            long value = 0;
            bool signed;
            if( @this.IsEnd ) return @this.SetError();
            if( (signed = @this.TryMatchChar( '-' )) && @this.IsEnd ) return @this.BackwardAddError( savedIndex );

            char c;
            if( @this.TryMatchChar( '0' ) )
            {
                if( !@this.IsEnd && (c = @this.Head) >= '0' && c <= '9' ) return @this.BackwardAddError( savedIndex, "0...9" );
                return @this.ClearError();
            }
            unchecked
            {
                long iMax = Int32.MaxValue;
                if( signed ) iMax = iMax + 1;
                while( !@this.IsEnd && (c = @this.Head) >= '0' && c <= '9' )
                {
                    value = value * 10 + (c - '0');
                    if( value > iMax ) break;
                    @this.UncheckedMove( 1 );
                }
            }
            if( @this.StartIndex > savedIndex )
            {
                if( signed ) value = -value;
                if( value < minValue || value > maxValue )
                {
                    return @this.BackwardAddError( savedIndex, String.Format( CultureInfo.InvariantCulture, "value between {0} and {1}", minValue, maxValue ) );
                }
                i = (int)value;
                return @this.ClearError();
            }
            return @this.SetError();
        }

        /// <summary>
        /// Tries to match a //.... or /* ... */ comment.
        /// Proper termination of comment (by a new line or the closing */) is not required: 
        /// a ending /*... is considered valid.
        /// </summary>
        /// <param name="this">This <see cref="IStringMatcher"/>.</param>
        /// <returns>True on success, false if the <see cref="IStringMatcher.Head"/> is not on a /.</returns>
        public static bool TryMatchJSComment( this IStringMatcher @this )
        {
            if( !@this.TryMatchChar( '/' ) ) return false;
            if( @this.TryMatchChar( '/' ) )
            {
                while( !@this.IsEnd && @this.Head != '\n' ) @this.UncheckedMove( 1 );
                if( !@this.IsEnd ) @this.UncheckedMove( 1 );
                return true;
            }
            else if( @this.TryMatchChar( '*' ) )
            {
                while( !@this.IsEnd )
                {
                    if( @this.Head == '*' )
                    {
                        @this.UncheckedMove( 1 );
                        if( @this.IsEnd || @this.TryMatchChar( '/' ) ) return true;
                    }
                    @this.UncheckedMove( 1 );
                }
                return true;
            }
            @this.UncheckedMove( -1 );
            return false;
        }

        /// <summary>
        /// Skips any white spaces or JS comments (//... or /* ... */) and always returns true.
        /// </summary>
        /// <param name="this">This <see cref="IStringMatcher"/>.</param>
        /// <returns>Always true to ease composition.</returns>
        public static bool SkipWhiteSpacesAndJSComments( this IStringMatcher @this )
        {
            @this.MatchWhiteSpaces( 0 );
            while( @this.TryMatchJSComment() ) @this.MatchWhiteSpaces( 0 );
            return true;
        }
    }
}
