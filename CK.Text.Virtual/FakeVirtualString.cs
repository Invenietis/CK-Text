using System;

namespace CK.Text.Virtual
{
    /// <summary>
    /// This class is used as a parameter for VirtualStringMatcher.
    /// It contains a real string to allow easier tests.
    /// <see cref="this[long]"/> can be used to read a character in the stream at a given position.
    /// </summary>
    public sealed class FakeVirtualString : IVirtualString
    {
        string _text;
        long _length;

        /// <summary>
        /// Initialises a new instance of the <see cref="FakeVirtualString"/> class on a non null string.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        public FakeVirtualString( string text )
        {
            if( text == null ) throw new ArgumentException( nameof( text ) );
            _text = text;
            _length = text.Length;
        }

        /// <summary>
        /// Gets the current length available.
        /// </summary>
        /// <value>The length.</value>
        public long Length => _length;

        /// <summary>
        /// Gets the current text available.
        /// </summary>
        /// <value>The text.</value>
        public string Text => _text;

        /// <summary>
        /// Gets the char value of <see cref="System.IO.Stream"/> at position <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to look at.</param>
        /// <returns>The char found.</returns>
        public char this[long index]
        {
            get
            {
                return _text[(int)index];
            }
        }

        /// <summary>
        /// Returns a substring from <see cref="Text"/>
        /// </summary>
        /// <param name="index">The index to start the string from.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>The string containing characters in the range.</returns>
        public string GetText( long index, int length )
        {
            return _text.Substring( (int)index, length );
        }
    }
}