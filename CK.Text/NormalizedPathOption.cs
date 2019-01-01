using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Text
{
    /// <summary>
    /// 
    /// </summary>
    public enum NormalizedPathOption
    {
        None = 0,

        StartsWithSeparator,

        StartsWithDoubleSeparator,

        StartsWithTilde,

        StartsWithVolume,

        StartsWithScheme
    }
}
