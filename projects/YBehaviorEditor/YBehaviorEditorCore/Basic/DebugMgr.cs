using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class DebugPointInfo
    {
        public int HitCount { get; set; }

        public bool HasBreakPoint { get { return HitCount > 0; } }
        public bool HasLogPoint { get { return HitCount < 0; } }
        public bool NoDebugPoint { get { return HitCount == 0; } }
    }

    public enum NodeState
    {
        NS_INVALID = -1,
        NS_SUCCESS,
        NS_FAILURE,
        NS_BREAK,
        NS_RUNNING,
    };

    public class TreeRunInfo
    {
        public Dictionary<uint, int> info = new Dictionary<uint, int>();
        public string treeName;
        public TreeMemory sharedData;

        public void Clear()
        {
            info.Clear();
            sharedData = null;
            treeName = string.Empty;
        }
    }

    public class FSMRunInfo
    {
        public Dictionary<uint, int> info = new Dictionary<uint, int>();
        public string fsmName;
        // TODO: trans?

        public void Clear()
        {
            info.Clear();
        }
    }

    public class DebugMgr : Singleton<DebugMgr>
    {
        TreeMemory m_EmptySharedData = new TreeMemory(null);

        public bool bBreaked { get; set; }

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
        public Dictionary<string, TreeRunInfo> GetRunInfos { get { return m_RunInfo; } }
        Dictionary<string, TreeRunInfo> m_RunInfo = new Dictionary<string, TreeRunInfo>();

        public FSMRunInfo FSMRunInfo { get; } = new FSMRunInfo();

        public NodeState GetRunState(uint uid)
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
                return NodeState.NS_INVALID;

            if (bench is TreeBench)
            {
                if (m_RunInfo.TryGetValue(bench.FileInfo.Name, out TreeRunInfo runInfo))
                {
                    if (runInfo.info.TryGetValue(uid, out int state))
                    {
                        return (NodeState)state;
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
        public void ClearRunState()
        {
            foreach (var info in m_RunInfo)
            {
                info.Value.info.Clear();
            }
            FSMRunInfo.info.Clear();
        }
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

        public void Clear()
        {
            ClearRunInfo();
        }

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

        public DebugMgr()
        {
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
        }

        private void _OnNetworkConnectionChanged(EventArg arg)
        {
            NetworkConnectionChangedArg oArg = arg as NetworkConnectionChangedArg;
            if (oArg.bConnected)
            {
                //m_SharedData = new SharedData(null);
            }
        }

        public void StartDebugTreeWithAgent(ulong uid)
        {
            TreeFileMgr.TreeFileInfo fileInfo = null;
            if (WorkBenchMgr.Instance.ActiveWorkBench != null)
            {
                fileInfo = WorkBenchMgr.Instance.ActiveWorkBench.FileInfo;
            }
            else
                fileInfo = null;

            //List<WorkBench> benches = WorkBenchMgr.Instance.OpenAllRelated();
            //BuildRunInfo(benches);
            NetworkMgr.Instance.MessageProcessor.DebugTreeWithAgent(fileInfo, uid);
        }

        public void Continue()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoContinue();
        }

        public void StepInto()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoStepInto();
        }

        public void StepOver()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoStepOver();
        }

        public void SetDebugPoint(uint uid, int count)
        {
            string treename = WorkBenchMgr.Instance.ActiveWorkBench.FileInfo.Name;
            NetworkMgr.Instance.MessageProcessor.SetDebugPoint(treename, uid, count);
        }

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
