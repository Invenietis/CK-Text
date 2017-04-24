using System.Collections.Generic;
using System.Globalization;

namespace CK.Text
{
    /// <summary>
    /// JSON Visitor for big files
    /// </summary>
    public class JSONVirtualVisitor
    {
        readonly VirtualStringMatcher _m;
        readonly List<Parent> _path;

        /// <summary>
        /// Describes a parent object: it is the name of a property and its index or the index in a array.
        /// </summary>
        public struct Parent
        {
            /// <summary>
            /// The name of the property or null if this an array entry.
            /// </summary>
            public readonly string PropertyName;

            /// <summary>
            /// The index in the array or the property number (the count of properties
            /// that appear before this one in the object definition).
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// Gets whether this is an array cell (ie. <see cref="PropertyName"/> is null).
            /// </summary>
            public bool IsArrayCell => PropertyName == null;

            /// <summary>
            /// Initiliazes a new parent object.
            /// </summary>
            /// <param name="propertyName">Name of the property. Null for an array entry.</param>
            /// <param name="index">Index of the property or index in an array.</param>
            public Parent( string propertyName, int index )
            {
                PropertyName = propertyName;
                Index = index;
            }

            /// <summary>
            /// Overriden to retrun either <see cref="PropertyName"/> or [<see cref="Index"/>].
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return IsArrayCell
                    ? '[' + Index.ToString( CultureInfo.InvariantCulture ) + ']'
                    : PropertyName;
            }
        }

        /// <summary>
        /// Initialize a new <see cref="JSONVirtualVisitor"/> bound to a <see cref="Matcher"/>.
        /// </summary>
        /// <param name="m">The virtual string matcher.</param>
        public JSONVirtualVisitor( VirtualStringMatcher m )
        {
            _m = m;
            _path = new List<Parent>();
        }

        /// <summary>
        /// Gets the <see cref="VirtualStringMatcher"/> to which the visitor is bound.
        /// </summary>
        /// <value>The virtual string matcher.</value>
        public VirtualStringMatcher Matcher => _m;

        /// <summary>
        /// Gets the current path of the visited item.
        /// </summary>
        /// <value>The path.</value>
        protected IReadOnlyList<Parent> Path => _path;

        /// <summary>
        /// Visits any json item: it is either a terminal (<see cref="VisitTerminalValue()"/>), 
        /// {"an":"object"} (see <see cref="VisitObjectContent()"/> or ["an","array"] (see <see cref="VisitArrayContent()"/>).
        /// </summary>
        /// <returns>True on success. On error a message may be retrieved from the <see cref="Matcher"/>.</returns>
        public virtual bool Visit()
        {
            SkipWhiteSpaces();
            if( _m.TryMatchChar( '{' ) ) return VisitObjectContent();
            if( _m.TryMatchChar( '[' ) ) return VisitArrayContent();
            return VisitTerminalValue();
        }

        /// <summary>
        /// Visits a comma seprarated list of "property" : ... fields until a closing } is found
        /// or <see cref="Matcher"/>.<see cref="VirtualStringMatcher.IsEnd">IsEnd</see> becomes true.
        /// </summary>
        /// <returns>True on success. On error a message may be retrieved from the <see cref="Matcher"/>.</returns>
        public virtual bool VisitObjectContent()
        {
            int propertyNumber = 0;
            while( !_m.IsEnd )
            {
                SkipWhiteSpaces();
                if( _m.TryMatchChar( '}' ) ) return true;
                long startPropertyIndex = _m.StartIndex;
                string propName;
                if( !_m.TryMatchJSONQuotedString( out propName ) ) return false;
                SkipWhiteSpaces();
                if( !_m.MatchChar( ':' ) || !VisitObjectProperty( startPropertyIndex, propName, propertyNumber ) ) return false;
                SkipWhiteSpaces();
                _m.TryMatchChar( ',' );
                ++propertyNumber;
            }
            return false;
        }

        /// <summary>
        /// Visits a "property" : ... JSONVirtual property.
        /// </summary>
        /// <param name="startPropertyIndex">
        /// Starting index of the <paramref name="propertyName"/> in <see cref="Matcher"/>:
        /// this is the index of the opening quote ".
        /// </param>
        /// <param name="propertyName">Parsed property name.</param>
        /// <param name="propertyNumber">Zero based number of the property in the <see cref="Parent"/> object.</param>
        /// <returns>True on success. On error a message may be retrieved from the <see cref="Matcher"/>.</returns>
        protected virtual bool VisitObjectProperty( long startPropertyIndex, string propertyName, int propertyNumber )
        {
            try
            {
                _path.Add( new Parent( propertyName, propertyNumber ) );
                return Visit();
            }
            finally
            {
                _path.RemoveAt( _path.Count - 1 );
            }
        }

        /// <summary>
        /// Visits a comma seprarated list of json items until a closing ']' is found.
        /// </summary>
        /// <returns>True on success. On error a message may be retrieved from the <see cref="Matcher"/>.</returns>
        public virtual bool VisitArrayContent()
        {
            int cellNumber = 0;
            while( !_m.IsEnd )
            {
                SkipWhiteSpaces();
                if( _m.TryMatchChar( ']' ) ) return true;
                if( !VisitArrayCell( cellNumber ) ) return false;
                SkipWhiteSpaces();
                _m.TryMatchChar( ',' );
                ++cellNumber;
            }
            return false;
        }

        /// <summary>
        /// Visits a cell in a <see cref="Parent"/> array.
        /// </summary>
        /// <param name="cellNumber">Zero based cell nummber.</param>
        /// <returns>True on success. On error a message may be retrieved from the <see cref="Matcher"/>.</returns>
        protected virtual bool VisitArrayCell( int cellNumber )
        {
            try
            {
                _path.Add( new Parent( null, cellNumber ) );
                return Visit();
            }
            finally
            {
                _path.RemoveAt( _path.Count - 1 );
            }
        }

        /// <summary>
        /// Visits a terminal value. This method simply calls <see cref="VirtualStringMatcher.MatchWhiteSpaces(long)">Matcher.MatchWhiteSpaces(0)</see>
        /// to skip any whitespace and <see cref="VirtualStringMatcherTextExtension.TryMatchJSONTerminalValue(VirtualStringMatcher)">TryMatchJSONTerminalValue</see>
        /// to skip the value itself.
        /// </summary>
        /// <returns>True on success. On error a message may be retrieved from the <see cref="Matcher"/>.</returns>
        protected virtual bool VisitTerminalValue()
        {
            SkipWhiteSpaces();
            return _m.TryMatchJSONTerminalValue() || _m.SetError();
        }

        /// <summary>
        /// Skips white spaces: simply calls <see cref="VirtualStringMatcher.MatchWhiteSpaces(long)"/> 
        /// with 0 minimal count of spaces.
        /// </summary>
        protected virtual void SkipWhiteSpaces()
        {
            _m.MatchWhiteSpaces( 0 );
        }
    }
}
