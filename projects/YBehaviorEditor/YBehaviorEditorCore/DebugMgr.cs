using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
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

    public class RunInfo
    {
        public Dictionary<uint, int> info = new Dictionary<uint, int>();
        public string treeName;
        public SharedData sharedData;

        public void Clear()
        {
            info.Clear();
            sharedData = null;
            treeName = string.Empty;
        }
    }

    public class DebugMgr : Singleton<DebugMgr>
    {
        string m_TargetTreeName;

        uint m_UID;
        SharedData m_EmptySharedData = new SharedData(null);
        public string TargetTreeName { get { return m_TargetTreeName; } }

        public bool bBreaked { get; set; }

        public SharedData DebugSharedData
        {
            get
            {
                string treeName = WorkBenchMgr.Instance.ActiveTreeName;
                if (m_RunInfo.TryGetValue(treeName, out RunInfo runInfo))
                {
                    if (runInfo.sharedData != null)
                        return runInfo.sharedData;
                }
                return m_EmptySharedData;
            }
        }

        Dictionary<string, RunInfo> m_RunInfo = new Dictionary<string, RunInfo>();
        public NodeState GetRunState(uint uid)
        {
            string treeName = WorkBenchMgr.Instance.ActiveTreeName;
            if (m_RunInfo.TryGetValue(treeName, out RunInfo runInfo))
            {
                if (runInfo.info.TryGetValue(uid, out int state))
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
                ObjectPool<RunInfo>.Recycle(info.Value);
            }
            m_RunInfo.Clear();
        }
        public void ClearRunState()
        {
            foreach (var info in m_RunInfo)
            {
                info.Value.info.Clear();
            }
        }
        public RunInfo GetRunInfo(string treeName)
        {
            RunInfo runInfo;
            if (!m_RunInfo.TryGetValue(treeName, out runInfo))
            {
                runInfo = ObjectPool<RunInfo>.Get();
                runInfo.treeName = treeName;
                m_RunInfo[treeName] = runInfo;
            }
            return runInfo;
        }

        public void Clear()
        {
            m_TargetTreeName = string.Empty;
            m_UID = 0;
            ClearRunInfo();
        }

        public bool IsDebugging(string treeName = null)
        {
            if (treeName == null)
            {
                treeName = WorkBenchMgr.Instance.ActiveTreeName;
            }

            if (NetworkMgr.Instance.IsConnected)
                return m_RunInfo.ContainsKey(treeName);
            return false;
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

        public void StartDebugTreeWithAgent(uint uid)
        {
            m_TargetTreeName = WorkBenchMgr.Instance.ActiveWorkBench.FileInfo.Name;
            m_UID = uid;
            //m_SharedData = WorkBenchMgr.Instance.ActiveWorkBench.MainTree.GetTreeSharedData().Clone();

            List<WorkBench> benches = WorkBenchMgr.Instance.OpenAllRelated();
            BuildRunInfo(benches);
            NetworkMgr.Instance.MessageProcessor.DebugTreeWithAgent(m_TargetTreeName, m_UID, benches);

            DebugTargetChangedArg arg = new DebugTargetChangedArg
            {
                Treename = m_TargetTreeName
            };
            EventMgr.Instance.Send(arg);
        }

        public void Continue()
        {
            bBreaked = false;
            NetworkMgr.Instance.MessageProcessor.DoContinue();
        }

        public void SetDebugPoint(uint uid, int count)
        {
            string treename = WorkBenchMgr.Instance.ActiveWorkBench.FileInfo.Name;
            NetworkMgr.Instance.MessageProcessor.SetDebugPoint(treename, uid, count);
        }

        void BuildRunInfo(List<WorkBench> benches)
        {
            ClearRunInfo();
            foreach (WorkBench bench in benches)
            {
                RunInfo runInfo = GetRunInfo(bench.FileInfo.Name);
                runInfo.sharedData = bench.MainTree.SharedData.Clone();
            }
        }
    }
}
