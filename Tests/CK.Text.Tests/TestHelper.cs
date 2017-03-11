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

#if NET451
    static class Does
    {
        public static SubstringConstraint Contain(string expected) => Is.StringContaining(expected);

        public static EndsWithConstraint EndWith(string expected) => Is.StringEnding(expected);

        public static StartsWithConstraint StartWith(string expected) => Is.StringStarting(expected);

        public static ConstraintExpression Not => Is.Not;

        public static SubstringConstraint Contain(this ConstraintExpression @this, string expected) => @this.StringContaining(expected);
    }
#endif

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
#if NET451
            string p = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            p = Path.GetDirectoryName(p);
#else
            string p = Directory.GetCurrentDirectory();
#endif
            while (!Directory.EnumerateFiles(p).Where(f => f.EndsWith(".sln")).Any())
            {
                p = Path.GetDirectoryName(p);
            }
            _solutionFolder = p;

            Console.WriteLine($"SolutionFolder is: {_solutionFolder}.");
            Console.WriteLine($"Core path: {typeof(string).GetTypeInfo().Assembly.CodeBase}.");
        }
    }
}
