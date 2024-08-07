﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// A collection that could add or remove elements in multiple frames using UnityCoroutines
    /// </summary>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="T"></typeparam>
    public class CoroutineCollection<C, T> where C : Collection<T>, new()
    {
        enum Op
        {
            Add,
            Remove,
        }
        struct ToDo
        {
            public Op op;
            public T data;
        }

        C m_Collection = new C();
        Queue<ToDo> m_Ops = new Queue<ToDo>();
        bool m_Oping = false;

        public int Step { get; set; } = 20;

        public C Collection { get { return m_Collection; } }

        /// <summary>
        /// If no operations in queue, directly add the element to the collection
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (m_Ops.Count == 0)
                m_Collection.Add(item);
            else
            {
                DelayAdd(item);
            }
        }

        /// <summary>
        /// Add the element to the queue
        /// </summary>
        /// <param name="item"></param>
        public void DelayAdd(T item)
        {
            ToDo toDo;
            toDo.op = Op.Add;
            toDo.data = item;
            m_Ops.Enqueue(toDo);
        }
        /// <summary>
        /// Clear all
        /// </summary>
        public void Clear()
        {
            ///> Just clear all.
            m_Ops.Clear();
            m_Collection.Clear();
        }

        /// <summary>
        /// If no operations in queue, directly remove the element from the collection
        /// </summary>
        /// <param name="item"></param>
        public bool Remove(T item)
        {
            if (m_Ops.Count == 0)
                return m_Collection.Remove(item);
            else
            {
                DelayRemove(item);
            }

            return true;
        }

        /// <summary>
        /// Add the request of removing element to the queue
        /// </summary>
        /// <param name="item"></param>
        public void DelayRemove(T item)
        {
            ToDo toDo;
            toDo.op = Op.Remove;
            toDo.data = item;
            m_Ops.Enqueue(toDo);
        }

        /// <summary>
        /// Start coroutine to slowly process the collection
        /// </summary>
        public void Dispose()
        {
            if (m_Ops.Count > 0 && !m_Oping)
            {
                if (m_Ops.Count < Step)
                    _ProcessOp();
                else
                {
                    m_Oping = true;
                    UnityCoroutines.CoroutineManager.Instance.StartCoroutine(_SlowProcessOp(Step));
                }
            }
        }

        void _ProcessOp()
        {
            while (m_Ops.Count > 0)
            {
                ToDo todo = m_Ops.Dequeue();
                if (todo.op == Op.Add)
                    m_Collection.Add(todo.data);
                else if (todo.op == Op.Remove)
                    m_Collection.Remove(todo.data);
            }
        }

        private System.Collections.IEnumerator _SlowProcessOp(int count)
        {
            int counter = count;
            while (m_Ops.Count > 0)
            {
                if (--counter < 0)
                {
                    counter = count;
                    yield return null;
                }

                ToDo todo = m_Ops.Dequeue();
                if (todo.op == Op.Add)
                    m_Collection.Add(todo.data);
                else if (todo.op == Op.Remove)
                    m_Collection.Remove(todo.data);
            }
            m_Oping = false;
        }

        static Queue<ToDo> s_Temp = new Queue<ToDo>();
        /// <summary>
        /// Stop the current process, clear the collection and add the elements to the collection from start
        /// </summary>
        public void ReAdd()
        {
            if (m_Collection.Count == 0)
                return;

            s_Temp.Clear();
            while(m_Ops.Count > 0)
            {
                s_Temp.Enqueue(m_Ops.Dequeue());
            }

            foreach(var v in m_Collection)
            {
                ToDo toDo;
                toDo.op = Op.Add;
                toDo.data = v;
                m_Ops.Enqueue(toDo);
            }
            m_Collection.Clear();
            while(s_Temp.Count > 0)
            {
                m_Ops.Enqueue(s_Temp.Dequeue());
            }

            Dispose();
        }
    }
}
