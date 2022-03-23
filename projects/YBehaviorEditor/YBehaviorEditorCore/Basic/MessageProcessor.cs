using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    class TickResultData
    {
        public string MainData;
        public string FSMRunData;
        public Dictionary<string, TreeResult> TreeRunDatas = new Dictionary<string, TreeResult>();
        public struct TreeResult
        {
            public string Name;
            public string LocalData;
            public string RunData;
        }

        public void BuildFromString(string ss)
        {
            if (ss == null)
                return;
            string[] data = ss.Split(MessageProcessor.msgContentSplitter);
            int subtreesoffset = 2;
            if (data.Length >= subtreesoffset)
            {
                MainData = data[0];
                FSMRunData = data[1];

                if ((data.Length - subtreesoffset) % 3 == 0)
                {
                    TreeResult treeResult = new TreeResult();
                    for (int i = subtreesoffset; i < data.Length; ++i)
                    {
                        int innerIndex = i - subtreesoffset;
                        ///> TreeName
                        if (innerIndex % 3 == 0)
                        {
                            treeResult.Name = data[i];
                            continue;
                        }
                        ///> LocalData
                        ///> Only copied to the specified subtree
                        if (innerIndex % 3 == 1)
                        {
                            treeResult.LocalData = data[i];
                            continue;
                        }

                        ///> RunInfo
                        treeResult.RunData = data[i];
                        TreeRunDatas[treeResult.Name] = treeResult;
                    }
                }
            }

        }
    }
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
                System.Windows.Media.CompositionTarget.Rendering -= _ProcessMessages;
                System.Windows.Media.CompositionTarget.Rendering += _ProcessMessages;

                if (m_Timer != null)
                    m_Timer.Stop();
                m_Timer = new System.Windows.Threading.DispatcherTimer();
                m_Timer.Tick += new EventHandler(_ShowTickResultTimerCallback);
                m_Timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                m_Timer.Start();
                Clear();
            }
            else
            {
                System.Windows.Media.CompositionTarget.Rendering -= _ProcessMessages;

                if (m_Timer != null)
                    m_Timer.Stop();
                m_Timer = null;
                Clear();
            }
        }

        void _ShowTickResultTimerCallback(object state, System.EventArgs arg)
        {
            _FireTickResult();

            m_DiffScore = 0.0f;
            m_KeyFrameTickResultData = null;

            //LogMgr.Instance.Log("KeyFrameScore: " + m_DiffScore);
        }

        void _ProcessMessages(object state, System.EventArgs arg)
        {
            if (m_ReceiveBuffer.Count == 0)
                return;

            lock (m_Lock)
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

            if (DebugMgr.Instance.bBreaked)
                _FireTickResult();
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
        public void DebugTreeWithAgent(FileMgr.FileInfo fileInfo, ulong agentUID, bool waitforbegin)
        {
            StringBuilder sb = new StringBuilder();
            string head;
            string content;
            if (agentUID != 0 || fileInfo == null)
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
            sb.Append(head).Append(msgContentSplitter).Append(content).Append(msgContentSplitter).Append(waitforbegin ? 1 : 0);
            // [DebugXXX] name waitforbegin
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
            DebugMgr.Instance.ClearRunState();
        }

        public void DoStepInto()
        {
            NetworkMgr.Instance.SendText("[StepInto]");
            DebugMgr.Instance.ClearRunState();
        }

        public void DoStepOver()
        {
            NetworkMgr.Instance.SendText("[StepOver]");
            DebugMgr.Instance.ClearRunState();
        }

        public void SetDebugPoint(FileMgr.FileInfo fileInfo, uint uid, int count)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(fileInfo.FileType == FileType.TREE ? "[DebugTreePoint]" : "[DebugFSMPoint]")
                .Append(msgContentSplitter).Append(fileInfo.Name)
                .Append(msgContentSplitter).Append(uid)
                .Append(msgContentSplitter).Append(count);
            NetworkMgr.Instance.SendText(sb.ToString());
            //NetworkMgr.Instance.SendText("[DebugPoint]" + msgContentSplitter + treename + msgContentSplitter + uid.ToString() + msgContentSplitter + count.ToString());
        }

        public static readonly char cContentSplitter = (char)4;
        public static readonly char cSectionSplitter = (char)5;
        public static readonly char cSequenceSplitter = (char)6;
        public static readonly char cListSplitter = (char)7;
        public static readonly char[] msgHeadSplitter = { (char)(3) };
        public static readonly char[] msgContentSplitter = { cContentSplitter };
        public static readonly char[] msgSectionSplitter = { cSectionSplitter };
        public static readonly char[] msgSequenceSplitter = { cSequenceSplitter };
        public static readonly char[] msgListSplitter = { cListSplitter };
        void _ProcessMsg(string msg)
        {
            string[] words = msg.Split(msgHeadSplitter, StringSplitOptions.RemoveEmptyEntries);

            switch(words[0])
            {
                case "[TickResult]":
                    if (words.Length != 2)
                        return;
                    var data = _PreProcessTickResult(words[1]);
                    _CompareTickResult(data);
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
        TickResultData m_KeyFrameTickResultData = null;
        TickResultData m_PreviousTickResultData = null;
        float m_PreviousDiffScore = 0.0f;
        float m_DiffScore = 0.0f;

        uint m_TickResultToken = 0;
        uint m_LastTickResultToken = 0;
        public uint TickResultToken
        {
            get { return m_TickResultToken; }
        }

        DiffMatchPatch.diff_match_patch dmp = new DiffMatchPatch.diff_match_patch();
        TickResultData _PreProcessTickResult(string ss)
        {
            TickResultData data = new TickResultData();
            data.BuildFromString(ss);
            return data;
        }
        void _CompareTickResult(TickResultData ss)
        {
            if (m_PreviousTickResultData == null)
            {
                m_PreviousTickResultData = ss;
                m_KeyFrameTickResultData = ss;
                return;
            }

            var bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
            {
                return;
            }

            Func<List<DiffMatchPatch.Diff>, float, float> func = 
                (List<DiffMatchPatch.Diff> diffList, float w) =>
            {
                float d = 0f;
                for (int i = 0; i < diffList.Count; ++i)
                {
                    if (diffList[i].operation == DiffMatchPatch.Operation.EQUAL)
                        continue;
                    d += (float)Math.Sqrt(diffList[i].text.Length) * w;
                }
                return d;
            };

            float diff = 0.0f;
            float weight = 0f;

            if (bench is FSMBench)
            {
                diff += func(dmp.diff_main(m_PreviousTickResultData.FSMRunData, ss.FSMRunData), 1f);
                weight += 1f;
            }
            else
            {
                if (m_PreviousTickResultData.TreeRunDatas.TryGetValue(bench.FileInfo.Name, out var prev)
                    &&
                    ss.TreeRunDatas.TryGetValue(bench.FileInfo.Name, out var cur))
                {
                    diff += func(dmp.diff_main(prev.RunData, cur.RunData), 1f);
                    diff += func(dmp.diff_main(prev.LocalData, cur.LocalData), 0.2f);
                    weight += 1.2f;
                }
            }
            diff += func(dmp.diff_main(m_PreviousTickResultData.MainData, ss.MainData), 0.2f);
            diff /= weight;

            float avgDiff = (diff + m_PreviousDiffScore) * 0.5f;
            if (m_DiffScore == 0.0f)
            {
                m_DiffScore = avgDiff;
                m_KeyFrameTickResultData = ss;
            }
            else if(m_DiffScore <= avgDiff)
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
        void _HandleTickResult(TickResultData data)
        {
            if (data == null)
                return;

            DebugMgr.Instance.ClearRunState();
            ///> MainData
            ///> Copied to each subtrees
            foreach (var v in DebugMgr.Instance.GetRunInfos.Values)
            {
                if (v.sharedData != null)
                    _HandleMemory(v.sharedData.SharedMemory, data.MainData);
            }

            ///> FSM RunInfo
            {
                FSMRunInfo runInfo = DebugMgr.Instance.FSMRunInfo;
                string[] runInfos = data.FSMRunData.Split(msgListSplitter);
                foreach (string s in runInfos)
                {
                    string[] strR = s.Split(msgSequenceSplitter);
                    if (strR.Length != 3)
                        continue;

                    runInfo.info[uint.Parse(strR[0])] = int.Parse(strR[1]);
                }

            }

            ++m_TickResultToken;

            foreach (var d in data.TreeRunDatas.Values)
            {
                TreeRunInfo runInfo = DebugMgr.Instance.GetRunInfo(d.Name);
                runInfo.info.Clear();
                if (runInfo.sharedData != null)
                    _HandleMemory(runInfo.sharedData.LocalMemory, d.LocalData);
                string[] runInfos = d.RunData.Split(msgListSplitter);
                foreach (string s in runInfos)
                {
                    string[] strR = s.Split(msgSequenceSplitter);
                    if (strR.Length != 3)
                        continue;

                    runInfo.info[uint.Parse(strR[0])] = new TreeRunInfo.ResultState()
                    {
                        Self = int.Parse(strR[1]),
                        Final = int.Parse(strR[2]),
                    };
                }
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
                LogMgr.Instance.LogLineWithColor("\nEditor Time " + System.DateTime.Now.ToString("hh:mm:ss.fff"), ConsoleColor.DarkGreen);
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
                List<string> names = new List<string>();
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
                        /////> The first one is the FSM
                        //if (names.Count == 0)
                        //    names.Add(new BenchInfo() { Name = sub[0], Type = GraphType.FSM });
                        //else
                        //    names.Add(new BenchInfo() { Name = sub[0], Type = GraphType.TREE });
                        names.Add(sub[0]);
                        hashes.Add(hash);
                    }
                }

                if (hashes.Count > 0)
                {
                    List<WorkBench> res = WorkBenchMgr.Instance.OpenAList(names);
                    if (res.Count != hashes.Count)
                    {
                        LogMgr.Instance.Error("Open some files failed.");
                        _DoDebugFailed();
                    }
                    else
                    {
                        bool notmatch = false;
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
                                notmatch = true;

                                System.Windows.MessageBoxResult dr = System.Windows.MessageBox.Show(res[i].DisplayName + ": version is different from RunTime.", "Continue Or Not", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
                                if (dr == System.Windows.MessageBoxResult.Cancel)
                                {
                                    _DoDebugFailed();
                                    return;
                                }
                                break;
                            }
                        }

                        DebugMgr.Instance.BuildRunInfo(res);
                        _DoDebugBegin(res);

                        if (!notmatch)
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
                    Utility.OperateNode((workBench as TreeBench).Tree.Root, action);
                }
            }


            NetworkMgr.Instance.SendText(sb.ToString());

        }

        void _DoDebugFailed()
        {
            NetworkMgr.Instance.SendText("[DebugFailed]");
        }
    }
}
