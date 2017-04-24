using System.IO;
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
            _solutionFolder = Path.GetDirectoryName( Path.GetDirectoryName( GetProjectPath() ) );
        }

        static string GetProjectPath( [CallerFilePath]string path = null ) => Path.GetDirectoryName( path );
    }
}
