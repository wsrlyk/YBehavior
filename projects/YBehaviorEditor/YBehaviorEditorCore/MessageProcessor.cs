﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class MessageProcessor
    {
        object m_Lock = new object();
        Queue<string> m_ReceiveBuffer = new Queue<string>();
        List<string> m_ProcessingMsgs = new List<string>();

        System.Threading.Timer m_Timer;
        delegate void DebugTreeWithAgentAppendOneNode(Node node, StringBuilder stringBuilder);

        public void OnNetworkConnectionChanged(bool bConnected)
        {
            if (bConnected)
            {
                if (m_Timer != null)
                    m_Timer.Dispose();
                m_Timer = new System.Threading.Timer(_MessageTimerCallback, null, 0, 1000);
            }
            else
            {
                if (m_Timer != null)
                    m_Timer.Dispose();
                m_Timer = null;
            }
        }

        void _MessageTimerCallback(object state)
        {
            Update();
        }

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

        public void Update()
        {
            if (m_ReceiveBuffer.Count == 0)
                return;

            lock(m_Lock)
            {
                while (m_ReceiveBuffer.Count > 0)
                {
                    m_ProcessingMsgs.Add(m_ReceiveBuffer.Dequeue());
                }
            }

            for (int i = 0; i < m_ProcessingMsgs.Count; ++i)
            {
                _ProcessMsg(m_ProcessingMsgs[i]);
            }

            m_ProcessingMsgs.Clear();
        }

        void _ProcessMsg(string msg)
        {
            string[] words = msg.Split(' ');
            if (words.Length < 1)
                return;

            switch(words[0])
            {
                case "[TickResult]":
                    _HandleTickResult(words);
                    break;
            }
        }

        public void Receive(string msg)
        {
            lock(m_Lock)
            {
                m_ReceiveBuffer.Enqueue(msg);
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        void _HandleTickResult(string[] data)
        {
            if (data.Length > 1)
            {
                string[] sharedDatas = data[1].Split(';');
                foreach (string s in sharedDatas)
                {
                    string[] strV = s.Split(',');
                    if (strV.Length != 2)
                        continue;

                    Variable v = DebugMgr.Instance.DebugSharedData.GetVariable(strV[0]);
                    if (v == null)
                        continue;

                    v.Value = strV[1];
                }
            }
        }
    }
}
