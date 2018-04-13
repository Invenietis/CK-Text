using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Text
{
    /// <summary>
    /// Immmutable encapsulation of a path that normalizes <see cref="System.IO.Path.AltDirectorySeparatorChar"/>
    /// to <see cref="System.IO.Path.DirectorySeparatorChar"/> and provides useful path manipulation methods.
    /// This struct is implicitely convertible to and from string.
    /// All comparisons uses <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </summary>
    public struct NormalizedPath : IEquatable<NormalizedPath>, IComparable<NormalizedPath>
    {
        static readonly char[] _separators = new[] { System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar };

        readonly string[] _parts;
        readonly string _path;

        /// <summary>
        /// Gets the <see cref="System.IO.Path.DirectorySeparatorChar"/> as a string.
        /// </summary>
        public static readonly string DirectorySeparatorString = new String( System.IO.Path.DirectorySeparatorChar, 1 );

        /// <summary>
        /// Gets the <see cref="System.IO.Path.AltDirectorySeparatorChar"/> as a string.
        /// </summary>
        public static readonly string AltDirectorySeparatorString = new String( System.IO.Path.AltDirectorySeparatorChar, 1 );

        /// <summary>
        /// Explicitely builds a new <see cref="NormalizedPath"/> struct from a string (that can be null or empty).
        /// </summary>
        /// <param name="path">The path as a string (can be null or empty).</param>
        public NormalizedPath( string path )
        {
            _parts = path?.Split( _separators, StringSplitOptions.RemoveEmptyEntries );
            if( _parts != null && _parts.Length == 0 ) _parts = null;
            _path = _parts?.Concatenate( DirectorySeparatorString );
        }

        /// <summary>
        /// Implicitely converts a path to a normalized string path.
        /// </summary>
        /// <param name="path">The normalized path.</param>
        public static implicit operator string( NormalizedPath path ) => path._path;

        /// <summary>
        /// Implicitely converts a string to a <see cref="NormalizedPath"/>.
        /// </summary>
        /// <param name="path">The path as a string.</param>
        public static implicit operator NormalizedPath( string path ) => new NormalizedPath( path );

        NormalizedPath( string[] parts, string path )
        {
            _parts = parts;
            _path = path;
        }

        /// <summary>
        /// Gets the parent list from this up to the <see cref="FirstPart"/>.
        /// </summary>
        public IEnumerable<NormalizedPath> Parents
        {
            get
            {
                var p = this;
                while( !p.IsEmpty )
                {
                    yield return p;
                    p = p.RemoveLastPart();
                }
            }
        }

        /// <summary>
        /// Enumerates paths from this one up to the <see cref="FirstPart"/> with <paramref name="subPaths"/>
        /// and <paramref name="lastParts"/> cross combined and appended in order.
        /// Each result ends with one of the <paramref name="lastParts"/>: if <paramref name="lastParts"/> is empty,
        /// this enumeration is empty.
        /// </summary>
        /// <param name="subPaths">The sub paths that will be combined in order. Can be null empty.</param>
        /// <param name="lastParts">
        /// The last parts that will be appended in order.
        /// Can not be null and should not be empty otherwise there will be no result at all.
        /// </param>
        /// <returns>
        /// All <see cref="Parents"/> with each <paramref name="subPaths"/> combined and
        /// each <paramref name="lastParts"/> appended.
        /// </returns>
        public IEnumerable<NormalizedPath> PathsToFirstPart( IEnumerable<NormalizedPath> subPaths, IEnumerable<string> lastParts )
        {
            if( lastParts == null ) throw new ArgumentNullException( nameof( lastParts ) );
            var p = this;
            if( subPaths != null && subPaths.Any() )
            {
                while( !p.IsEmpty )
                {
                    foreach( var sub in subPaths )
                    {
                        var pSub = p.Combine( sub );
                        foreach( var last in lastParts )
                        {
                            yield return String.IsNullOrEmpty( last ) ? pSub : pSub.AppendPart( last );
                        }
                    }
                    p = p.RemoveLastPart();
                }
            }
            else
            {
                while( !p.IsEmpty )
                {
                    foreach( var last in lastParts )
                    {
                        yield return String.IsNullOrEmpty( last ) ? p : p.AppendPart( last );
                    }
                    p = p.RemoveLastPart();
                }
            }
        }
        
        /// <summary>
        /// Returns a path where '.' and '..' parts are resolved under a root part.
        /// When <paramref name="throwOnAboveRoot"/> is true (the default), any '..' that would
        /// lead to a path above the root throws an <see cref="InvalidOperationException"/>.
        /// When false, the root acts as an absorbing element.
        /// </summary>
        /// <param name="rootPartsCount">
        /// By default, the resolution can reach the empty root.
        /// By specifying a positive number, any prefix length can be locked.
        /// Dotted parts in this locked prefix will be ignored and left as-is in the result.
        /// </param>
        /// <param name="throwOnAboveRoot">
        /// By default any attempt to resolve above the root will throw an <see cref="InvalidOperationException"/>.
        /// By specifying false, the root acts as an absorbing element.
        /// </param>
        /// <returns>The resolved normalized path.</returns>
        public NormalizedPath ResolveDots( int rootPartsCount = 0, bool throwOnAboveRoot = true )
        {
            int len = _parts != null ? _parts.Length : 0;
            if( rootPartsCount > len ) throw new ArgumentOutOfRangeException( nameof( rootPartsCount ) );
            if( rootPartsCount == len ) return this;
            Debug.Assert( !IsEmpty );
            string[] newParts = null;
            int current = 0;
            for( int i = rootPartsCount; i < len; ++i )
            {
                string curPart = _parts[i];
                bool isDot = curPart == ".";
                bool isDotDot = !isDot && curPart == "..";
                if( isDot || isDotDot )
                {
                    if( newParts == null )
                    {
                        newParts = new string[_parts.Length];
                        current = i;
                        if( isDotDot ) --current;
                        if( current < rootPartsCount )
                        {
                            if( throwOnAboveRoot ) ThrowAboveRootException( _parts, rootPartsCount, i );
                            current = rootPartsCount;
                        }
                        Array.Copy( _parts, 0, newParts, 0, current );
                    }
                    else if( isDotDot )
                    {
                        if( current == rootPartsCount )
                        {
                            if( throwOnAboveRoot ) ThrowAboveRootException( _parts, rootPartsCount, i );
                        }
                        else --current;
                    }
                }
                else if( newParts != null )
                {
                    newParts[current++] = curPart;
                }
            }
            if( newParts == null ) return this;
            if( current == 0 ) return new NormalizedPath();
            Array.Resize( ref newParts, current );
            return new NormalizedPath( newParts, String.Join( DirectorySeparatorString, newParts ) );
        }

        static void ThrowAboveRootException( string[] parts, int rootPartsCount, int iCulprit )
        {
            var msg = $"Path '{String.Join( DirectorySeparatorString, parts.Skip( iCulprit ) )}' must not resolve above root '{String.Join( DirectorySeparatorString, parts.Take( rootPartsCount ) )}'.";
            throw new InvalidOperationException( msg );
        }

        /// <summary>
        /// Appends the given path to this one and returns a new <see cref="NormalizedPath"/>.
        /// Note that relative parts (. and ..) are not resolved by this method.
        /// </summary>
        /// <param name="suffix">The path to append.</param>
        /// <returns>The resulting path.</returns>
        public NormalizedPath Combine( NormalizedPath suffix )
        {
            if( IsEmpty ) return suffix;
            if( suffix.IsEmpty ) return this;
            var parts = new string[_parts.Length + suffix._parts.Length];
            Array.Copy( _parts, parts, _parts.Length );
            Array.Copy( suffix._parts, 0, parts, _parts.Length, suffix._parts.Length );
            return new NormalizedPath( parts, _path + System.IO.Path.DirectorySeparatorChar + suffix._path );
        }

        /// <summary>
        /// Gets the last part of this path or the empty string if <see cref="IsEmpty"/> is true.
        /// </summary>
        public string LastPart => _parts?[_parts.Length - 1] ?? String.Empty;

        /// <summary>
        /// Gets the first part of this path or the empty string if <see cref="IsEmpty"/> is true.
        /// </summary>
        public string FirstPart => _parts?[0] ?? String.Empty;

        /// <summary>
        /// Appends a part that must not be null or empty nor contain <see cref="System.IO.Path.DirectorySeparatorChar"/>
        /// or <see cref="System.IO.Path.AltDirectorySeparatorChar"/> and returns a new <see cref="NormalizedPath"/>.
        /// </summary>
        /// <param name="part">The part to append. Must not be null or empty.</param>
        /// <returns>A new <see cref="NormalizedPath"/>.</returns>
        public NormalizedPath AppendPart( string part )
        {
            if( string.IsNullOrEmpty( part ) ) throw new ArgumentNullException( nameof( part ) );
            if( part.IndexOfAny( _separators ) >= 0 ) throw new ArgumentException( $"Illegal separators in '{part}'.", nameof( part ) );
            if( _parts == null ) return new NormalizedPath( new[] { part }, part );
            var parts = new string[_parts.Length + 1];
            Array.Copy( _parts, parts, _parts.Length );
            parts[_parts.Length] = part;
            return new NormalizedPath( parts, _path + System.IO.Path.DirectorySeparatorChar + part );
        }

        /// <summary>
        /// Returns a new <see cref="NormalizedPath"/> with <see cref="LastPart"/> removed (or more).
        /// Can be safely called when <see cref="IsEmpty"/> is true.
        /// </summary>
        /// <param name="count">Number of parts to remove.</param>
        /// <returns>A new path.</returns>
        public NormalizedPath RemoveLastPart( int count = 1 )
        {
            if( count <= 0 )
            {
                if( count == 0 ) return this;
                throw new ArgumentException();
            }
            if( _parts == null )
            {
                if( count == 0 ) return this;
                throw new ArgumentException();
            }
            if( count >= _parts.Length )
            {
                if( count == _parts.Length ) return new NormalizedPath();
                throw new ArgumentException();
            }
            var parts = new string[_parts.Length - count];
            Array.Copy( _parts, parts, parts.Length );
            int len = _parts[_parts.Length - 1].Length + count;
            while( --count > 0 ) len += _parts[_parts.Length - count - 2].Length;
            return new NormalizedPath( parts, _path.Substring( 0, _path.Length - len ) );
        }

        /// <summary>
        /// Returns a new <see cref="NormalizedPath"/> with <see cref="FirstPart"/> removed (or more).
        /// Can be safely called when <see cref="IsEmpty"/> is true.
        /// </summary>
        /// <returns>A new path.</returns>
        public NormalizedPath RemoveFirstPart( int count = 1 )
        {
            if( count <= 0 )
            {
                if( count == 0 ) return this;
                throw new ArgumentException();
            }
            if( _parts == null )
            {
                if( count == 0 ) return this;
                throw new ArgumentException();
            }
            if( count >= _parts.Length )
            {
                if( count == _parts.Length ) return new NormalizedPath();
                throw new ArgumentException();
            }
            var parts = new string[_parts.Length - count];
            Array.Copy( _parts, count, parts, 0, parts.Length );
            int len = _parts[0].Length + count;
            while( --count > 0 ) len += _parts[count + 1].Length;
            return new NormalizedPath( parts, _path.Substring( len ) );
        }

        /// <summary>
        /// Removes one of the <see cref="Parts"/> and returns a new <see cref="NormalizedPath"/>.
        /// The <paramref name="index"/> must be valid otherwise a <see cref="IndexOutOfRangeException"/> will be thrown.
        /// </summary>
        /// <param name="index">Index of the part to remove.</param>
        /// <returns>A new path.</returns>
        public NormalizedPath RemovePart( int index ) => RemoveParts( index, 1 );

        /// <summary>
        /// Removes some of the <see cref="Parts"/> and returns a new <see cref="NormalizedPath"/>.
        /// The <paramref name="startIndex"/> and <paramref name="count"/> must be valid
        /// otherwise a <see cref="IndexOutOfRangeException"/> will be thrown.
        /// </summary>
        /// <param name="startIndex">Starting index to remove.</param>
        /// <param name="count">Number of parts to remove (can be 0).</param>
        /// <returns>A new path.</returns>
        public NormalizedPath RemoveParts( int startIndex, int count )
        {
            int to = startIndex + count;
            if( _parts == null || startIndex < 0 || startIndex >= _parts.Length || to > _parts.Length ) throw new IndexOutOfRangeException();
            if( count == 0 ) return this;
            int nb = _parts.Length - count;
            if( nb == 0 ) return new NormalizedPath();
            var parts = new string[nb];
            Array.Copy( _parts, parts, startIndex );
            int sIdx = startIndex, sLen = count;
            int tailCount = _parts.Length - to;
            if( tailCount != 0 ) Array.Copy( _parts, to, parts, startIndex, tailCount );
            else --sIdx;
            int i = 0;
            for( ; i < startIndex; ++i ) sIdx += _parts[i].Length;
            for( ; i < to; ++i ) sLen += _parts[i].Length;
            return new NormalizedPath( parts, _path.Remove( sIdx, sLen ) );
        }

        /// <summary>
        /// Tests whether this <see cref="NormalizedPath"/> starts with another one.
        /// </summary>
        /// <param name="other">The path that may be a prefix of this path.</param>
        /// <param name="strict">
        /// False to allow the other path to be the same as this one.
        /// By default this path must be longer than the other one.</param>
        /// <returns>True if this path starts with the other one.</returns>
        public bool StartsWith( NormalizedPath other, bool strict = true ) => (other.IsEmpty && !strict)
                                                        || (!other.IsEmpty
                                                            && !IsEmpty
                                                            && other._parts.Length <= _parts.Length
                                                            && (!strict || other._parts.Length < _parts.Length)
                                                            && StringComparer.OrdinalIgnoreCase.Equals( other.LastPart, _parts[other._parts.Length - 1] )
                                                            && _path.StartsWith( other._path, StringComparison.OrdinalIgnoreCase ));

        /// <summary>
        /// Tests whether this <see cref="NormalizedPath"/> ends with another one.
        /// </summary>
        /// <param name="other">The path that may be a prefix of this path.</param>
        /// <param name="strict">
        /// False to allow the other path to be the same as this one.
        /// By default this path must be longer than the other one.</param>
        /// <returns>True if this path ends with the other one.</returns>
        public bool EndsWith( NormalizedPath other, bool strict = true ) => (other.IsEmpty && !strict)
                                                        || (!other.IsEmpty
                                                            && !IsEmpty
                                                            && other._parts.Length <= _parts.Length
                                                            && (!strict || other._parts.Length < _parts.Length)
                                                            && StringComparer.OrdinalIgnoreCase.Equals( other.FirstPart, _parts[_parts.Length - other._parts.Length] )
                                                            && _path.EndsWith( other._path, StringComparison.OrdinalIgnoreCase ));

        /// <summary>
        /// Removes the prefix from this path. The prefix must starts with or be exaclty the same as this one
        /// otherwise an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        /// <param name="prefix">The prefix to remove.</param>
        /// <returns>A new path.</returns>
        public NormalizedPath RemovePrefix( NormalizedPath prefix )
        {
            if( !StartsWith( prefix, false ) ) throw new ArgumentException( $"'{prefix}' is not a prefix of '{_path}'." );
            int nb = _parts.Length - prefix._parts.Length;
            if( nb == 0 ) return new NormalizedPath();
            var parts = new string[nb];
            Array.Copy( _parts, prefix._parts.Length, parts, 0, nb );
            return new NormalizedPath( parts, _path.Substring( prefix._path.Length + 1 ) );
        }

        /// <summary>
        /// Gets whether this is an empty path. A new <see cref="NormalizedPath"/>() (default constructor),
        /// or <c>default(NormalizedPath)</c> are empty.
        /// </summary>
        public bool IsEmpty => _parts == null;

        /// <summary>
        /// Gets the parts that compose this <see cref="NormalizedPath"/>.
        /// </summary>
        public IReadOnlyList<string> Parts => _parts ?? Array.Empty<string>();

        /// <summary>
        /// Gets this path as a normalized string.
        /// </summary>
        public string Path => _path ?? String.Empty;

        /// <summary>
        /// Compares this path to another one.
        /// The <see cref="Parts"/> length is considered first and if they are equal, the
        /// two <see cref="Path"/> are compared using <see cref="StringComparer.OrdinalIgnoreCase"/>.
        /// </summary>
        /// <param name="other">The path to compare to.</param>
        /// <returns>A positive integer if this is greater than other, a negative integer if this is lower than the other one and 0 if they are equal.</returns>
        public int CompareTo( NormalizedPath other )
        {
            if( _parts == null ) return other._parts == null ? 0 : -1;
            if( other._parts == null ) return 1;
            int cmp = _parts.Length - other._parts.Length;
            return cmp != 0 ? cmp : StringComparer.OrdinalIgnoreCase.Compare( _path, other._path );
        }

        /// <summary>
        /// Equality operator calls <see cref="Equals(NormalizedPath)"/>.
        /// </summary>
        /// <param name="p1">First path.</param>
        /// <param name="p2">Second path.</param>
        /// <returns>True if the two paths are equal.</returns>
        public static bool operator ==( NormalizedPath p1, NormalizedPath p2 ) => p1.Equals( p2 );

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="p1">First path.</param>
        /// <param name="p2">Second path.</param>
        /// <returns>True if the two paths are not equal.</returns>
        public static bool operator !=( NormalizedPath p1, NormalizedPath p2 ) => !p1.Equals( p2 );

        /// <summary>
        /// Comparison operator calls <see cref="CompareTo(NormalizedPath)"/>.
        /// </summary>
        /// <param name="p1">First path.</param>
        /// <param name="p2">Second path.</param>
        /// <returns>True p1 is greater than p2.</returns>
        public static bool operator >( NormalizedPath p1, NormalizedPath p2 ) => p1.CompareTo( p2 ) > 0;

        /// <summary>
        /// Comparison operator calls <see cref="CompareTo(NormalizedPath)"/>.
        /// </summary>
        /// <param name="p1">First path.</param>
        /// <param name="p2">Second path.</param>
        /// <returns>True if p1 is smaller than p2.</returns>
        public static bool operator <( NormalizedPath p1, NormalizedPath p2 ) => p1.CompareTo( p2 ) < 0;

        /// <summary>
        /// Comparison operator calls <see cref="CompareTo(NormalizedPath)"/>.
        /// </summary>
        /// <param name="p1">First path.</param>
        /// <param name="p2">Second path.</param>
        /// <returns>True if p1 is greater than or equal to p2.</returns>
        public static bool operator >=( NormalizedPath p1, NormalizedPath p2 ) => p1.CompareTo( p2 ) >= 0;

        /// <summary>
        /// Comparison operator calls <see cref="CompareTo(NormalizedPath)"/>.
        /// </summary>
        /// <param name="p1">First path.</param>
        /// <param name="p2">Second path.</param>
        /// <returns>True if p1 is less than or equal to p2.</returns>
        public static bool operator <=( NormalizedPath p1, NormalizedPath p2 ) => p1.CompareTo( p2 ) <= 0;


        /// <summary>
        /// Gets whether the <paramref name="obj"/> is a <see cref="NormalizedPath"/> that is equal to
        /// this one.
        /// Comparison is done by <see cref="StringComparer.OrdinalIgnoreCase"/>.
        /// </summary>
        /// <param name="obj">The object to challenge.</param>
        /// <returns>True if they are equal, false otherwise.</returns>
        public override bool Equals( object obj ) => obj is NormalizedPath p && Equals( p );

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode( ToString() );

        /// <summary>
        /// Gets whether the other path is equal to this one.
        /// Comparison is done by <see cref="StringComparer.OrdinalIgnoreCase"/>.
        /// </summary>
        /// <param name="other">The other path to challenge.</param>
        /// <returns>True if they are equal, false otherwise.</returns>
        public bool Equals( NormalizedPath other )
        {
            if( _parts == null ) return other._parts == null;
            if( other._parts == null || _parts.Length != other._parts.Length ) return false;
            return StringComparer.OrdinalIgnoreCase.Equals( _path, other._path );
        }

        /// <summary>
        /// Returns the string <see cref="Path"/>.
        /// </summary>
        /// <returns>The path as a string.</returns>
        public override string ToString() => _path ?? String.Empty;

        /// <summary>
        /// Returns a path with a specific character as the path separator instead of <see cref="System.IO.Path.DirectorySeparatorChar"/>.
        /// </summary>
        /// <param name="separator">The separator to use.</param>
        /// <returns>The path with the separator.</returns>
        public string ToString( char separator )
        {
            if( _path == null ) return String.Empty;
            if( separator == System.IO.Path.DirectorySeparatorChar || _parts.Length == 1 )
            {
                return _path;
            }
            return _path.Replace( System.IO.Path.DirectorySeparatorChar, separator );
        }
    }
}
