using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    class LogMgr
    {
        public static LogMgr Instance { get { return s_Instance; } }
        static LogMgr s_Instance = new LogMgr();

        public void Log(string content)
        {

        }

        public void Error(string content)
        {

        }
    }
}
