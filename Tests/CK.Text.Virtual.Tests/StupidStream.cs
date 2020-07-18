using System;
using System.IO;

namespace CK.Text.Virtual.Tests
{
    partial class VirtualStringStreamTester
    {
        class StupidStream : Stream
        {
            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => Int64.MaxValue;

            public override long Position { get; set; }

            public override void Flush()
            {
            }

            public static char CharAt( long p ) => (char)At( p );

            static byte At( long p ) => (byte)((p % 26) + 'A');

            public override int Read( byte[] buffer, int offset, int count )
            {
                int c = count;
                while( --c >= 0 ) buffer[offset++] = At( Position++ );
                return count;
            }

            public override long Seek( long offset, SeekOrigin origin ) => origin switch
            {
                SeekOrigin.Current => Position = +offset,
                SeekOrigin.End => Position = Length - offset,
                _ => Position = offset
            };

            public override void SetLength( long value ) => throw new NotSupportedException();

            public override void Write( byte[] buffer, int offset, int count ) => throw new NotSupportedException();
        }


    }
}
