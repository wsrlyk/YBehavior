﻿using System.Diagnostics;
using YBehaviorSharp;

namespace TestSharp
{
    class Program
    {
        class SharpLauncher : YBehaviorSharp.ISharpLauncher
        {
            public int DebugPort => 444;
            public void OnLog()
            {
                string data = SharpHelper.GetFromBufferString();
                Console.WriteLine(data);
            }
            public void OnError()
            {
                string data = SharpHelper.GetFromBufferString();
                Console.WriteLine(data);
            }

            public void OnGetFilePath()
            {
                string filename = SharpHelper.GetFromBufferString();
                filename = "E:\\Develop\\YBehavior\\projects\\YBehaviorEditor\\bin\\exported\\" + filename;
                SharpHelper.SetToBufferString(filename);
            }

        }
        static void Main(string[] args)
        {
            SharpLauncher launcher = new SharpLauncher();
            YBehaviorSharp.SharpHelper.Init(launcher);
            SharpHelper.RegisterTreeNode(new XCustomAction());

            XEntity entity0 = new XEntity("Hehe");
            XEntity entity1 = new XEntity("Haha");

            Scene.Instance.entities[0] = entity0;
            Scene.Instance.entities[1] = entity1;

            string[] state2tree = new string[] { "Main", "Test0"};
            YBehaviorSharp.SharpHelper.SetBehavior(entity0.Agent.Ptr, "EmptyFSM", state2tree, 2, null, 0);
            YBehaviorSharp.SharpHelper.SetSharedVariableByString(entity0.Agent.Ptr, "II0", "1342^32^643", '^');

            int i = 0;
            while (++i < 500)
            {
                YBehaviorSharp.SharpHelper.Tick(entity0.Agent.Ptr);
                System.Threading.Thread.Sleep(1000);
            }

            //Put it here only for TEST. SHOULD put it at the end.
            SharpHelper.UninitSharp();

            entity0.Destroy();
            entity1.Destroy();
            Console.WriteLine("End.");
            Console.Read();
        }

        static void ShowLog()
        {
            string data = SharpHelper.GetFromBufferString();
            Console.WriteLine(data);
        }

        static void GetFilePath()
        {
            string filename = SharpHelper.GetFromBufferString();
            filename = "E:\\Develop\\YBehavior\\projects\\YBehaviorEditor\\bin\\exported\\" + filename;
            SharpHelper.SetToBufferString(filename);
        }
    }

    class Scene
    {
        public XEntity[] entities = new XEntity[2];
        public static Scene Instance = new Scene();
    }
    /// <summary>
    /// Entity used in Real Game
    /// </summary>
    class XEntity : IEntity
    {
        static ulong s_UID = 0;
        ulong m_UID = 0;
        public ulong UID => m_UID;
        public IntPtr Ptr { get; set; } = IntPtr.Zero;
        public int Index { get; set; } = -1;
        XSAgent m_SAgent;
        public XSAgent Agent { get { return m_SAgent; } }

        string m_Name;
        public string Name { get { return m_Name; } }

        public XEntity(string name)
        {
            m_UID = ++s_UID;
            m_Name = name;
            SharpHelper.CreateEntity(this);

            m_SAgent = new XSAgent(this);
        }

        public void Destroy()
        {
            m_SAgent.Destroy();
            SharpHelper.DestroyEntity(this);
        }
    }

    class XSAgent : IAgent
    {
        public IntPtr Ptr { get; set; } = IntPtr.Zero;
        public int Index { get; set; } = -1;
        public IEntity? Entity { get; set; } = null;
        public XSAgent(XEntity entity)
        {
            SharpHelper.CreateAgent(this, entity);
        }

        public void Destroy()
        {
            SharpHelper.DestroyAgent(this);
        }
    }
    public class SelectTargetAction : ITreeNodeWithPin
    {
        SPinEntity m_Target;

        public string NodeName => "SelectTargetAction";

        public bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Target = YBehaviorSharp.SharpHelper.CreatePin(pNode, "Target", pData, EPinCreateFlag.IsOutput) as SPinEntity;
            if (!m_Target.IsValid)
            {
                return false;
            }

            return true;
        }

