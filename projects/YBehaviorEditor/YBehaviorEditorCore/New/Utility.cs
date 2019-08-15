using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core.New
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
        public static NodeBase CloneNode(NodeBase template, bool bIncludeChildren)
        {
            NodeBase node = template.Clone();

            if (bIncludeChildren)
            {
                foreach (Connector ctr in template.Conns.ConnectorsList)
                {
                    foreach (Connection conn in ctr.Conns)
                    {
                        NodeBase child = CloneNode(conn.To.Owner, true);
                        node.Conns.Connect(child, conn.From.Identifier);
                    }
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

        public static void OperateNode(NodeBase node, bool bIncludeChildren, Action<NodeBase> action)
        {
            action(node);

            if (bIncludeChildren)
            {
                foreach (NodeBase child in node.Conns)
                {
                    OperateNode(child, bIncludeChildren, action);
                }
            }
        }

        public static void OperateNode(NodeBase node, object param, bool bIncludeChildren, Action<NodeBase, object> action)
        {
            action(node, param);

            if (bIncludeChildren)
            {
                foreach (NodeBase child in node.Conns)
                {
                    OperateNode(child, param, bIncludeChildren, action);
                }
            }
        }
    }
}
