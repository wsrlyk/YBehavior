using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class BreakPointInfo
    {
        public int HitCount { get; set; }

        public bool HasBreakPoint { get { return HitCount > 0; } }
    }

    public class DebugMgr : Singleton<DebugMgr>
    {
    }
}
