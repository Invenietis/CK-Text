using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Text
{
    /// <summary>
    /// Characerizes a <see cref="NormalizedPathOption"/>.
    /// </summary>
    public enum NormalizedPathOption
    {
        /// <summary>
        /// No specific prefix. The path is considered relative.
        /// </summary>
        None = 0,

        /// <summary>
        /// Any first part that starts with ~ is considered a root.
        /// </summary>
        StartsWithTilde,

        /// <summary>
        /// Any char followed by a colon like "X:" in the first part is considered as a volume.
        /// Its normalization is "X:" without any separator.
        /// </summary>
        StartsWithVolume,

        /// <summary>
        /// '/' or '\' that starts a path makes the path rooted.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separator, but
        /// the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// starts with it (normalized to <see cref="System.IO.Path.DirectorySeparatorChar"/>).
        /// </summary>
        StartsWithSeparator,

        /// <summary>
        /// Double separators ("//" or "\\") that starts a path makes the path rooted.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separators, but
        /// the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// starts with them (normalized to <see cref="System.IO.Path.DirectorySeparatorChar"/>).
        /// </summary>
        StartsWithDoubleSeparator,

        /// <summary>
        /// Any first part that ends with a colon ':' ans is longer that 2 characters
        /// is considered a root.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separators (only the "scheme:"),
        /// but the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// contains them (normalized to <see cref="System.IO.Path.DirectorySeparatorChar"/>): "scheme://".
        /// </summary>
        StartsWithScheme
    }
}
