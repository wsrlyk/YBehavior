using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class MessageProcessor
    {
        object m_Lock = new object();
        Queue<string> m_ReceiveBuffer = new Queue<string>();
        List<string> m_ProcessingMsgs = new List<string>();

        System.Windows.Threading.DispatcherTimer m_Timer;
        delegate void DebugTreeWithAgentAppendOneNode(NodeBase node, StringBuilder stringBuilder);
        
        public void OnNetworkConnectionChanged(bool bConnected)
        {
            if (bConnected)
            {
                if (m_Timer != null)
                    m_Timer.Stop();
                m_Timer = new System.Windows.Threading.DispatcherTimer();
                m_Timer.Tick += new EventHandler(_MessageTimerCallback);
                m_Timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                m_Timer.Start();
                Clear();
            }
            else
            {
                if (m_Timer != null)
                    m_Timer.Stop();
                m_Timer = null;
                Clear();
            }
        }

        void _MessageTimerCallback(object state, System.EventArgs arg)
        {
            Update();
        }

        void Clear()
        {
            m_ReceiveBuffer.Clear();
            m_KeyFrameTickResultData = null;
            m_PreviousTickResultData = null;
            m_PreviousDiffScore = 0.0f;
            m_DiffScore = 0.0f;
            m_LastTickResultToken = 0;
            m_TickResultToken = 0;
        }
        public void DebugTreeWithAgent(TreeFileMgr.TreeFileInfo fileInfo, ulong agentUID)
        {
            StringBuilder sb = new StringBuilder();
            string head;
            string content;
            if (agentUID != 0 || fileInfo == null || fileInfo.FileType == FileType.FOLDER)
            {
                head = "[DebugAgent]";
                content = agentUID.ToString();
            }
            else
            {
                if (fileInfo.FileType == FileType.TREE)
                    head = "[DebugTree]";
                else
                    head = "[DebugFSM]";
                content = fileInfo.Name;
            }
            sb.Append(head).Append(msgContentSplitter).Append(content);
            NetworkMgr.Instance.SendText(sb.ToString());
            Clear();
        }
        //public void DebugTreeWithAgent(string treename, ulong agentUID, List<WorkBench> benches)
        //{
        //    if (benches == null || benches.Count == 0)
        //    {
        //        LogMgr.Instance.Error("No Active Tree");
        //        return;
        //    }

        //    Action<Node, StringBuilder> appendOneNode = null;
        //    appendOneNode = delegate (Node node, StringBuilder stringBuilder)
        //    {
        //        if (!node.DebugPointInfo.NoDebugPoint)
        //        {
        //            stringBuilder.Append(cContentSplitter).Append(node.UID).Append(cContentSplitter).Append(node.DebugPointInfo.HitCount);
        //        }

        //        foreach (Node chi in node.Conns)
        //        {
        //            appendOneNode(chi, stringBuilder);
        //        }
        //    };


        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("[DebugTreeWithAgent] ").Append(treename);
        //    sb.Append(" ").Append(agentUID);

        //    foreach (WorkBench workBench in benches)
        //    {
        //        sb.Append(" ");
        //        sb.Append(workBench.FileInfo.Name).Append(cContentSplitter).Append(workBench.ExportFileHash);

        //        appendOneNode(workBench.MainTree, sb);
        //    }


        //    NetworkMgr.Instance.SendText(sb.ToString());

        //    Clear();
        //}

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
            StringBuilder sb = new StringBuilder();
            sb.Append("[DebugPoint]").Append(msgContentSplitter).Append(treename).Append(msgContentSplitter).Append(uid).Append(msgContentSplitter).Append(count);
            NetworkMgr.Instance.SendText(sb.ToString());
            //NetworkMgr.Instance.SendText("[DebugPoint]" + msgContentSplitter + treename + msgContentSplitter + uid.ToString() + msgContentSplitter + count.ToString());
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

            //LogMgr.Instance.Log("KeyFrameScore: " + m_DiffScore);
            _FireTickResult();
        }

        char[] msgHeadSplitter = { (char)(3) };
        char[] msgContentSplitter = { cContentSplitter };
        char[] msgSectionSplitter = { cSectionSplitter };
        char[] msgSequenceSplitter = { cSequenceSplitter };
        char[] msgListSplitter = { cListSplitter };
        static readonly char cContentSplitter = (char)4;
        static readonly char cSectionSplitter = (char)5;
        static readonly char cSequenceSplitter = (char)6;
        static readonly char cListSplitter = (char)7;
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
                case "[SubTrees]":
                    if (words.Length != 2)
                        return;
                    _HandleSubTrees(words[1]);
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
        string m_PreviousTickResultData = null;
        float m_PreviousDiffScore = 0.0f;
        float m_DiffScore = 0.0f;

        uint m_TickResultToken = 0;
        uint m_LastTickResultToken = 0;
        public uint TickResultToken
        {
            get { return m_TickResultToken; }
        }

        void _CompareTickResult(string ss)
        {
            if (m_PreviousTickResultData == null)
            {
                m_PreviousTickResultData = ss;
                m_KeyFrameTickResultData = ss;
                return;
            }

            DiffMatchPatch.diff_match_patch dmp = new DiffMatchPatch.diff_match_patch();
            List<DiffMatchPatch.Diff> diffs = dmp.diff_main(m_PreviousTickResultData, ss);

            float diff = 0.0f;
            for (int i = 0; i < diffs.Count; ++i)
            {
                if (diffs[i].operation == DiffMatchPatch.Operation.EQUAL)
                    continue;
                diff += (float)Math.Sqrt(diffs[i].text.Length);
            }

            float avgDiff = (diff + m_PreviousDiffScore) * 0.5f;
            if(m_DiffScore <= avgDiff)
            {
                m_DiffScore = avgDiff;
                m_KeyFrameTickResultData = m_PreviousTickResultData;
                //LogMgr.Instance.Log("Larger Content ^ " + avgDiff);
            }
            m_PreviousDiffScore = diff;
            m_PreviousTickResultData = ss;
            //LogMgr.Instance.Log(ss);
        }

        void _HandleMemory(IVariableCollection variableCollection, string datas)
        {
            if (variableCollection == null || datas == null)
                return;
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                {
                    string[] sharedDatas = datas.Split(msgListSplitter);
                    foreach (string s in sharedDatas)
                    {
                        string[] strV = s.Split(msgSequenceSplitter);
                        if (strV.Length != 2)
                            continue;

                        Variable v = variableCollection.GetVariable(strV[0]);
                        if (v == null)
                            continue;

                        bool isRefreshed = v.Value != strV[1];
                        v.SetValue(strV[1], v.IsLocal);
                        v.IsRefreshed = isRefreshed;
                    }
                }
            }
        }
        void _HandleTickResult(string ss)
        {
            if (ss == null)
                return;
            string[] data = ss.Split(msgContentSplitter);
            if (data.Length >= 2)
            {
                DebugMgr.Instance.ClearRunState();
                ///> MainData
                ///> Copied to each subtrees
                foreach (var v in DebugMgr.Instance.GetRunInfos.Values)
                {
                    if (v.sharedData != null)
                        _HandleMemory(v.sharedData.SharedMemory, data[0]);
                }

                ///> FSM RunInfo
                {
                    FSMRunInfo runInfo = DebugMgr.Instance.FSMRunInfo;
                    string[] runInfos = data[1].Split(msgListSplitter);
                    foreach (string s in runInfos)
                    {
                        string[] strR = s.Split(msgSequenceSplitter);
                        if (strR.Length != 2)
                            continue;

                        runInfo.info[uint.Parse(strR[0])] = int.Parse(strR[1]);
                    }

                }

                ++m_TickResultToken;

                int offset = 2;
                if ((data.Length - offset) % 3 == 0)
                {
                    TreeRunInfo runInfo = null;
                    for (int i = offset; i < data.Length; ++i)
                    {
                        int innerIndex = i - offset;
                        ///> TreeName
                        if (innerIndex % 3 == 0)
                        {
                            runInfo = DebugMgr.Instance.GetRunInfo(data[i]);
                            runInfo.info.Clear();
                            continue;
                        }
                        ///> LocalData
                        ///> Only copied to the specified subtree
                        if (innerIndex % 3 == 1)
                        {
                            if (runInfo.sharedData != null)
                                _HandleMemory(runInfo.sharedData.LocalMemory, data[i]);
                            continue;
                        }

                        ///> RunInfo
                        string[] runInfos = data[i].Split(msgListSplitter);
                        foreach (string s in runInfos)
                        {
                            string[] strR = s.Split(msgSequenceSplitter);
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
            ///> Make the next frame become key frame
            m_PreviousTickResultData = null;
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
                LogMgr.Instance.LogWordWithColor("-------<LogPoint ", ConsoleColor.DarkGreen);

                int index = 0;
                if (index < data.Length)
                {
                    LogMgr.Instance.LogWordWithColor(data[index], ConsoleColor.DarkGreen);
                }

                LogMgr.Instance.LogLineWithColor(">-------", ConsoleColor.DarkGreen);

                ///> State
                ++index;
                NodeState rawState = NodeState.NS_INVALID;
                NodeState finalState = NodeState.NS_INVALID;
                if (index + 1 < data.Length)
                {
                    int v;
                    if (int.TryParse(data[index], out v))
                    {
                        rawState = (NodeState)v;
                    }
                    ++index;
                    if (int.TryParse(data[index], out v))
                    {
                        finalState = (NodeState)v;
                    }
                }

                ///> Before
                ++index;
                while(index < data.Length)
                {
                    if (data[index] == "BEFORE" || data[index] == "AFTER")
                    {
                        LogMgr.Instance.LogLineWithColor(data[index], ConsoleColor.DarkYellow);
                        ++index;
                        if (index < data.Length)
                        {
                            int vCount = int.Parse(data[index]);
                            ++index;
                            if (vCount + index <= data.Length)
                            {
                                for (int i = 0; i < vCount; ++i)
                                {
                                    LogMgr.Instance.LogWordWithColor(data[index + i], ConsoleColor.White);
                                    LogMgr.Instance.LogWordWithColor("; ", ConsoleColor.DarkYellow);
                                }
                                index += vCount;
                            }
                        }
                        LogMgr.Instance.LogEnd();
                    }
                    else
                    {
                        LogMgr.Instance.LogWordWithColor(data[index++], ConsoleColor.White);
                    }
                }
                LogMgr.Instance.LogEnd();

                if (rawState == finalState)
                    LogMgr.Instance.LogLineWithColor(_GetStateString(rawState), _GetStateColor(rawState));
                else
                {
                    LogMgr.Instance.LogWordWithColor(_GetStateString(finalState), _GetStateColor(finalState));
                    LogMgr.Instance.LogWordWithColor("(FINAL) ", ConsoleColor.Gray);
                    LogMgr.Instance.LogWordWithColor(_GetStateString(rawState), _GetStateColor(rawState));
                    LogMgr.Instance.LogLineWithColor("(RAW)", ConsoleColor.Gray);
                }
                LogMgr.Instance.LogLineWithColor("-------</LogPoint>-------", ConsoleColor.DarkGreen);
            }
        }

        ConsoleColor _GetStateColor(NodeState state)
        {
            switch (state)
            {
                case NodeState.NS_SUCCESS:
                    return ConsoleColor.Green;
                case NodeState.NS_FAILURE:
                    return ConsoleColor.Cyan;
                case NodeState.NS_BREAK:
                    return ConsoleColor.Red;
                case NodeState.NS_RUNNING:
                    return ConsoleColor.Magenta;
                default:
                    return ConsoleColor.Gray;
            }
        }

        string _GetStateString(NodeState state)
        {
            switch (state)
            {
                case NodeState.NS_SUCCESS:
                    return "SUCCESS";
                case NodeState.NS_FAILURE:
                    return "FAILURE";
                case NodeState.NS_BREAK:
                    return "BREAK";
                case NodeState.NS_RUNNING:
                    return "RUNNING";
                default:
                    return state.ToString();
            }
        }

        void _HandleSubTrees(string ss)
        {
            if (Config.Instance.PrintIntermediateInfo)
                LogMgr.Instance.Log(ss);

            string[] data = ss.Split(msgListSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (data.Length > 0)
            {
                List<BenchInfo> names = new List<BenchInfo>();
                List<uint> hashes = new List<uint>();

                foreach (string s in data)
                {
                    string[] sub = s.Split(msgSequenceSplitter, StringSplitOptions.RemoveEmptyEntries);
                    if (sub.Length != 2)
                    {
                        EventMgr.Instance.Send(new ShowSystemTipsArg
                        {
                            Content = "Format error: " + s,
                            TipType = ShowSystemTipsArg.TipsType.TT_Error,
                        });

                        return;
                    }

                    if (uint.TryParse(sub[1], out uint hash))
                    {
                        ///> The first one is the FSM
                        if (names.Count == 0)
                            names.Add(new BenchInfo() { Name = sub[0], Type = GraphType.FSM });
                        else
                            names.Add(new BenchInfo() { Name = sub[0], Type = GraphType.TREE });
                        hashes.Add(hash);
                    }
                }

                if (hashes.Count > 0)
                {
                    List<WorkBench> res = WorkBenchMgr.Instance.OpenAList(names);
                    if (res.Count != hashes.Count)
                    {
                        LogMgr.Instance.Error("Open some files failed.");
                    }
                    else
                    {
                        for (int i = 0; i < res.Count; i++)
                        {
                            if (res[i].ExportFileHash != hashes[i])
                            {
                                ShowSystemTipsArg arg = new ShowSystemTipsArg
                                {
                                    Content = res[i].DisplayName + " version is different from RunTime.",
                                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                                };
                                EventMgr.Instance.Send(arg);
                                break;
                            }
                        }

                        DebugMgr.Instance.BuildRunInfo(res);
                        _DoDebugBegin(res);

                        {
                            ShowSystemTipsArg arg = new ShowSystemTipsArg
                            {
                                Content = "Start Debug",
                                TipType = ShowSystemTipsArg.TipsType.TT_Success,
                            };
                            EventMgr.Instance.Send(arg);
                        }

                        {
                            bool bOpened = false;
                            foreach (WorkBench bench in res)
                            {
                                if (WorkBenchMgr.Instance.ActiveWorkBench == bench)
                                {
                                    bOpened = true;
                                    break;
                                }
                            }
                            if (!bOpened)
                            {
                                SelectWorkBenchArg arg = new SelectWorkBenchArg
                                {
                                    Bench = res[0],
                                };
                                EventMgr.Instance.Send(arg);
                            }
                            else
                            {
                                DebugTargetChangedArg arg = new DebugTargetChangedArg();
                                EventMgr.Instance.Send(arg);
                            }
                        }
                    }
                }
            }
        }

        void _DoDebugBegin(List<WorkBench> benches)
        {
            //Action<NodeBase, StringBuilder> appendOneNode = null;
            //appendOneNode = delegate (NodeBase node, StringBuilder stringBuilder)
            //{
            //    if (!node.DebugPointInfo.NoDebugPoint)
            //    {
            //        stringBuilder.Append(cSequenceSplitter).Append(node.UID).Append(cSequenceSplitter).Append(node.DebugPointInfo.HitCount);
            //    }

            //    foreach (NodeBase chi in node.Conns)
            //    {
            //        appendOneNode(chi, stringBuilder);
            //    }
            //};


            StringBuilder sb = new StringBuilder();
            sb.Append("[DebugBegin]");

            void action(NodeBase node)
            {
                sb.Append(cSequenceSplitter).Append(node.UID).Append(cSequenceSplitter).Append(node.DebugPointInfo.HitCount);
            }

            foreach (WorkBench workBench in benches)
            {
                sb.Append(cContentSplitter);
                sb.Append(workBench.FileInfo.Name);

                if (workBench is FSMBench)
                {
                    Utility.ForEachFSMState((workBench as FSMBench).FSM, action);
                }
                else
                {
                    Utility.OperateNode((workBench as TreeBench).Tree.Root, true, action);
                }
            }


            NetworkMgr.Instance.SendText(sb.ToString());

        }
    }
}
