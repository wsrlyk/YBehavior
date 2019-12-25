using System;

namespace YBehavior.Editor.Core.New
{
    public class LogMgr : Singleton<LogMgr>
    {
        public string LatestLog
        {
            get
            {
                if (m_Count == 0)
                    return string.Empty;
                return logList[CurIndex];
            }
        }

        public string LatestTwoLog
        {
            get
            {
                if (m_Count == 0)
                    return string.Empty;
                if (m_Count == 1)
                    return logList[m_Head];

                return logList[PrevIndex] + '\n' + logList[CurIndex];
            }
        }
        string[] logList = new string[LIST_SIZE];
        int m_Head = LIST_SIZE - 1;
        int m_Count = 0;
        static readonly int LIST_SIZE = 20;

        int CurIndex
        {
            get { return m_Head; }
        }
        int NextIndex
        {
            get { return (m_Head + 1) % LIST_SIZE; }
        }


        int PrevIndex
        {
            get { return (m_Head - 1 + LIST_SIZE) % LIST_SIZE; }
        }

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

        public void LogWordWithColor(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(content);
        }

        public void LogLineWithColor(string content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(content);
        }

        public void LogEnd()
        {
            Console.WriteLine();
        }

        public void Error(string content)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(content);
        }
    }
}
