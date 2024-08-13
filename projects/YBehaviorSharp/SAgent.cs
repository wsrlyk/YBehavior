using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YBehaviorSharp
{
    /// <summary>
    /// Interface of a unit in cpp
    /// </summary>
    public interface IPtr
    {
        /// <summary>
        /// Pointer in cpp
        /// </summary>
        IntPtr Ptr { get; set; }
    }
    /// <summary>
    /// Interface of agent.
    /// An agent can have behavior.
    /// </summary>
    public interface IAgent : IPtr
    {
        /// <summary>
        /// The entity the agent belongs to
        /// </summary>
        IEntity? Entity { get; set; }
    }
    /// <summary>
    /// Interface of entity.
    /// An entity has nothing to do with behavior
    /// </summary>
    public interface IEntity : IPtr
    {
        /// <summary>
        /// A Unique ID
        /// </summary>
        ulong UID { get; }
    }

    public partial class SharpHelper
    {
        [DllImport(VERSION.dll)]
        static extern IntPtr CreateEntity(ulong uid);

        [DllImport(VERSION.dll)]
        static extern void DeleteEntity(IntPtr pEntity);

        [DllImport(VERSION.dll)]
        static extern IntPtr CreateAgent(IntPtr pEntity);

        [DllImport(VERSION.dll)]
        static extern void DeleteAgent(IntPtr pAgent);

        public static void CreateEntity(IEntity entity)
        {
            if (entity != null && entity.Ptr == IntPtr.Zero)
            {
                entity.Ptr = CreateEntity(entity.UID);
                SPtrMgr.Instance.Add(entity);
            }
        }

        public static void DestroyEntity(IEntity entity)
        {
            if (entity != null && entity.Ptr != IntPtr.Zero)
            {
                SPtrMgr.Instance.Remove(entity);
                SharpHelper.DeleteEntity(entity.Ptr);
                entity.Ptr = IntPtr.Zero;
            }
        }

        public static void CreateAgent(IAgent agent, IEntity entity)
        {
            if (agent != null && agent.Ptr == IntPtr.Zero && entity != null)
            {
                agent.Ptr = CreateAgent(entity.Ptr);
                agent.Entity = entity;
                SPtrMgr.Instance.Add(agent);
            }
        }
        public static void DestroyAgent(IAgent agent)
        {
            if (agent != null && agent.Ptr != IntPtr.Zero)
            {
                SPtrMgr.Instance.Remove(agent);
                agent.Entity = null;
                DeleteAgent(agent.Ptr);
                agent.Ptr = IntPtr.Zero;
            }
        }

    }

    public class SPtrMgr
    {
        public static SPtrMgr Instance { get; private set; } = new SPtrMgr();

        Dictionary<IntPtr, IPtr> m_Dic = new Dictionary<IntPtr, IPtr>();

        public void Add(IPtr entity)
        {
            if (entity == null)
                return;

            m_Dic.Add(entity.Ptr, entity);
        }

        public void Remove(IPtr entity)
        {
            if (entity == null)
                return;

            m_Dic.Remove(entity.Ptr);
        }

        public IPtr? Get(IntPtr core)
        {
            if (core == IntPtr.Zero)
                return null;
            if (m_Dic.TryGetValue(core, out IPtr entity))
                return entity;
            return null;
        }
    }
}
