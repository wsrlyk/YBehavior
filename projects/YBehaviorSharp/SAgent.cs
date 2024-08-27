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
        /// <summary>
        /// Index in mgr
        /// </summary>
        int Index { get; set; }
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
        static extern IntPtr CreateEntity(ulong uid, int index);

        [DllImport(VERSION.dll)]
        static extern void DeleteEntity(IntPtr pEntity);

        [DllImport(VERSION.dll)]
        static extern IntPtr CreateAgent(IntPtr pEntity, int index);

        [DllImport(VERSION.dll)]
        static extern void DeleteAgent(IntPtr pAgent);
        /// <summary>
        /// Create an entity in cpp
        /// </summary>
        /// <param name="entity">Entity in csharp</param>
        public static void CreateEntity(IEntity entity)
        {
            if (entity != null && entity.Ptr == IntPtr.Zero)
            {
                SPtrMgr.Instance.Add(entity);
                entity.Ptr = CreateEntity(entity.UID, entity.Index);
            }
        }
        /// <summary>
        /// Destroy the entity in cpp
        /// </summary>
        /// <param name="entity">Entity in csharp</param>
        public static void DestroyEntity(IEntity entity)
        {
            if (entity != null && entity.Ptr != IntPtr.Zero)
            {
                SPtrMgr.Instance.Remove(entity);
                DeleteEntity(entity.Ptr);
                entity.Ptr = IntPtr.Zero;
            }
        }
        /// <summary>
        /// Create an agent in cpp
        /// </summary>
        /// <param name="agent">Agent in csharp</param>
        /// <param name="entity">Entity the agent belongs to</param>
        public static void CreateAgent(IAgent agent, IEntity entity)
        {
            if (agent != null && agent.Ptr == IntPtr.Zero && entity != null)
            {
                SPtrMgr.Instance.Add(agent);
                agent.Ptr = CreateAgent(entity.Ptr, agent.Index);
                agent.Entity = entity;
            }
        }
        /// <summary>
        /// Destroy the agent in cpp
        /// </summary>
        /// <param name="agent">Agent in csharp</param>
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
    /// <summary>
    /// Manage the mapping of agent/entity between cs and cpp
    /// </summary>
    public class SPtrMgr
    {
        public static SPtrMgr Instance { get; private set; } = new SPtrMgr();

        LinkedList<IPtr> m_List = new LinkedList<IPtr>();
        /// <summary>
        /// Add an agent/entity
        /// </summary>
        /// <param name="entity"></param>
        /// <summary>
        /// Add an agent/entity
        /// </summary>
        /// <param name="entity"></param>
        public void Add(IPtr entity)
        {
            if (entity == null)
                return;

            entity.Index = m_List.Add(entity);
        }
        /// <summary>
        /// Remove an agent/entity
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(IPtr entity)
        {
            if (entity == null)
                return;

            m_List.Remove(entity.Index);
            entity.Index = -1;
        }
        /// <summary>
        /// Find an agent/entity
        /// </summary>
        /// <param name="index">Index in mgr</param>
        /// <returns></returns>
        public IPtr? Get(int index)
        {
            if (m_List.TryGetValue(index, out IPtr entity))
                return entity;
            return null;
        }
    }

    internal class LinkedList<T>
    {
        List<T> m_Value = new List<T>();
        List<int> m_Next = new List<int>();

        int m_EmptyIndex = -1;

        public int Add(T item)
        {
            if (m_EmptyIndex < 0)
            {
                m_Value.Add(item);
                m_Next.Add(-2);
                return m_Value.Count - 1;
            }
            else
            {
                var idx = m_Next[m_EmptyIndex];
                var res = m_EmptyIndex;
                m_Value[m_EmptyIndex] = item;
                m_Next[m_EmptyIndex] = -2;
                m_EmptyIndex = idx;
                return res;
            }
        }

        public bool Remove(int index)
        {
            if (index < 0 || index >= m_Value.Count)
                return false;

            m_Next[index] = m_EmptyIndex;
            m_EmptyIndex = index;
            return true;
        }

        public bool TryGetValue(int index,  out T item)
        {
            if (index < 0 || index >= m_Value.Count || m_Next[index] != -2)
            {
                item = default;
                return false;
            }
            item = m_Value[index];
            return true;
        }
    }
}
