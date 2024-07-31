using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using YBehaviorSharp;

namespace TestSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            YBehaviorSharp.SharpHelper.OnLogCallback = ShowLog;
            YBehaviorSharp.SharpHelper.OnErrorCallback = ShowLog;
            YBehaviorSharp.SharpHelper.OnThreadLogCallback = ShowLog;
            YBehaviorSharp.SharpHelper.OnThreadErrorCallback = ShowLog;
            SharpHelper.OnGetFilePathCallback = GetFilePath;
            YBehaviorSharp.SharpHelper.Init();
            SharpHelper.Register<XCustomAction>();

            XEntity entity0 = new XEntity("Hehe");
            XEntity entity1 = new XEntity("Haha");

            Scene.Instance.entities[0] = entity0;
            Scene.Instance.entities[1] = entity1;

            string[] state2tree = new string[] { "Main", "Test0"};
            YBehaviorSharp.SharpHelper.SetBehavior(entity0.Agent.Core, "EmptyFSM", state2tree, 2, null, 0);
            YBehaviorSharp.SharpHelper.SetSharedDataByString(entity0.Agent.Core, "II0", "1342^32^643", '^');

            int i = 0;
            while (++i > 0)
            {
                YBehaviorSharp.SharpHelper.Tick(entity0.Agent.Core);
                System.Threading.Thread.Sleep(1000);
            }

            entity0.Destroy();
            entity1.Destroy();
            Console.Read();
        }

        static void ShowLog()
        {
            string data = YBehaviorSharp.SUtility.GetFromBufferString();
            Console.WriteLine(data);
        }

        static void GetFilePath()
        {
            string filename = YBehaviorSharp.SUtility.GetFromBufferString();
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
    class XEntity
    {
        XSEntity m_SEntity;
        XSAgent m_SAgent;
        public XSAgent Agent { get { return m_SAgent; } }
        public XSEntity Entity { get { return m_SEntity; } }
        string m_Name;
        public string Name { get { return m_Name; } }

        public XEntity(string name)
        {
            m_Name = name;
            m_SEntity = new XSEntity(this);
            m_SAgent = new XSAgent(m_SEntity);
        }

        public void Destroy()
        {
            m_SAgent.Dispose();
            m_SEntity.Dispose();
            m_SEntity = null;
            m_SAgent = null;
        }
    }

    /// <summary>
    /// XEntity can be reached by this in an AINode
    /// </summary>
    class XSEntity : SEntity
    {
        XEntity m_Entity;
        public XEntity GetEntity { get { return m_Entity; } }

        public XSEntity(XEntity entity)
        {
            m_Entity = entity;
        }
    }

    class XSAgent : SAgent
    {
        public XSAgent(XSEntity entity)
            : base(entity)
        {

        }
    }

    public class XCustomAction : STreeNode
    {
        //SVariableString m_String0;
        SVariableInt m_Int0;

        //SVariableEntity m_Entity0;

        SVariable m_Array0;

        public XCustomAction()
        {
            m_Name = "XCustomAction";
        }

        protected override bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            //m_String0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "String0", pData) as SVariableString;

            m_Int0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Int0", pData) as SVariableInt;

            //m_Entity0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Entity0", pData) as SVariableEntity;

            m_Array0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Array0", pData);

            if (YBehaviorSharp.SharpHelper.TryGetValue(pNode, "Type", pData))
            {
                string s = SUtility.GetFromBufferString();
                Console.WriteLine(string.Format("Type: {0}", s));
            }
            return true;
        }

        protected override NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine();
            Console.WriteLine("XCustomAction Update");

            //this.LogVariable(m_String0, true);
            this.LogVariable(m_Int0, true);
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
            string sharedData1 = SUtility.GetFromBufferString();
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
            this.LogVariable(m_Int0, false);
            //this.LogVariable(m_Entity0, false);
            this.LogVariable(m_Array0, false);

            return NodeState.NS_SUCCESS;
        }

        static int counter = 0;
    }
}
