﻿using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.Text
{
    /// <summary>
    /// This class supports "Match and Forward" pattern for a <see cref="IVirtualString"/>.
    /// On a failed match, the <see cref="SetError(object, string)"/> method sets the <see cref="ErrorMessage"/>.
    /// On a successful match, the <see cref="StartIndex"/> is updated by a call to <see cref="Forward(long)"/> so that 
    /// the <see cref="Head"/> is positioned after the match (and any existing error is cleared).
    /// There are 2 main kind of methods: TryMatchXXX that when the match fails returns false but do not call 
    /// <see cref="SetError(object, string)"/>and MatchXXX that do set an error on failure.
    /// This class does not actually hide/encapsulate a lot of things: it is designed to be extended through 
    /// extension methods.
    /// </summary>
    public sealed class VirtualStringMatcher
    {
        readonly IVirtualString _text;
        long _length;
        long _startIndex;
        string _errorDescription;

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStringMatcher"/> class on a non null string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        public VirtualStringMatcher( IVirtualString text, long startIndex = 0 )
            : this( text, startIndex, text.Length )
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualStringMatcher"/> class on a <see cref="IVirtualString"/>.
        /// </summary>
        /// <param name="text">The Virtual String to parse.</param>
        /// <param name="startIndex">Index where the match must start in the <paramref name="text"/>.</param>
        /// <param name="length">
        /// Number of character to consider in the string.
        /// If <paramref name="startIndex"/> + length is geater than the length of the string, an <see cref="ArgumentException"/> is thrown.
        /// </param>
        public VirtualStringMatcher( IVirtualString text, long startIndex, long length )
        {
            if( text == null ) throw new ArgumentNullException( nameof( text ) );
            if( _startIndex < 0 || _startIndex > text.Length ) throw new ArgumentException( nameof( text ) );
            _text = text;
            _length = length;
            _startIndex = startIndex;
        }

        /// <summary>
        /// Get the whole Virtual String.
        /// </summary>
        /// <value>The Virtual String.</value>
        public IVirtualString Text => _text;

        /// <summary>
        /// Gets the current start index: this is incremented by <see cref="Forward(long)"/>
        /// or <see cref="UncheckedMove(long)"/>.
        /// </summary>
        /// <value>The current start index.</value>
        public long StartIndex => _startIndex;

        /// <summary>
        /// Gets the current head: this is the character in <see cref="Text"/> at index <see cref="StartIndex"/>.
        /// </summary>
        /// <value>The head.</value>
        public char Head => _text[_startIndex];

        /// <summary>
        /// Gets the current length available.
        /// </summary>
        /// <value>The length.</value>
        public long Length => _length;

        /// <summary>
        /// Gets whether this matcher is at the end of the text to match.
        /// </summary>
        /// <value><c>true</c> on end; otherwise, <c>false</c>.</value>
        public bool IsEnd => _length <= 0;

        /// <summary>
        /// Gets whether an error has been been set.
        /// You can call <see cref="SetSuccess"/> to clear the error.
        /// </summary>
        /// <value><c>true</c> on error; otherwise, <c>false</c>.</value>
        public bool IsError => _errorDescription != null;

        /// <summary>
        /// Gets the error message if any.
        /// You can call <see cref="SetSuccess"/> to clear the error.
        /// </summary>
        /// <value>The message message. Null when no error.</value>
        public string ErrorMessage => _errorDescription;

        /// <summary>
        /// Sets and error and always returns false. The message starts with the caller's method name.
        /// Use <see cref="SetSuccess()"/> to clear any existing error.
        /// </summary>
        /// <param name="expectedMessage">
        /// Optional object. Its <see cref="object.ToString()"/> will be used to generate an "expected '...'" message.
        /// </param>
        /// <param name="callerName">Name of the caller (automatically injected by the compiler).</param>
        /// <returns>Always false to use it as the return statement in a match method.</returns>
        public bool SetError( object expectedMessage = null, [CallerMemberName]string callerName = null )
        {
            _errorDescription = FormatMessage( expectedMessage, callerName );
            return false;
        }

        /// <summary>
        /// Adds an error (the message starts with the caller's method name) to the existing one (if any).
        /// </summary>
        /// <param name="expectedMessage">
        /// Optional object. Its <see cref="object.ToString()"/> will be used to generate an "expected '...'" message.
        /// </param>
        /// <param name="beforeExisting">
        /// True to add the error before the existing ones (as a consequence: [added] &lt;-- [previous]),
        /// false to append it (as a cause: [previous] &lt;-- [added])
        /// </param>
        /// <param name="callerName">Name of the caller (automatically injected by the compiler).</param>
        /// <returns>Always false to use it as the return in a match method.</returns>
        public bool AddError( object expectedMessage = null, bool beforeExisting = false, [CallerMemberName]string callerName = null )
        {
            if( _errorDescription != null )
            {
                if( beforeExisting )
                {
                    _errorDescription = FormatMessage( expectedMessage, callerName ) + Environment.NewLine + "<-- " + _errorDescription;
                }
                else
                {
                    _errorDescription = _errorDescription + Environment.NewLine + "<-- " + FormatMessage( expectedMessage, callerName );
                }
            }
            else _errorDescription = FormatMessage( expectedMessage, callerName );
            return false;
        }

        static string FormatMessage( object expectedMessage, string callerName )
        {
            string d = callerName;
            string tail = expectedMessage?.ToString();
            if( !string.IsNullOrEmpty( tail ) ) d += ": expected '" + tail + "'.";
            return d;
        }

        /// <summary>
        /// Clears any error and returns true.
        /// </summary>
        /// <returns>Always true to use it as the return statement in a match method.</returns>
        public bool SetSuccess()
        {
            _errorDescription = null;
            return true;
        }

        /// <summary>
        /// Move back the head at a previously index and adds an error as a consequence of any previous errors.
        /// The message starts with the caller's method name.
        /// </summary>
        /// <param name="savedStartIndex">Index to reset.</param>
        /// <param name="expectedMessage">
        /// Optionnal object. Its <see cref="object.ToString()"/> will be used to generate an "expected '...'" message.
        /// </param>
        /// <param name="callerName">Name of the caller (automatically injected by the compiler).</param>
        /// <returns>Always false to use it as the return statement in a match method.</returns>
        public bool BackwardAddError( long savedStartIndex, object expectedMessage = null, [CallerMemberName]string callerName = null )
        {
            long delta = _startIndex - savedStartIndex;
            if( savedStartIndex < 0 || delta < 0 ) throw new ArgumentException( nameof( savedStartIndex ) );
            _length += delta;
            _startIndex = savedStartIndex;
            return AddError( expectedMessage, true, callerName );
        }

        /// <summary>
        /// Moves the head without any check and returns always true: typically called by
        /// successful TryMatchXXX methods.
        /// Can be used to move the head at any postion in the <see cref="Text"/> (or outside since NO checks are made).
        /// </summary>
        /// <param name="delta">Numbers of characters.</param>
        /// <returns>Always <c>true</c>.</returns>
        public bool UncheckedMove( long delta )
        {
            _startIndex += delta;
            _length -= delta;
            return true;
        }

        /// <summary>
        /// Increments the <see cref="StartIndex"/> (and decrements <see cref="Length"/>) with the 
        /// specified character count and clears any existing error. Always returns true.
        /// </summary>
        /// <param name="charCount">The successfully matched character count. 
        /// Must be positive and should not move head past the end of the substring.</param>
        /// <returns>Always true to use it as the return statement in a match method.</returns>
        public bool Forward( long charCount )
        {
            if( charCount < 0 ) throw new ArgumentException( nameof( charCount ) );
            long newLen = _length - charCount;
            if( newLen < 0 ) throw new InvalidOperationException( Impl.CoreResources.StringMatcherForwardPastEnd );
            _startIndex += charCount;
            _length = newLen;
            _errorDescription = null;
            return true;
        }

        /// <summary>
        /// Matches an exact single character.
        /// If a match fails, <see cref="SetError(object, string)"/> is called.
        /// </summary>
        /// <param name="c">The character that must match.</param>
        /// <returns>True on success, false if the match failed.</returns>
        public bool MatchChar( char c ) => TryMatchChar( c ) ? SetSuccess() : SetError( c );

        /// <summary>
        /// Attempts to match an exact single character.
        /// </summary>
        /// <param name="c">The character that must match.</param>
        /// <returns>True on sucess, false if the match failed.</returns>
        public bool TryMatchChar( char c ) => !IsEnd && Head == c ? UncheckedMove( 1 ) : false;

        /// <summary>
        /// Matches a text.
        /// </summary>
        /// <param name="text">The string that must match. Can not be null nor empty.</param>
        /// <param name="comparisonType">Specifies the culture, case, and sort rules.</param>
        /// <returns>True on success, false if the match failed.</returns>
        public bool MatchText( string text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase ) => TryMatchText( text ) ? SetSuccess() : SetError();

        /// <summary>
        /// Matches a text without setting an error if matches fails.
        /// </summary>
        /// <param name="text">The string that must match. Can not be null nor empty.</param>
        /// <param name="comparisonType">Specifies the culture, case, and sort rules.</param>
        /// <returns></returns>
        public bool TryMatchText( string text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase )
        {
            if( string.IsNullOrEmpty( text ) ) throw new ArgumentException( nameof( text ) );
            int len = text.Length;
            return !IsEnd
                    && len <= _length
                    && string.Compare( _text.GetText( _startIndex, len ), 0, text, 0, len, comparisonType ) == 0
                ? UncheckedMove( len )
                : false;
        }

        /// <summary>
        /// Matches a sequence of white spaces.
        /// Use <paramref name="minCount"/> = 0 to skip any white spaces.
        /// </summary>
        /// <param name="minCount">Minimal number of white spaces to match.</param>
        /// <returns>True on sucess, false if the match failed.</returns>
        public bool MatchWhiteSpaces( long minCount = 1 )
        {
            long i = _startIndex;
            long len = _length;
            while( len != 0 && char.IsWhiteSpace( _text[i] ) ) { ++i; --len; }
            if( i - _startIndex >= minCount )
            {
                _startIndex = i;
                _length = len;
                _errorDescription = null;
                return true;
            }
            return SetError( minCount + " whitespace(s)" );
        }

        /// <summary>
        /// Overriden to retrun adetailed string with <see cref="ErrorMessage"/> (if any),
        /// the <see cref="Head"/> character, <see cref="StartIndex"/> position and
        /// whole <see cref="Text"/> (Max character written for <see cref="Text"/> is 1024.
        /// </summary>
        /// <returns>Detailed string.</returns>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            if( _errorDescription != null ) b.Append( "Error: " ).Append( _errorDescription ).AppendLine();
            if( !IsEnd ) b.Append( "Head: " ).Append( Head ).Append( ", StartIndex: " ).Append( StartIndex ).AppendLine();
            b.Append( "Text: " ).Append( _text.GetText( 0, (int)(_text.Length > 1024 ? 1024 : _text.Length) ) );
            return b.ToString();
        }
    }
}
