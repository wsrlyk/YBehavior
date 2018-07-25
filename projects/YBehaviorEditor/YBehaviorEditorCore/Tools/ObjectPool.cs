using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    class ObjectPool<T> where T: new()
    {
        static Stack<T> s_Pool = new Stack<T>();
        public static T Get()
        {
            if (s_Pool.Count == 0)
            {
                return new T();
            }

            return s_Pool.Pop();
        }

        public static void Recycle(T t)
        {
            s_Pool.Push(t);
        }
    }
}
