using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class MessageProcessor : Singleton<MessageProcessor>
    {
        delegate void DebugTreeWithAgentAppendOneNode(Node node, StringBuilder stringBuilder);

        public void DebugTreeWithAgent(string treename, uint agentUID = 0)
        {
            WorkBench workBench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (workBench == null || workBench.MainTree == null)
            {
                LogMgr.Instance.Error("No Active Tree");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[DebugTreeWithAgent] ").Append(workBench.FileInfo.Name);
            sb.Append(" ").Append(agentUID);

            Action< Node, StringBuilder> appendOneNode = null;
            appendOneNode = delegate (Node node, StringBuilder stringBuilder) 
            {
                if (node.BreakPointInfo.HasBreakPoint)
                {
                    stringBuilder.Append(" ").Append(node.UID);
                }

                foreach (Node chi in node.Conns)
                {
                    appendOneNode(chi, stringBuilder);
                }
            };

            appendOneNode(workBench.MainTree, sb);

            NetworkMgr.Instance.SendText(sb.ToString());
        }
    }
}
