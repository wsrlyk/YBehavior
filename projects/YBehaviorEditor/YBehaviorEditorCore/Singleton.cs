using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public abstract class Singleton<T> where T : new()
    {
        protected Singleton()
        {
            /* Here we added restriction in case a totally new Object of T be created outside.
             * It is not very delicate but works.
             * Also we can use reflection to get ctor. of T in a usual way, But
             * it's may cost more performance.
             */

            if (null != _instance)
            {
                //barely goes here...
                //Type type = _instance.GetType();
                throw new Exception(_instance.ToString() + @" can not be created again.");
            }
        }

        private static readonly T _instance = new T();

        public static T Instance
        {
            get
            {
                return _instance;
            }
        }

        public virtual bool Init() { return true; }
        public virtual void Uninit() { }
    }
}
