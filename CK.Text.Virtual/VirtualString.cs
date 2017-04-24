using System;
using System.IO;
using System.Text;

namespace CK.Text
{
    /// <summary>
    /// This class is used as a parameter for VirtualStringMatcher.
    /// It contains a Stream and utility methods.
    /// <see cref="this[long]"/> can be used to read a character in the stream at a given position.
    /// </summary>
    public sealed class VirtualString : IVirtualString
    {
        readonly Stream _textStream;
        long _length;
        long _startIndex;
        byte[] _buffer;
        long _bufferPosition;
        int _bufferSize;
        Encoding _encoding;

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualString"/> class on a non null stream.
        /// </summary>
        /// <param name="textStream">The stream to parse. Must be Readable and Seekable.</param>
        /// <param name="startIndex">Index where the match must start in <paramref name="textStream"/>.</param>
        /// <param name="bufferSize">The size of the buffer used for matching.</param>
        /// <param name="encoding">The type of encoding used.</param>
        public VirtualString( Stream textStream, long startIndex = 0, int bufferSize = 256, Encoding encoding = null )
            : this( textStream, startIndex, textStream.Length - startIndex, bufferSize, encoding )
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VirtualString"/> class on a non null stream.
        /// </summary>
        /// <param name="textStream">The stream to parse. Must be Readable and Seekable.</param>
        /// <param name="startIndex">Index where the match must start in <paramref name="textStream"/>.</param>
        /// <param name="length">Number of character to consider.</param>
        /// <param name="bufferSize">The size of the buffer used for matching.</param>
        /// <param name="encoding">The type of encoding used.</param>
        public VirtualString( Stream textStream, long startIndex, long length, int bufferSize, Encoding encoding )
        {
            if( textStream == null ) throw new ArgumentNullException( nameof( textStream ) );
            if( !(textStream.CanSeek && textStream.CanRead) ) throw new ArgumentException( nameof( textStream ) );
            if( startIndex < 0 || startIndex > textStream.Length ) throw new ArgumentOutOfRangeException( nameof( startIndex ) );
            if( startIndex + length > textStream.Length ) throw new ArgumentException( nameof( length ) );
            if( bufferSize <= 0 ) throw new ArgumentException( nameof( bufferSize ) );
            _textStream = textStream;
            _length = length;
            _startIndex = startIndex;
            _bufferSize = bufferSize;
            _buffer = new byte[Math.Min( _bufferSize, _length )];
            _bufferPosition = _startIndex;
            _encoding = encoding ?? Encoding.UTF8;
            MoveBuffer( 0 );
        }

        /// <summary>
        /// Gets the current length available.
        /// </summary>
        /// <value>The length.</value>
        public long Length => _length;


        /// <summary>
        /// The size of the buffer used.
        /// </summary>
        /// <value>The size.</value>
        public int BufferSize => _bufferSize;

        /// <summary>
        /// Gets whether this index is currently buffered.
        /// You can move the buffer with <see cref="MoveBuffer(long)"/>.
        /// </summary>
        /// <param name="index">The index we are looking for.</param>   
        /// <returns><c>true</c> if the value is buffered. Otherwise <c>false</c>.</returns>
        bool IsBuffered( long index ) => index >= _bufferPosition && index < _bufferPosition + _bufferSize;

        /// <summary>
        /// Moves the buffer to a new position on the stream.
        /// The new buffer range will be from <paramref name="index"/> to <paramref name="index"/> + <see cref="BufferSize"/>
        /// </summary>
        /// <param name="index">The index where the buffer will be moved to.</param>
        void MoveBuffer( long index )
        {
            _textStream.Position = index;
            _textStream.Read( _buffer, 0, (int)Math.Min( _bufferSize, _length ) );
            _bufferPosition = index;
        }

        /// <summary>
        /// Gets the char value of <see cref="Stream"/> at position <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to look at.</param>
        /// <returns>The char found.</returns>
        public char this[long index]
        {
            get
            {
                if( index < 0 || index > Length ) throw new ArgumentOutOfRangeException( nameof( index ) );
                if( !IsBuffered( index ) ) MoveBuffer( index );
                return (char)_buffer[index - _bufferPosition];
            }
        }

        /// <summary>
        /// Generates and returns a string from <see cref="Stream"/> in the given range.
        /// </summary>
        /// <param name="index">The index to start the string from.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>The string containing characters in the range.</returns>
        public string GetText( long index, int length )
        {
            if( index < 0 || index > Length ) throw new ArgumentException( nameof( index ) );
            if( index + length > Length ) throw new ArgumentOutOfRangeException( nameof( length ) );
            string resultString = "";

            if( !IsBuffered( index ) || index + length > _bufferPosition + _bufferSize ) MoveBuffer( index );
            while( length > _bufferSize )
            {
                int delta = (int)(index - _bufferPosition);
                resultString += _encoding.GetString( _buffer ).Substring( delta );
                length -= (_bufferSize - delta);
                index += (_bufferSize - delta);
                MoveBuffer( index );
            }

            resultString += _encoding.GetString( _buffer ).Substring( (int)(index - _bufferPosition), length );

            return resultString;
        }
    }
}
