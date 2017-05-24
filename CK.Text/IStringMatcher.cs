using System;
using System.Runtime.CompilerServices;

namespace CK.Text
{
    public interface IStringMatcher
    {
        string GetText( long index, int length );
        long StartIndex { get; }
        char Head { get; }
        long Length { get; }
        bool IsEnd { get; }
        bool Success { get; set; }
        bool ShouldStop { get; }
        bool IsError { get; }
        bool ShouldStopOnError { get; set; }
        string ErrorMessage { get; }
        bool SetError( object expectedMessage = null, [CallerMemberName]string callerName = null );
        bool AddError( object expectedMesage = null, bool beforeExisting = false, [CallerMemberName]string callerName = null );
        bool ClearError();
        bool BackwardAddError( long savedStartIndex, object expectedMessage = null, [CallerMemberName]string callerName = null );
        bool UncheckedMove( long delta );
        bool Forward( long charCount );
        bool MatchChar( char c );
        bool TryMatchChar( char c );
        bool MatchText( string text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase );
        bool TryMatchText( string text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase );
        bool MatchWhiteSpaces( int minCount = 1 );
    }
}
