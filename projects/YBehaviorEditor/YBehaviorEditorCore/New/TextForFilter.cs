using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Get some string from a node for filtering the node from a list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct TextForFilterGetter<T> : IEnumerable<string> where T : IEnumerator<string>, INodeBaseConstructor, new()
    {
        public TextForFilterGetter(NodeBase node)
        {
            m_Node = node;
        }
        public IEnumerator<string> GetEnumerator()
        {
            T t = new T();
            t.Constructor(m_Node);
            return t;
        }
        private System.Collections.IEnumerator GetEnumerator1()
        {
            return this.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }
        NodeBase m_Node;
    }
    public interface INodeBaseConstructor
    {
        void Constructor(NodeBase node);
    }
    /// <summary>
    /// Just return the Nickname for filtering
    /// </summary>
    public struct BaseTextForFilter : IEnumerator<string>, INodeBaseConstructor
    {
        public string Current { get { return m_I == 0 ? m_Node.Name : m_Node.NickName; } }
        public bool MoveNext() { ++m_I; return m_I < 1 || !string.IsNullOrEmpty(m_Node.NickName); }
        public void Reset() { m_I = -1; }
        public void Dispose() { }

        object System.Collections.IEnumerator.Current => this.Current;

        public void Constructor(NodeBase node)
        {
            m_I = -1;
            m_Node = node;
        }

        int m_I;
        NodeBase m_Node;
    }

    /// <summary>
    /// Return the Name, Nickname, Description, etc. for filtering
    /// </summary>
    public struct TreeNodeTextForFilter : IEnumerator<string>, INodeBaseConstructor
    {
        public string Current
        {
            get
            {
                switch (m_I)
                {
                    case 0:
                        return m_Node.Name;
                    case 1:
                        return m_Node.NickName;
                    case 2:
                        return m_Node.Description;
                    case 3:
                        if (m_Enums != null && m_J >= 0 && m_J < m_Enums.Length) 
                            return m_Enums[m_J];
                        return m_Variables.Current.Variable.Name;

                }
                return m_I == 0 ? m_Node.Name : m_Node.NickName;
            }
        }
        public bool MoveNext()
        {
            ///> Name
            if (m_I < 0)
            {
                ++m_I;
                return true;
            }
            ///> NickName
            if (m_I < 1)
            {
                ++m_I;
                if (!string.IsNullOrEmpty(m_Node.NickName))
                    return true;
            }
            ///> Description
            if (m_I < 2)
            {
                ++m_I;
                if (!string.IsNullOrEmpty(m_Node.Description))
                    return true;
            }
            if (m_I < 3)
            {
                ++m_I;
                m_Variables = m_Node.Variables.Datas.GetEnumerator();
            }
            if (m_I == 3)
            {
                ///> Fetch each element in the enums
                if (m_Enums != null)
                {
                    ++m_J;
                    if (m_J < m_Enums.Length)
                        return true;
                    m_Enums = null;
                }

                if (m_Variables.MoveNext())
                {
                    m_Enums = m_Variables.Current.Variable.Enums;
                    if (m_Enums != null)
                        m_J = -2;
                    return true;
                }
            }

            return false;
        }
        public void Reset() { m_I = -1; m_J = -1; }
        public void Dispose() { }

        object System.Collections.IEnumerator.Current => this.Current;

        public void Constructor(NodeBase node)
        {
            m_I = -1;
            m_J = -1;
            m_Node = node as TreeNode;

            if (m_Node == null)
                throw new Exception("Not a TreeNode");

        }

        int m_I;
        int m_J;
        TreeNode m_Node;

        IEnumerator<VariableHolder> m_Variables;
        string[] m_Enums;
    }
}
