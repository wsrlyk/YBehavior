using System;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Log Management
    /// </summary>
    public class LogMgr : Singleton<LogMgr>
    {
        //public string LatestLog
        //{
        //    get
        //    {
        //        if (m_Count == 0)
        //            return string.Empty;
        //        return logList[CurIndex];
        //    }
        //}

        //public string LatestTwoLog
        //{
        //    get
        //    {
        //        if (m_Count == 0)
        //            return string.Empty;
        //        if (m_Count == 1)
        //            return logList[m_Head];

        //        return logList[PrevIndex] + '\n' + logList[CurIndex];
        //    }
        //}
        string[] logList = new string[LIST_SIZE];
        int m_Head = LIST_SIZE - 1;
        int m_Count = 0;
        static readonly int LIST_SIZE = 20;

        //int CurIndex
        //{
        //    get { return m_Head; }
        //}
        //int NextIndex
        //{
        //    get { return (m_Head + 1) % LIST_SIZE; }
        //}


        //int PrevIndex
        //{
        //    get { return (m_Head - 1 + LIST_SIZE) % LIST_SIZE; }
        //}

        /// <summary>
        /// Print log with line end
        /// </summary>
        /// <param name="content"></param>
        public void Log(string content)
        {
            Console.ResetColor();
            Console.WriteLine(content);
            /*
            m_Head = NextIndex;
            logList[m_Head] = content;

            if (m_Count < LIST_SIZE)
            {
                ++m_Count;
            }

            if (PropertyChanged != null)
            {
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LatestTwoLog"));
            }
            */
        }

        /// <summary>
        /// Print log using color
        /// </summary>
        /// <param name="content"></param>
        /// <param name="color"></param>
        public void LogWordWithColor(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(content);
        }

        /// <summary>
        /// Print log with line end using color
        /// </summary>
        /// <param name="content"></param>
        /// <param name="color"></param>
        public void LogLineWithColor(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(content);
        }

        /// <summary>
        /// Print line end
        /// </summary>
        public void LogEnd()
        {
            Console.WriteLine();
        }

        /// <summary>
        /// Print log with line end using RED color
        /// </summary>
        /// <param name="content"></param>
        public void Error(string content)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(content);
        }
    }
}