        public ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex)
        {
            Console.WriteLine("SelectTargetAction Update");
            IEntity entity = m_Target.Get(pAgent);
            return ENodeState.Success;
        }
    }

    public class GetTargetNameAction : ITreeNodeWithNothing
    {
        public string NodeName => "GetTargetNameAction";

        public ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex)
        {
            Console.WriteLine("GetTargetNameAction Update");
            return ENodeState.Success;
        }
    }

    public class SetVector3Action : ITreeNodeWithPin
    {
        SPin m_Src;
        SPin m_Des;

        public string NodeName => "SetVector3Action";

        public bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Src = YBehaviorSharp.SharpHelper.CreatePin(pNode, "Src", pData);
            m_Des = YBehaviorSharp.SharpHelper.CreatePin(pNode, "Des", pData, EPinCreateFlag.IsOutput);

            return true;
        }

        public ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex)
        {
            Console.WriteLine("SetVector3Action Update");

            Vector3 src = (m_Src as SPinVector3).Get(pAgent);
            src.x += 1;
            (m_Des as SPinVector3).Set(pAgent, src);

            return ENodeState.Success;
        }
    }
    public class XCustomActionContext : ITreeNodeContext
    {
        int i = 0;
        public void OnInit()
        {
            i = 0;
            Console.WriteLine("XCustomActionContext.OnInit");
        }

        public ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex, ENodeState lastState)
        {
            ++i;
            if (i > 10)
            {
                Console.WriteLine("XCustomActionContext.Running " + i);
                return ENodeState.Success;
            }
            return ENodeState.Running;
        }
    }
    public class XCustomAction : ITreeNodeWithPin
    {
        //SVariableString m_String0;
        SPinInt m_Int0;

        //SVariableEntity m_Entity0;

        SPin m_Array0;
        SArrayPin? m_Entity0;
        public string NodeName => "XCustomAction";

        public ITreeNodeContext CreateContext()
        {
            return new XCustomActionContext();
        }

        public void DestroyContext(ITreeNodeContext context) { }
        public bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            //m_String0 = YBehaviorSharp.SVariableHelper.CreatePin(pNode, "String0", pData) as SVariableString;

            m_Int0 = YBehaviorSharp.SharpHelper.CreatePin(pNode, "Int0", pData) as SPinInt;

            //m_Entity0 = YBehaviorSharp.SVariableHelper.CreatePin(pNode, "Entity0", pData) as SVariableEntity;
            YBehaviorSharp.SharpHelper.CreatePin(ref m_Entity0, pNode, "Entity0", pData);

            m_Array0 = YBehaviorSharp.SharpHelper.CreatePin(pNode, "Array0", pData, EPinCreateFlag.IsOutput);

            if (YBehaviorSharp.SharpHelper.TryGetValue(pNode, "Type", pData))
            {
                string s = SharpHelper.GetFromBufferString();
                Console.WriteLine(string.Format("Type: {0}", s));
            }
            return true;
        }

        public ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex)
        {
            Console.WriteLine();
            Console.WriteLine("XCustomAction Update");

            //this.LogVariable(m_String0, true);
            //if (SharpHelper.IsDebugging)
            //    SharpHelper.TryLogPin(pNode, m_Int0, true);
            //this.LogVariable(m_Entity0, true);
            //this.LogVariable(m_Array0, true);
            XSAgent agent = YBehaviorSharp.SPtrMgr.Instance.Get(agentIndex) as XSAgent;
            if (agent == null)
                return ENodeState.Failure;

            var key0 = SharpHelper.GetOrCreateVariableKeyByName("S0");
            var key1 = SharpHelper.GetOrCreateVariableKeyByName("S1");

            SharpHelper.GetSharedVariable(pAgent, key0, out string sharedData0);
            sharedData0 = sharedData0 + "0";
            SharpHelper.SetSharedVariable(pAgent, key0, sharedData0);

            SharpHelper.GetSharedVariable(pAgent, key1, out string sharedData1);
            //sharedData1 = sharedData1 + "1";
            SharpHelper.SetToBufferString(sharedData1);
            //SharpHelper.SetSharedData(pAgent, key1, GetClassType<string>.ID);

            //string name0 = m_String0.Get(pAgent);
            //name = (agent.Entity as XSEntity).GetEntity.Name;
            //string name1 = m_String1.Get(pAgent);
            int int0 = m_Int0.Get(pAgent);
            //m_String0.Set(pAgent, name1);
            //m_String1.Set(pAgent, name0);
            m_Int0.Set(pAgent, int0 - 2);

            Console.WriteLine(string.Format("int0: {0}", int0));

            //this.LogInfo(string.Format("0: {0}, 1: {1}", name0, name1));
            ////////////////////////////////////////////////////////////////////////////

            //XSEntity entity = m_Entity0.Get(pAgent) as XSEntity;
            //if (entity != null)
            //{
            //    Console.WriteLine(string.Format("entity: {0}", entity.GetEntity.Name));
            //}
            //entity = Scene.Instance.entities[++counter % 2].Entity;
            //m_Entity0.Set(pAgent, entity);

            ////////////////////////////////////////////////////////////////////////////
            var keya = SharpHelper.GetOrCreateVariableKeyByName("II0");

            SArrayInt arr = SharpHelper.GetSharedArray<int>(pAgent, keya) as SArrayInt;
            //arr.Clear();
            arr.PushBack(100);
            //SharpHelper.SetSharedData(pAgent, keya, GetType<int>.VecID);

            if (m_Array0 is SArrayPin)
            {
                SArrayPin av = m_Array0 as SArrayPin;
                SArrayInt array = av.Get(pAgent) as SArrayInt;
                if (array != null)
                {
                    if (array.GetLength() > 10)
                        array.Clear();
                    array.PushBack(array.GetLength());

                    string s = array.Get(0).ToString();
                    for (int i = 1; i < array.GetLength(); ++i)
                    {
                        s += "|";
                        s += array.Get(i);
                    }
                    Console.WriteLine(string.Format("Array: {0}", s));
                }
            }

            if (m_Entity0 != null)
            {
                var array = m_Entity0.Get(pAgent) as SArrayEntity;
                if (array != null)
                {
                    array.Clear();
                    foreach (var item in Scene.Instance.entities)
                    {
                        array.PushBack(null);
                    }
                }
            }
            //SharpHelper.TryLogPin(pNode, m_Int0, false);
            //SharpHelper.TryLogPin(pNode, m_Array0, false);

            return ENodeState.Success;
        }

        static int counter = 0;
    }
}
