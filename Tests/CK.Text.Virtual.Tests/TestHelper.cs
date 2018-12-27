using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Text.Virtual.Tests
{
    static partial class TestHelper
    {
        static string _solutionFolder;

        static TestHelper()
        {
        }

        public static string SolutionFolder
        {
            get
            {
                if( _solutionFolder == null ) InitalizePaths();
                return _solutionFolder;
            }
        }

        public static string DataFolder => Path.Combine( SolutionFolder, "Tests", "CK.Text.Virtual.Tests", "Data" );

        static void InitalizePaths()
        {
            NormalizedPath path = AppContext.BaseDirectory;
            var s = path.PathsToFirstPart( null, new[] { "CK-Text.sln" } ).FirstOrDefault( p => File.Exists( p ) );
            if( s.IsEmpty ) throw new InvalidOperationException( $"Unable to find CK-Text.sln above '{AppContext.BaseDirectory}'." );
            _solutionFolder = s.RemoveLastPart();
        }
    }
}
