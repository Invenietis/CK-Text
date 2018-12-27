using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.Text.Tests
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

        static void InitalizePaths()
        {
            NormalizedPath path = AppContext.BaseDirectory;
            var s = path.PathsToFirstPart( null, new[] { "CK-Text.sln" } ).FirstOrDefault( p => File.Exists( p ) );
            if( s.IsEmpty ) throw new InvalidOperationException( $"Unable to find CK-Text.sln above '{AppContext.BaseDirectory}'." ); 
            _solutionFolder = s.RemoveLastPart();
            Console.WriteLine($"SolutionFolder is: {_solutionFolder}.");
            Console.WriteLine($"Core path: {typeof(string).GetTypeInfo().Assembly.CodeBase}.");
        }

    }
}
