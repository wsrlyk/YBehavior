using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class BreakPointInfo
    {
        public int HitCount { get; set; }

        public bool HasBreakPoint { get { return HitCount > 0; } }
    }

    public class DebugMgr : Singleton<DebugMgr>
    {
        string m_TargetTreeName;
        uint m_UID;

        public string TargetTreeName { get { return m_TargetTreeName; } }

        SharedData m_SharedData;
        public SharedData DebugSharedData { get { return m_SharedData; } }

        public bool IsDebugging(string treeName = null)
        {
            if (treeName == null)
            {
                treeName = WorkBenchMgr.Instance.ActiveTreeName;
            }

            if (NetworkMgr.Instance.IsConnected)
                return m_TargetTreeName == treeName;
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
                m_SharedData = new SharedData(null);
            }
        }

        public void StartDebugTreeWithAgent(string treeName, uint uid)
        {
            m_TargetTreeName = treeName;
            m_UID = uid;
            m_SharedData = WorkBenchMgr.Instance.ActiveWorkBench.MainTree.GetTreeSharedData().Clone();
            NetworkMgr.Instance.MessageProcessor.DebugTreeWithAgent(m_TargetTreeName, m_UID);

            DebugTargetChangedArg arg = new DebugTargetChangedArg
            {
                Treename = treeName
            };
            EventMgr.Instance.Send(arg);
        }


    }
}
