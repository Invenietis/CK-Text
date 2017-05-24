using System;
using System.Runtime.CompilerServices;

namespace CK.Text
{
    /// <summary>
    /// This interface contains the definition for string matchers parameters. It contains text and matcher properties and some basic methods.
    /// </summary>
    public interface IStringMatcher
    {
        /// <summary>
        /// Returns a substring from the text in the given range.
        /// </summary>
        /// <param name="index">The index to start the string from.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns></returns>
        string GetText( long index, int length );

        /// <summary>
        /// Gets the current start index.
        /// </summary>
        /// <value>The current start index.</value>
        long StartIndex { get; }

        /// <summary>
        /// Gets the current head: the character at index <see cref="StartIndex"/>.
        /// </summary>
        /// <value>The head.</value>
        char Head { get; }

        /// <summary>
        /// Gets the current length available.
        /// </summary>
        /// <value>The length.</value>
        long Length { get; }

        /// <summary>
        /// Gets whether this matcher is at the end of the text to match.
        /// </summary>
        /// <value><c>true</c> on end; otherwise, <c>false</c>.</value>
        bool IsEnd { get; }

        /// <summary>
        /// Is used by <see cref="ShouldStop"/>. Setting this to true will set <see cref="ShouldStop"/> to true.
        /// </summary>
        /// <value><c>true</c> on success; otherwise, <c>false</c>.</value>
        bool Success { get; set; }

        /// <summary>
        /// Gets whether the matcher should stop.
        /// </summary>
        /// <value><c>true</c> on <see cref="Success"/> or on error if <see cref="ShouldStopOnError"/> is true; otherwise, <c>false</c>.</value>
        bool ShouldStop { get; }

        /// <summary>
        /// Gets whether an error has been set.
        /// You can call <see cref="ClearError"/> to clear the error.
        /// </summary>
        /// <value><c>true</c> on error; otherwise, <c>false</c>.</value>
        bool IsError { get; }

        /// <summary>
        /// Gets whether the matcher should stop on error.
        /// Setting this to true will set <see cref="ShouldStop"/> to true when <see cref="IsError"/> is true.
        /// </summary>
        /// <value><c>true</c> if we want to stop; otherwise, <c>false</c>.</value>
        bool ShouldStopOnError { get; set; }

        /// <summary>
        /// Gets the error message if any.
        /// You can call <see cref="ClearError"/> to clear the error.
        /// </summary>
        /// <value>The error message. Null when no error.</value>
        string ErrorMessage { get; }

        /// <summary>
        /// Sets an error and always returns false. The message starts with the caller's method name.
        /// Use <see cref="ClearError"/> to clear any existing error.
        /// </summary>
        /// <param name="expectedMessage">
        /// Optional object. Its <see cref="object.ToString()"/> will be used to generate an "expected '...'" message.
        /// </param>
        /// <param name="callerName">Name of the caller (automatically injected by the compiler).</param>
        /// <returns>Always false to use it as the return statement in a match method.</returns>
        bool SetError( object expectedMessage = null, [CallerMemberName]string callerName = null );

        /// <summary>
        /// Adds an error (the message starts with the caller's method name) to the existing ones (if any).
        /// </summary>
        /// <param name="expectedMessage">
        /// Optional object. Its <see cref="object.ToString()"/> will be used to generate an "expected '...'" message.
        /// </param>
        /// <param name="beforeExisting">
        /// True to add the error before the existing ones (as a consequence: [added] &lt;-- [previous]), 
        /// false to append it (as a cause: [previous] &lt;-- [added])</param>
        /// <param name="callerName">Name of the caller (automatically injected by the compiler).</param>
        /// <returns>Always false to use it as the return statement in a match method.</returns>
        bool AddError( object expectedMesage = null, bool beforeExisting = false, [CallerMemberName]string callerName = null );

        /// <summary>
        /// Clears any error and returns true. 
        /// </summary>
        /// <returns>Always true to use it as the return statement in a match method.</returns>
        bool ClearError();

        /// <summary>
        /// Moves back the head at a previously index and adds an error as a consequence of any previous errors. 
        /// The message starts with the caller's method name.
        /// </summary>
        /// <param name="savedStartIndex">Index to reset.</param>
        /// <param name="expectedMessage">
        /// Optional object. Its <see cref="object.ToString()"/> will be used to generate an "expected '...'" message.
        /// </param>
        /// <param name="callerName">Name of the caller (automatically injected by the compiler).</param>
        /// <returns>Always false to use it as the return statement in a match method.</returns>
        bool BackwardAddError( long savedStartIndex, object expectedMessage = null, [CallerMemberName]string callerName = null );

        /// <summary>
        /// Moves the head without any check and returns always true: typically called by 
        /// successful TryMatchXXX methods.
        /// Can be used to move the head at any position in the <see cref="Text"/> (or outside it since NO checks are made).
        /// </summary>
        /// <param name="delta">Number of characters.</param>
        /// <returns>Always <c>true</c>.</returns>
        bool UncheckedMove( long delta );

        /// <summary>
        /// Increments the <see cref="StartIndex"/> (and decrements <see cref="Length"/>) with the 
        /// specified character count and clears any existing error. Always returns true.
        /// </summary>
        /// <param name="charCount">The successfully matched character count. 
        /// Must be positive and should not move head past the end of the substring.</param>
        /// <returns>Always true to use it as the return statement in a match method.</returns>
        bool Forward( long charCount );

        /// <summary>
        /// Matches an exact single character. 
        /// If match fails, <see cref="SetError"/> is called.
        /// </summary>
        /// <param name="c">The character that must match.</param>
        /// <returns>True on success, false if the match failed.</returns>
        bool MatchChar( char c );

        /// <summary>
        /// Attempts to match an exact single character. 
        /// </summary>
        /// <param name="c">The character that must match.</param>
        /// <returns>True on success, false if the match failed.</returns>
        bool TryMatchChar( char c );

        /// <summary>
        /// Matches a text.
        /// </summary>
        /// <param name="text">The string that must match. Can not be null nor empty.</param>
        /// <param name="comparisonType">Specifies the culture, case, and sort rules.</param>
        /// <returns>True on success, false if the match failed.</returns>
        bool MatchText( string text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase );

        /// <summary>
        /// Matches a text without setting an error if match fails.
        /// </summary>
        /// <param name="text">The string that must match. Can not be null nor empty.</param>
        /// <param name="comparisonType">Specifies the culture, case, and sort rules.</param>
        /// <returns>True on success, false if the match failed.</returns>
        bool TryMatchText( string text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase );

        /// <summary>
        /// Matches a sequence of white spaces.
        /// Use <paramref name="minCount"/> = 0 to skip any white spaces.
        /// </summary>
        /// <param name="minCount">Minimal number of white spaces to match.</param>
        /// <returns>True on success, false if the match failed.</returns>
        bool MatchWhiteSpaces( int minCount = 1 );
    }
}
