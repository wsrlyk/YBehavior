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
            YBehaviorSharp.SharpHelper.SetSharedDataByString(entity0.Agent.Ptr, "II0", "1342^32^643", '^');

            int i = 0;
            while (++i > 0)
            {
                YBehaviorSharp.SharpHelper.Tick(entity0.Agent.Ptr);
                System.Threading.Thread.Sleep(1000);
            }

            entity0.Destroy();
            entity1.Destroy();
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
        public IntPtr Ptr { get; set; } = IntPtr.Zero;
        XSAgent m_SAgent;
        public XSAgent Agent { get { return m_SAgent; } }

        string m_Name;
        public string Name { get { return m_Name; } }

        public XEntity(string name)
        {
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
    public class SelectTargetAction : ITreeNode
    {
        SVariableEntity m_Target;

        public string NodeName => "SelectTargetAction";

        public bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Target = new SVariableEntity(YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Target", pData, true));
            if (!m_Target.IsValid)
            {
                return false;
            }

            return true;
        }

        public NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("SelectTargetAction Update");
            IEntity entity = m_Target.Get(pAgent);
            return NodeState.NS_SUCCESS;
        }
    }

    public class GetTargetNameAction : IStaticTreeNode
    {
        public string NodeName => "GetTargetNameAction";

        public NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("GetTargetNameAction Update");
            return NodeState.NS_SUCCESS;
        }
    }

    public class SetVector3Action : ITreeNode
    {
        SVariable m_Src;
        SVariable m_Des;

        public string NodeName => "SetVector3Action";

        public bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Src = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Src", pData, true);
            m_Des = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Des", pData, true);

            return true;
        }

        public NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("SetVector3Action Update");

            Vector3 src = (m_Src as SVariableVector3).Get(pAgent);
            src.x += 1;
            (m_Des as SVariableVector3).Set(pAgent, src);

            return NodeState.NS_SUCCESS;
        }
    }

    public class XCustomAction : ITreeNode
    {
        //SVariableString m_String0;
        SVariableInt m_Int0;

        //SVariableEntity m_Entity0;

        SVariable m_Array0;

        public string NodeName => "XCustomAction";

        public bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            //m_String0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "String0", pData) as SVariableString;

            m_Int0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Int0", pData) as SVariableInt;

            //m_Entity0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Entity0", pData) as SVariableEntity;

            m_Array0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Array0", pData);

            if (YBehaviorSharp.SharpHelper.TryGetValue(pNode, "Type", pData))
            {
                string s = SharpHelper.GetFromBufferString();
                Console.WriteLine(string.Format("Type: {0}", s));
            }
            return true;
        }

        public NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine();
            Console.WriteLine("XCustomAction Update");

            //this.LogVariable(m_String0, true);
            SharpHelper.LogVariable(pNode, m_Int0.Ptr, true);
            //this.LogVariable(m_Entity0, true);
            //this.LogVariable(m_Array0, true);
            XSAgent agent = YBehaviorSharp.SPtrMgr.Instance.Get(pAgent) as XSAgent;
            if (agent == null)
                return NodeState.NS_FAILURE;

            var key0 = SharpHelper.GetTypeKeyByName("S0", GetClassType<string>.ID);
            var key1 = SharpHelper.GetTypeKeyByName("S1", GetClassType<string>.ID);

            string sharedData0 = SSharedData.GetSharedString(pAgent, key0);
            sharedData0 = sharedData0 + "0";
            SSharedData.SetSharedString(pAgent, key0, sharedData0);

            SharpHelper.GetSharedData(pAgent, key1, GetClassType<string>.ID);
            string sharedData1 = SharpHelper.GetFromBufferString();
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
            var keya = SharpHelper.GetTypeKeyByName("II0", GetClassType<int>.VecID);

            SharpHelper.GetSharedData(pAgent, keya, GetClassType<int>.VecID);
            SArrayInt arr = SSharedData.GetSharedArray<int>(pAgent, keya) as SArrayInt;
            //arr.Clear();
            arr.PushBack(100);
            SharpHelper.SetSharedData(pAgent, keya, GetClassType<int>.VecID);

            if (m_Array0 is SArrayVaraible)
            {
                SArrayVaraible av = m_Array0 as SArrayVaraible;
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

            //this.LogVariable(m_String0, false);
            SharpHelper.LogVariable(pNode, m_Int0.Ptr, false);
            //this.LogVariable(m_Entity0, false);
            SharpHelper.LogVariable(pNode, m_Array0.Ptr, false);

            return NodeState.NS_SUCCESS;
        }

        static int counter = 0;
    }
}
