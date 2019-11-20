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

    public struct TransRoute
    {
        public TransRoute(FSMStateNode fromState, FSMStateNode toState)
        {
            FromState = fromState;
            ToState = toState;
            Route = new List<KeyValuePair<FSMStateNode, FSMStateNode>>();
        }
        public List<KeyValuePair<FSMStateNode, FSMStateNode>> Route;
        public FSMStateNode FromState;
        public FSMStateNode ToState;
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

        public static void ForEachFSMState(FSM fsm, Action<FSMStateNode> action)
        {
            ForEachFSMState(fsm.RootMachine, action);
        }

        public static void ForEachFSMState(FSMMachineNode machine, Action<FSMStateNode> action)
        {
            foreach (NodeBase child in machine.States)
            {
                action(child as FSMStateNode);
            }
            foreach (NodeBase child in machine.States)
            {
                if (child is FSMMetaStateNode)
                    ForEachFSMState((child as FSMMetaStateNode).SubMachine, action);
            }
        }

        ///> TODO: make it universal
        public static FSMMachineNode FindAncestor(FSMMachineNode a, FSMMachineNode b, ref FSMMachineNode toppestLevelChild)
        {
            if (a == null || b == null)
                return null;

            FSMMachineNode deeper;
            FSMMachineNode shallower;

            if (a.Level > b.Level)
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

            for (uint i = deeper.Level- shallower.Level; i > 0; --i)
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

        public static int SortByFSMNodeSortIndex(FSMStateNode aa, FSMStateNode bb)
        {
            return aa.SortIndex.CompareTo(bb.SortIndex);
        }

        public static TransRoute FindTransRoute(FSMStateNode fromState, FSMStateNode toState)
        {
            TransRoute route = new TransRoute(fromState, toState);
            List<KeyValuePair<FSMStateNode, FSMStateNode>> res = route.Route;

            if (toState == null)
            {
                LogMgr.Instance.Error("ToState is null");
            }

            ///> fromState == null ->  AnyState=>ToState
            else if (fromState == null)
            {
                fromState = toState.OwnerMachine.AnyState;
                res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(fromState, toState));
            }
            ///> In the same machine, simplest situation, nothing to do
            else if (fromState.OwnerMachine == toState.OwnerMachine)
            {
                res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(fromState, toState));
            }
            ///> Find their ancester
            else
            {
                FSMMachineNode toppestLevelChild = null;
                FSMMachineNode ancestorMachine = Utility.FindAncestor(fromState.OwnerMachine, toState.OwnerMachine, ref toppestLevelChild);

                ///> fromState is the parent of toState 
                ///>    ---> fromState=>toppestLevelChild
                ///>    ---> UpperState=>toState
                if (ancestorMachine == fromState.OwnerMachine)
                {
                    if (toppestLevelChild == null
                        || toppestLevelChild.MetaState == null
                        || toppestLevelChild.MetaState.OwnerMachine != fromState.OwnerMachine)
                    {
                        LogMgr.Instance.Error("Something error when find ancestor");
                    }
                    else
                    {
                        res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(fromState, toppestLevelChild.MetaState));
                        res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(toState.OwnerMachine.UpperState, toState));
                    }
                }
                ///> toState is the parent of fromState 
                ///>    ---> fromState=>UpperState
                ///>    ---> toppestLevelChild=>ToState
                else if (ancestorMachine == toState.OwnerMachine)
                {
                    if (toppestLevelChild == null
                        || toppestLevelChild.MetaState == null
                        || toppestLevelChild.MetaState.OwnerMachine != toState.OwnerMachine)
                    {
                        LogMgr.Instance.Error("Something error when find ancestor");
                    }
                    else
                    {
                        res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(fromState, fromState.OwnerMachine.UpperState));
                        res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(toppestLevelChild.MetaState, toState));
                    }
                }
                ///> XXState is the common ancestor of fromState and toState
                ///>    ---> fromState=>UpperState
                ///>    ---> UpperState=>UpperUpperState
                ///>    ---> (In AncestorMachine) Upper..UpperFromState=>Upper..UpperToState
                ///>    ---> UpperUpperState=>UpperState...
                ///>    ---> UpperState=>toState
                else
                {
                    while (fromState.OwnerMachine != ancestorMachine)
                    {
                        res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(fromState, fromState.OwnerMachine.UpperState));
                        fromState = fromState.OwnerMachine.MetaState;
                    }
                    while (toState.OwnerMachine != ancestorMachine)
                    {
                        res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(toState.OwnerMachine.UpperState, toState));
                        toState = toState.OwnerMachine.MetaState;
                    }
                    res.Add(new KeyValuePair<FSMStateNode, FSMStateNode>(fromState, toState));
                }
            }

            return route;
        }

    }
}
