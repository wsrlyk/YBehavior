using System;
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
                Clear();
            }
            else
            {
                if (m_Timer != null)
                    m_Timer.Dispose();
                m_Timer = null;
                Clear();
            }
        }

        void _MessageTimerCallback(object state)
        {
            Update();
        }

        void Clear()
        {
            m_ReceiveBuffer.Clear();
            m_KeyFrameTickResultData = null;
            m_PreviosTickResultData = null;
            m_DiffScore = 0;
            m_LastTickResultToken = 0;
            m_TickResultToken = 0;
        }
        public void DebugTreeWithAgent(string treename, uint agentUID, List<WorkBench> benches)
        {
            if (benches == null || benches.Count == 0)
            {
                LogMgr.Instance.Error("No Active Tree");
                return;
            }

            Action<Node, StringBuilder> appendOneNode = null;
            appendOneNode = delegate (Node node, StringBuilder stringBuilder)
            {
                if (!node.DebugPointInfo.NoDebugPoint)
                {
                    stringBuilder.Append(cContentSplitter).Append(node.UID).Append(cContentSplitter).Append(node.DebugPointInfo.HitCount);
                }

                foreach (Node chi in node.Conns)
                {
                    appendOneNode(chi, stringBuilder);
                }
            };


            StringBuilder sb = new StringBuilder();
            sb.Append("[DebugTreeWithAgent] ").Append(treename);
            sb.Append(" ").Append(agentUID);

            foreach (WorkBench workBench in benches)
            {
                sb.Append(" ");
                sb.Append(workBench.FileInfo.Name).Append(cContentSplitter).Append(workBench.ExportFileHash);

                appendOneNode(workBench.MainTree, sb);
            }


            NetworkMgr.Instance.SendText(sb.ToString());

            Clear();
        }

        public void DoContinue()
        {
            NetworkMgr.Instance.SendText("[Continue]");
        }

        public void DoStepInto()
        {
            NetworkMgr.Instance.SendText("[StepInto]");
        }

        public void DoStepOver()
        {
            NetworkMgr.Instance.SendText("[StepOver]");
        }

        public void SetDebugPoint(string treename, uint uid, int count)
        {
            NetworkMgr.Instance.SendText("[DebugPoint] " + treename + " " + uid.ToString() + " " + count.ToString());
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

            m_DiffScore = 0.0f;
            m_KeyFrameTickResultData = null;

            for (int i = 0; i < m_ProcessingMsgs.Count; ++i)
            {
                _ProcessMsg(m_ProcessingMsgs[i]);
            }

            m_ProcessingMsgs.Clear();

            LogMgr.Instance.Log("KeyFrameScore: " + m_DiffScore);
            _FireTickResult();
        }

        char[] msgHeadSplitter = { (char)(3) };
        char[] msgContentSplitter = { cContentSplitter };
        static readonly char cContentSplitter = (char)4;
        void _ProcessMsg(string msg)
        {
            string[] words = msg.Split(msgHeadSplitter, StringSplitOptions.RemoveEmptyEntries);

            switch(words[0])
            {
                case "[TickResult]":
                    if (words.Length != 2)
                        return;
                    _CompareTickResult(words[1]);
                    //_HandleTickResult(words[1]);
                    break;
                case "[Paused]":
                    _HandlePaused();
                    break;
                case "[LogPoint]":
                    if (words.Length != 2)
                        return;
                    _HandleLogPoint(words[1]);
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
        string m_KeyFrameTickResultData = null;
        string m_PreviosTickResultData = null;
        float m_DiffScore = 0.0f;

        uint m_TickResultToken = 0;
        uint m_LastTickResultToken = 0;
        public uint TickResultToken
        {
            get { return m_TickResultToken; }
        }

        void _CompareTickResult(string ss)
        {
            if (m_PreviosTickResultData == null)
            {
                m_PreviosTickResultData = ss;
                m_KeyFrameTickResultData = ss;
                return;
            }

            DiffMatchPatch.diff_match_patch dmp = new DiffMatchPatch.diff_match_patch();
            List<DiffMatchPatch.Diff> diffs = dmp.diff_main(m_PreviosTickResultData, ss);
            m_PreviosTickResultData = ss;

            float diff = 0.0f;
            for (int i = 0; i < diffs.Count; ++i)
            {
                if (diffs[i].operation == DiffMatchPatch.Operation.EQUAL)
                    continue;
                diff += (float)Math.Sqrt(diffs[i].text.Length);
            }

            if(m_DiffScore <= diff)
            {
                m_DiffScore = diff;
                m_KeyFrameTickResultData = ss;
            }
        }

        void _HandleTickResult(string ss)
        {
            if (ss == null)
                return;
            string[] data = ss.Split(msgContentSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (data.Length > 1)
            {
                using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
                {
                    string[] sharedDatas = data[0].Split(';');
                    foreach (string s in sharedDatas)
                    {
                        string[] strV = s.Split(',');
                        if (strV.Length != 2)
                            continue;

                        Variable v = DebugMgr.Instance.DebugSharedData.GetVariable(strV[0]);
                        if (v == null)
                            continue;

                        bool isRefreshed = v.Value != strV[1];
                        v.Value = strV[1];
                        v.IsRefreshed = isRefreshed;
                    }
                }
                ++m_TickResultToken;

                if (data.Length % 2 == 1)
                {
                    DebugMgr.Instance.ClearRunState();
                    RunInfo runInfo = null;
                    for (int i = 1; i < data.Length; ++i)
                    {
                        if (i % 2 == 1)
                        {
                            runInfo = DebugMgr.Instance.GetRunInfo(data[i]);
                            runInfo.info.Clear();
                            continue;
                        }
                        string[] runInfos = data[i].Split(';');
                        foreach (string s in runInfos)
                        {
                            string[] strR = s.Split('=');
                            if (strR.Length != 2)
                                continue;

                            runInfo.info[uint.Parse(strR[0])] = int.Parse(strR[1]);
                        }
                    }
                }
                //EventMgr.Instance.Send(new TickResultArg() { bInstant = !DebugMgr.Instance.bBreaked, Token = m_TickResultToken });
            }
        }

        void _FireTickResult()
        {
            if (m_KeyFrameTickResultData != null)
            {
                _HandleTickResult(m_KeyFrameTickResultData);
                m_KeyFrameTickResultData = null;
                m_DiffScore = 0;
            }
            if (m_LastTickResultToken != m_TickResultToken)
            {
                //LogMgr.Instance.Log("TickResult bInstant = " + (DebugMgr.Instance.bBreaked ? "False" : "True"));
                EventMgr.Instance.Send(new TickResultArg() { /*bInstant = !DebugMgr.Instance.bBreaked, */Token = m_TickResultToken });
                m_LastTickResultToken = m_TickResultToken;
            }
        }

        void _HandlePaused()
        {
            LogMgr.Instance.Log("Paused.");
            DebugMgr.Instance.bBreaked = true;
        }

        void _HandleLogPoint(string ss)
        {
            if (Config.Instance.PrintIntermediateInfo)
                LogMgr.Instance.Log(ss);

            string[] data = ss.Split(msgContentSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (data.Length > 0)
            {
                ///> Head
                LogMgr.Instance.LogWordWithColor("-------<LogPoint ", ConsoleColor.Cyan);

                int index = 0;
                if (index < data.Length)
                {
                    LogMgr.Instance.LogWordWithColor(data[index], ConsoleColor.Cyan);
                }

                LogMgr.Instance.LogLineWithColor(">-------", ConsoleColor.Cyan);

                ///> Before
                ++index;
                while(index < data.Length)
                {
                    if (data[index] == "BEFORE" || data[index] == "AFTER")
                    {
                        LogMgr.Instance.LogLineWithColor(data[index], ConsoleColor.Cyan);
                        ++index;
                        if (index < data.Length)
                        {
                            int vCount = int.Parse(data[index]);
                            ++index;
                            if (vCount + index < data.Length)
                            {
                                for (int i = 0; i < vCount; ++i)
                                {
                                    LogMgr.Instance.LogWordWithColor(data[index + i], ConsoleColor.Magenta);
                                    LogMgr.Instance.LogWordWithColor("; ", ConsoleColor.Magenta);
                                }
                                index += vCount;
                            }
                        }
                        LogMgr.Instance.LogEnd();
                    }
                    else
                    {
                        LogMgr.Instance.LogWordWithColor(data[index++], ConsoleColor.Yellow);
                    }
                }
                LogMgr.Instance.LogEnd();
                LogMgr.Instance.LogLineWithColor("-------</LogPoint>-------", ConsoleColor.Cyan);
            }
        }
    }
}
