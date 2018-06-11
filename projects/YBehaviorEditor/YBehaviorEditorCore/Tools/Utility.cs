using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class Lock : IDisposable
    {
        int m_LockCount = 0;
        public bool IsLocked { get { return m_LockCount > 0; } }
        public Lock StartLock()
        {
            ++m_LockCount;
            return this;
        }
        public void Dispose()
        {
            --m_LockCount;
        }
    }
    class Utility
    {
        public static Node CloneNode(Node template, bool bIncludeChildren)
        {
            Node node = template.Clone();

            if (bIncludeChildren)
            {
                foreach (Node templatechild in template.Conns)
                {
                    Node child = CloneNode(templatechild, true);
                    node.Conns.Connect(child, templatechild.ParentConn.Identifier);
                }
            }

            return node;
        }

        public static void InitNode(Node node, bool bIncludeChildren)
        {
            node.Init();

            if (bIncludeChildren)
            {
                foreach (Node child in node.Conns)
                {
                    InitNode(child, bIncludeChildren);
                }
            }
        }

        public static void OperateNode(Node node, bool bIncludeChildren, Action<Node> action)
        {
            action(node);

            if (bIncludeChildren)
            {
                foreach (Node child in node.Conns)
                {
                    OperateNode(child, bIncludeChildren, action);
                }
            }
        }
    }
}
