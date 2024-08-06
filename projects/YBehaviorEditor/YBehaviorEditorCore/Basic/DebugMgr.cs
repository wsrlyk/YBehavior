using System.Collections.Generic;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Debug point information of a node
    /// </summary>
    public class DebugPointInfo
    {
        /// <summary>
        /// A number to represent the debug point states. 
        /// 1 means break point, and -1 means log point.
        /// </summary>
        public int HitCount { get; set; }

        public bool HasBreakPoint { get { return HitCount > 0; } }
        public bool HasLogPoint { get { return HitCount < 0; } }
        public bool NoDebugPoint { get { return HitCount == 0; } }
    }

    /// <summary>
    /// Types of returned value of a node
    /// </summary>
    public enum NodeState
    {
        /// <summary>
        /// Invalid state
        /// </summary>
        NS_INVALID = -1,
        /// <summary>
        /// The node runs successfully
        /// </summary>
        NS_SUCCESS,
        /// <summary>
        /// The node runs failed
        /// </summary>
        NS_FAILURE,
        /// <summary>
        /// The node is paused by the break point
        /// </summary>
        NS_BREAK,
        /// <summary>
        /// The node is still running because of itself or its children
        /// </summary>
        NS_RUNNING,
    };

    /// <summary>
    /// Running information of a tree.
    /// This data is from the runtime through the network, and saved here for display.
    /// </summary>
    public class TreeRunInfo
    {
        /// <summary>
        /// Returned value of a node
        /// </summary>
        public struct ResultState
        {
            /// <summary>
            /// The raw value
            /// </summary>
            public int Self;
            /// <summary>
            /// The value modified by the ReturnType of the node
            /// </summary>
            public int Final;
        }
        /// <summary>
        /// The running information of all nodes. Key is node UID
        /// </summary>
        public Dictionary<uint, ResultState> info = new Dictionary<uint, ResultState>();
        public string treeName;
        /// <summary>
        /// Variables of the tree
        /// </summary>
        public TreeMemory sharedData;

        public void Clear()
        {
            info.Clear();
            sharedData = null;
            treeName = string.Empty;
        }
    }

    /// <summary>
    /// Running information of a fsm.
    /// This data is from the runtime through the network, and saved here for display.
    /// </summary>
    public class FSMRunInfo
    {
        /// <summary>
        /// The running information of all nodes. Key is node UID
        /// </summary>
        public Dictionary<uint, int> info = new Dictionary<uint, int>();
        public string fsmName;
        // TODO: trans?

        public void Clear()
        {
            info.Clear();
        }
    }

    /// <summary>
    /// Debug Management
    /// </summary>
    public class DebugMgr : Singleton<DebugMgr>
    {
        /// <summary>
        /// Just a default empty TreeMemory data
        /// </summary>
        TreeMemory m_EmptySharedData = new TreeMemory(null);

        /// <summary>
        /// Whether a break point is hit and paused
        /// </summary>
        public bool bBreaked { get; set; }

        /// <summary>
        /// Get the variables of the active tree from the RunIfo.
        /// If the active tree is not in debugging, return the empty one.
        /// </summary>
        public TreeMemory DebugSharedData
        {
            get
            {
                string treeName = WorkBenchMgr.Instance.ActiveTreeName;
                if (m_RunInfo.TryGetValue(treeName, out TreeRunInfo runInfo))
                {
                    if (runInfo.sharedData != null)
                        return runInfo.sharedData;
                }
                return m_EmptySharedData;
            }
        }
        /// <summary>
        /// Running information of all trees
        /// </summary>
        public Dictionary<string, TreeRunInfo> GetRunInfos { get { return m_RunInfo; } }
        Dictionary<string, TreeRunInfo> m_RunInfo = new Dictionary<string, TreeRunInfo>();

        /// <summary>
        /// Running information of the fsm
        /// </summary>
        public FSMRunInfo FSMRunInfo { get; } = new FSMRunInfo();

        /// <summary>
        /// Get the returned value of a node in the active tree/fsm
        /// </summary>
        /// <param name="uid">node uid</param>
        /// <param name="self">self or final result</param>
        /// <returns></returns>
        public NodeState GetRunState(uint uid, bool self)
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
                return NodeState.NS_INVALID;

            if (bench is TreeBench)
            {
                if (m_RunInfo.TryGetValue(bench.FileInfo.Name, out TreeRunInfo runInfo))
                {
                    if (runInfo.info.TryGetValue(uid, out var state))
                    {
                        return self ? (NodeState)state.Self : (NodeState)state.Final;
                    }
                }
            }
            else
            {
                if (FSMRunInfo.info.TryGetValue(uid, out int state))
                {
                    return (NodeState)state;
                }
            }
            return NodeState.NS_INVALID;
        }

        /// <summary>
        /// Get the returned value of a tree/fsm.
        /// If a node is break, return break. Or return the final result of the root
        /// </summary>
        /// <param name="bench"></param>
        /// <returns></returns>
        public NodeState GetRunState(WorkBench bench)
        {
            if (bench == null)
                return NodeState.NS_INVALID;
            if (bench is TreeBench)
            {
                if (m_RunInfo.TryGetValue(bench.FileInfo.Name, out TreeRunInfo runInfo))
                {
                    NodeState state = NodeState.NS_INVALID;
                    if (runInfo.info.TryGetValue(1, out var s))
                        state = (NodeState)s.Final;
                    if (state == NodeState.NS_RUNNING)
                    {
                        foreach (var ss in runInfo.info.Values)
                        {
                            NodeState detailState = (NodeState)ss.Final;
                            if (detailState == NodeState.NS_BREAK)
                                return NodeState.NS_BREAK;
                        }
                    }
                    return state;
                }
            }
            else
            {
                // just pick the first value
                foreach (var v in FSMRunInfo.info.Values)
                {
                    return (NodeState)v;
                }
            }
            return NodeState.NS_INVALID;
        }

        /// <summary>
        /// Clear all of the running information
        /// </summary>
        public void ClearRunInfo()
        {
            foreach (var info in m_RunInfo)
            {
                info.Value.Clear();
                ObjectPool<TreeRunInfo>.Recycle(info.Value);
            }
            m_RunInfo.Clear();
            FSMRunInfo.Clear();
        }
        /// <summary>
        /// Clear only the ResultState
        /// </summary>
        public void ClearRunState()
        {
            foreach (var info in m_RunInfo)
            {
                info.Value.info.Clear();
            }
            FSMRunInfo.info.Clear();
        }
        /// <summary>
        /// Get or create a tree RunInfo
        /// </summary>
        /// <param name="treeName"></param>
        /// <returns></returns>
        public TreeRunInfo GetRunInfo(string treeName)
        {
            TreeRunInfo runInfo;
            if (!m_RunInfo.TryGetValue(treeName, out runInfo))
            {
                runInfo = ObjectPool<TreeRunInfo>.Get();
                runInfo.treeName = treeName;
                m_RunInfo[treeName] = runInfo;
            }
            return runInfo;
        }

        /// <summary>
        /// Clear all of the running information
        /// </summary>
        public void Clear()
        {
            ClearRunInfo();
        }

        /// <summary>
        /// Whether a tree/fsm is in debugging
        /// </summary>
        /// <param name="bench"></param>
        /// <returns></returns>
        public bool IsDebugging(WorkBench bench = null)
        {
            if (!NetworkMgr.Instance.IsConnected)
                return false;
            if (bench == null)
            {
                bench = WorkBenchMgr.Instance.ActiveWorkBench;
            }

            if (bench == null)
                return false;

            if (bench is TreeBench)
                return m_RunInfo.ContainsKey(bench.FileInfo.Name);
            return FSMRunInfo.fsmName == bench.FileInfo.Name;
        }

        /// <summary>
        /// Whether it's debugging now
        /// </summary>
        /// <returns></returns>
        public bool IsSomeBenchDebugging()
        {
            if (!NetworkMgr.Instance.IsConnected)
                return false;
            return m_RunInfo.Count > 0 || FSMRunInfo.info.Count > 0;
        }
        //public DebugMgr()
        //{
        //    EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
        //}

        //private void _OnNetworkConnectionChanged(EventArg arg)
        //{
        //    NetworkConnectionChangedArg oArg = arg as NetworkConnectionChangedArg;
        //    if (oArg.bConnected)
        //    {
        //        //m_SharedData = new SharedData(null);
        //    }
        //}

        /// <summary>
        /// Tell the runtime to debug the active tree/fsm or specified entity
        /// </summary>
        /// <param name="uid">Entity uid. If it's valid, the runtime will only debug this entity. Or it will debug any entity that runs active tree/fsm</param>
        /// <param name="waitforbegin">Useful to debugging the initialization of an AI. Meaningless to other cases</param>
        public void StartDebugTreeWithAgent(ulong uid, bool waitforbegin)
        {
            FileMgr.FileInfo fileInfo = null;
            if (WorkBenchMgr.Instance.ActiveWorkBench != null)
            {
                fileInfo = WorkBenchMgr.Instance.ActiveWorkBench.FileInfo;
            }
            else
                fileInfo = null;

            //List<WorkBench> benches = WorkBenchMgr.Instance.OpenAllRelated();
            //BuildRunInfo(benches);
            NetworkMgr.Instance.MessageProcessor.DebugTreeWithAgent(fileInfo, uid, waitforbegin);
        }

        /// <summary>
        /// Tell the runtime to continue from the break
        /// </summary>
        public void Continue()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoContinue();
        }

        /// <summary>
        /// Tell the runtime to step into the children nodes
        /// </summary>
        public void StepInto()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoStepInto();
        }

        /// <summary>
        /// Tell the runtime to step over the children nodes
        /// </summary>
        public void StepOver()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoStepOver();
        }

        /// <summary>
        /// Change the debug point states of a node in active tree/fsm
        /// </summary>
        /// <param name="uid">node uid</param>
        /// <param name="count">DebugPointInfo.HitCount</param>
        public void SetDebugPoint(uint uid, int count)
        {
            NetworkMgr.Instance.MessageProcessor.SetDebugPoint(WorkBenchMgr.Instance.ActiveWorkBench.FileInfo, uid, count);
        }

        /// <summary>
        /// Create the RunInfo for a list of trees/fsms
        /// </summary>
        /// <param name="benches"></param>
        public void BuildRunInfo(List<WorkBench> benches)
        {
            ClearRunInfo();
            foreach (WorkBench bench in benches)
            {
                if (bench is TreeBench)
                {
                    TreeRunInfo runInfo = GetRunInfo(bench.FileInfo.Name);
                    runInfo.sharedData = (bench as TreeBench).Tree.TreeMemory.Clone() as TreeMemory;
                }
                else
                {
                    FSMRunInfo.fsmName = bench.FileInfo.Name;
                }
            }
        }
    }
}
