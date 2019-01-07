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
    public enum NormalizedPathOption : byte
    {
        /// <summary>
        /// Relative path.
        /// </summary>
        None = 0,

        /// <summary>
        /// Marks a path that is rooted. A path that starts with a tilde (~) is rooted
        /// as well as a path whose first part ends with a colon (:).
        /// </summary>
        RootedByFirstPart = 1,

        /// <summary>
        /// When '/' or '\' starts the path, it is rooted.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separator, but
        /// the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// starts with it (normalized to <see cref="System.IO.Path.DirectorySeparatorChar"/>).
        /// </summary>
        RootedBySeparator = 2,

        /// <summary>
        /// When double separators ("//" or "\\") starts the path, it is rooted.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separators, but
        /// the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// starts with them (normalized to <see cref="System.IO.Path.DirectorySeparatorChar"/>).
        /// </summary>
        RootedByDoubleSeparator = 3
    }
}
