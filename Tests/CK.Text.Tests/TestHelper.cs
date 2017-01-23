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
            string p = Directory.GetCurrentDirectory();
            while (!Directory.EnumerateFiles(p).Where(f => f.EndsWith(".sln")).Any())
            {
                p = Path.GetDirectoryName(p);
            }
            _solutionFolder = p;
            Console.WriteLine($"SolutionFolder is: {_solutionFolder}");
        }
    }
}
