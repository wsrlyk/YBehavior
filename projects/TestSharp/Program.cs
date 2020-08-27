using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using YBehaviorSharp;

namespace TestSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            YBehaviorSharp.SharpHelper.LoadDataCallback = new YBehaviorSharp.LoadDataCallback(LoadData);
            YBehaviorSharp.SharpHelper.Init();
            SharpHelper.Register<XCustomAction>();

            XEntity entity = new XEntity("Hehe");

            string[] state2tree = new string[] { "Main", "Test0"};
            YBehaviorSharp.SharpHelper.SetBehavior(entity.Agent.Core, "EmptyFSM", state2tree, 2, null, 0);

            int i = 0;
            while(++i < 1000)
            {
                YBehaviorSharp.SharpHelper.Tick(entity.Agent.Core);
                System.Threading.Thread.Sleep(1000);
            }

            entity.Destroy();
            Console.Read();
        }

        static string LoadData(string treename)
        {
            FileStream fileStream = new FileStream(treename, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, bytes.Length);
            fileStream.Close();

            string str = System.Text.Encoding.UTF8.GetString(bytes);
            return str;
        }
    }

    /// <summary>
    /// Entity used in Real Game
    /// </summary>
    class XEntity
    {
        XSEntity m_SEntity;
        XSAgent m_SAgent;
        public XSAgent Agent { get { return m_SAgent; } }
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

    public class XCustomAction : SBehaviorNode
    {
        SVariableString m_String0;
        SVariableString m_String1;

        public XCustomAction()
        {
            m_OnLoadCallback = new OnNodeLoaded(OnNodeLoaded);
            m_OnUpdateCallback = new OnNodeUpdate(OnNodeUpdate);
            m_Name = "XCustomAction";
        }

        protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_String0 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "String0", pData) as SVariableString;
            if (m_String0 == null)
                return false;
            m_String1 = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "String1", pData) as SVariableString;
            if (m_String1 == null)
                return false;
            return true;
        }

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("XCustomAction Update");

            XSAgent agent = YBehaviorSharp.SPtrMgr.Instance.Get(pAgent) as XSAgent;
            if (agent == null)
                return NodeState.NS_FAILURE;

            var key0 = SharpHelper.GetTypeKeyByName("S0", GetClassType<string>.ID);
            var key1 = SharpHelper.GetTypeKeyByName("S1", GetClassType<string>.ID);

            SharpHelper.GetSharedData(pAgent, key0, GetClassType<string>.ID);
            string sharedData0 = SharpHelper.GetFromBufferString();
            sharedData0 = sharedData0 + "0";
            SharpHelper.SetToBufferString(sharedData0);
            SharpHelper.SetSharedData(pAgent, key0, GetClassType<string>.ID);

            SharpHelper.GetSharedData(pAgent, key1, GetClassType<string>.ID);
            string sharedData1 = SharpHelper.GetFromBufferString();
            sharedData1 = sharedData1 + "1";
            SharpHelper.SetToBufferString(sharedData1);
            SharpHelper.SetSharedData(pAgent, key1, GetClassType<string>.ID);

            string name0 = m_String0.Get(pAgent);
            //name = (agent.Entity as XSEntity).GetEntity.Name;
            string name1 = m_String1.Get(pAgent);
            m_String0.Set(pAgent, name1);
            m_String1.Set(pAgent, name0);

            Console.WriteLine(string.Format("0: {0}, 1: {1}", name0, name1));
            return NodeState.NS_SUCCESS;
        }

    }
}
