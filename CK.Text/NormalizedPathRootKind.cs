using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Text
{
    /// <summary>
    /// Characterizes the root of a <see cref="NormalizedPath"/>.
    /// </summary>
    public enum NormalizedPathRootKind : byte
    {
        /// <summary>
        /// Relative path.
        /// </summary>
        None = 0,

        /// <summary>
        /// Marks a path that is rooted because of its <see cref="NormalizedPath.FirstPart"/>.
        /// A path that starts with a tilde (~) is rooted as well as a path whose first part ends with a colon (:).
        /// </summary>
        RootedByFirstPart = 1,

        /// <summary>
        /// When '/' or '\' starts the path, it is rooted.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separator (there can even be
        /// no parts at all), but the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// starts with it (normalized to <see cref="NormalizedPath.DirectorySeparatorChar"/>).
        /// </summary>
        RootedBySeparator = 2,

        /// <summary>
        /// When double separators ("//" or "\\") starts the path, it is rooted.
        /// The <see cref="NormalizedPath.FirstPart"/> does not contain the separators (there can even be
        /// no parts at all), but the <see cref="NormalizedPath.Path"/> (and <see cref="NormalizedPath.ToString()"/>)
        /// starts with them (normalized to <see cref="NormalizedPath.DirectorySeparatorChar"/>).
        /// </summary>
        RootedByDoubleSeparator = 3
    }
}
