using Cake.Core;
using Cake.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    public static class CakeEnvironmentExtension
    {
        class Reset : IDisposable
        {
            readonly ICakeEnvironment _e;
            readonly DirectoryPath _p;

            public Reset(ICakeEnvironment e, DirectoryPath p )
            {
                _e = e;
                _p = p;
            }

            public void Dispose()
            {
                _e.WorkingDirectory = _p;
            }
        }

        public static IDisposable SetWorkingDirectory(this ICakeEnvironment @this, string path)
        {
            return SetWorkingDirectory(@this, new DirectoryPath(path));
        }

        public static IDisposable SetWorkingDirectory(this ICakeEnvironment @this, DirectoryPath path)
        {
            var current = @this.WorkingDirectory;
            @this.WorkingDirectory = path;
            return new Reset(@this, current);
        }
    }
}
