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
        public static readonly HashSet<string> ReservedAttributes = new HashSet<string>(new string[] { "Class", "Connection" });
        public static readonly HashSet<string> ReservedAttributesAll = new HashSet<string>(new string[] { "Class", "Pos", "NickName", "Connection", "DebugPoint", "Comment" });

        public static NodeBase CloneNode(NodeBase template, bool bIncludeChildren)
        {
            NodeBase node = template.Clone();

            if (bIncludeChildren)
            {
                foreach (Connector ctr in template.Conns.ConnectorsList)
                {
                    foreach (Connection conn in ctr.Conns)
                    {
                        NodeBase child = CloneNode(conn.Ctr.To.Owner, true);
                        node.Conns.Connect(child, conn.Ctr.From.Identifier);
                    }
                }
            }

            return node;
        }

        //public static void InitNode(Node node, bool bIncludeChildren)
        //{
        //    node.Init();

        //    if (bIncludeChildren)
        //    {
        //        foreach (Node child in node.Conns)
        //        {
        //            InitNode(child, bIncludeChildren);
        //        }
        //    }
        //}

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

        ///> TODO: make it universal
        public static FSMMachineNode FindAncestor(FSMMachineNode a, FSMMachineNode b, ref FSMMachineNode toppestLevelChild)
        {
            if (a == null || b == null)
                return null;

            FSMMachineNode deeper;
            FSMMachineNode shallower;

            if (FSM.UID.GetLevel(a.UID) > FSM.UID.GetLevel(b.UID))
            {
                deeper = a;
                shallower = b;
            }
            else
            {
                deeper = b;
                shallower = a;
            }

            FSMMachineNode c = deeper;
            FSMMachineNode d = shallower;

            for (uint i = FSM.UID.GetLevel(deeper.UID)- FSM.UID.GetLevel(shallower.UID); i > 0; --i)
            {
                toppestLevelChild = c;
                c = c.OwnerMachine;
            }

            while (c != d && c != null)
            {
                c = c.OwnerMachine;
                d = d.OwnerMachine;
            }

            return c;
        }

        public static bool IsSubClassOf(Type type, Type baseType)
        {
            var b = type.BaseType;
            while (b != null)
            {
                if (b.Equals(baseType))
                {
                    return true;
                }
                b = b.BaseType;
            }
            return false;
        }
    }
}
