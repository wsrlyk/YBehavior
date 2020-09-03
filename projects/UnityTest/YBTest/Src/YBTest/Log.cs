using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YBTest
{
    public abstract class Singleton<T> where T : new()
    {
        protected Singleton()
        {
        }

        protected static readonly T s_Instance = new T();

        public static T Instance
        {
            get
            {
                return s_Instance;
            }
        }
    }

    public class LogMgr : Singleton<LogMgr>, IEnumerable
    {
        public static readonly int MaxLogCount = 15;

        string[] Logs = new string[MaxLogCount];
        int Head = 0;
        int Tail = 0;

        public void Log(string s)
        {
            s = DateTime.Now.ToLongTimeString() + "   " + s;
            Monitor.Enter(Logs);
            if ((Tail + 1) % MaxLogCount == Head)
            {
                Head = (Head + 1) % MaxLogCount;
            }
            Logs[Tail] = s;
            Tail = (Tail + 1) % MaxLogCount;

            Monitor.Exit(Logs);
        }

        public IEnumerator GetEnumerator() { return new Iterator(Logs, Head, Tail); }

        public struct Iterator : IEnumerator<string>
        {
            public Iterator(string[] d, int start, int end)
            {
                head = start;
                tail = end;
                current = start - 1;
                data = d;
            }

            int head;
            int tail;
            int current;
            string[] data;
            public string Current
            {
                get { return data[current]; }
            }

            public bool MoveNext()
            {
                current = (current + 1) % data.Length;

                return current != tail;
            }

            public void Reset()
            {
                current = head - 1;
            }
            object IEnumerator.Current
            {
                get { return data[current]; }
            }
            public void Dispose()
            {
            }
        }
    }
}
