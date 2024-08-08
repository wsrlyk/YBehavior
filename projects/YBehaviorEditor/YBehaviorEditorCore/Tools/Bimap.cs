using System.Collections.Generic;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Two-direction map.
    /// A -> B, B -> A
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    public class Bimap<T0, T1> : System.Collections.IEnumerable
    {
        Dictionary<T0, T1> map0 = new Dictionary<T0, T1>();
        Dictionary<T1, T0> map1 = new Dictionary<T1, T0>();

        public System.Collections.IEnumerator GetEnumerator()
        {
            return map0.GetEnumerator();
        }

        public void Clear()
        {
            map0.Clear();
            map1.Clear();
        }

        public void Add(T0 t0, T1 t1)
		{
			RemoveKey(t0);
            RemoveValue(t1);
            map0[t0] = t1;
            map1[t1] = t0;
        }
        public bool RemoveKey(T0 t0)
        {
            T1 t1;
            if (TryGetValue(t0, out t1))
            {
                map0.Remove(t0);
                map1.Remove(t1);
                return true;
            }
            return false;
        }
        public bool RemoveValue(T1 t1)
        {
            T0 t0;
            if (TryGetKey(t1, out t0))
            {
                map0.Remove(t0);
                map1.Remove(t1);
                return true;
            }

            return false;
        }

        public bool TryGetValue(T0 t0, out T1 t1)
        {
            return map0.TryGetValue(t0, out t1);
        }
        public bool TryGetKey(T1 t1, out T0 t0)
        {
            return map1.TryGetValue(t1, out t0);
        }

        public T1 GetValue(T0 t0, T1 defaultRes)
        {
            if (map0.TryGetValue(t0, out T1 t1))
            {
                return t1;
            }
            return defaultRes;
        }

        public T0 GetKey(T1 t1, T0 defaultRes)
        {
            if (map1.TryGetValue(t1, out T0 t0))
            {
                return t0;
            }
            return defaultRes;
        }
    }
}

