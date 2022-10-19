#include "YBehavior/agent.h"
#include "YBehavior/registerdata.h"
#ifdef YPROFILER
#include "YBehavior/profile/profilehelper.h"
#endif

YBehavior::UINT64 YBehavior::Agent::s_UID = 0;

YBehavior::RegisterData* YBehavior::Agent::GetRegister()
{
	if (m_RegisterData == nullptr)
		m_RegisterData = new RegisterData();
	return m_RegisterData;
}

//bool YBehavior::Agent::SetTree(const STRING& name, const std::vector<STRING>* subs)
//{
//	//UnloadTree();
//
//	//m_Tree = TreeMgr::Instance()->GetTree(name, subs);
//	//if (!m_Tree)
//	//	return false;
//	//m_Tree->CloneDataTo(*m_Memory->GetMainData());
//	return true;
//}

bool YBehavior::Agent::SetBehavior(const BehaviorKey& key)
{
	UnloadBehavior();

	if (!BehaviorProcessHelper::GetBehaviorProcess(key, m_Process))
		return false;

	return true;
}

//void YBehavior::Agent::UnloadTree()
//{
//	if (m_Tree)
//	{
//		ClearRC();
//		Mgrs::Instance()->GetTreeMgr()->ReturnTree(m_Tree, true);
//		m_Tree = nullptr;
//
//		///> m_SharedData will be written by new tree, or be deleted at destruction
//	}
//}

void YBehavior::Agent::UnloadBehavior()
{
	BehaviorProcessHelper::Release(m_Process);
}

void YBehavior::Agent::Tick()
{
	//if (m_Tree)
	//{
	//	m_Tree->RootExecute(this, m_RunningContexts.empty() ? NS_INVALID : NS_RUNNING);
	//}
#ifdef YPROFILER
	Profiler::AgentProfileHelper helper(this);
#endif
	BehaviorProcessHelper::Execute(this);
}

void YBehavior::Agent::ProcessRegister()
{
	_OnProcessRegister();
	m_RegisterData->GetSendData().Clear();
}

YBehavior::Agent::Agent(Entity* entity)
	//: m_Tree(nullptr)
	: m_RegisterData(nullptr)
	, m_Entity(entity)
{
	m_UID = ++s_UID;

	//m_Memory = new Memory();
	//m_pMachineContext = new MachineContext();
}

YBehavior::Agent::~Agent()
{
	//UnloadTree();
	UnloadBehavior();
	if (m_RegisterData)
		delete m_RegisterData;

	//delete m_Memory;
	//delete m_pMachineContext;
	//delete m_SharedData;
}

YBehavior::Entity::Entity()
{
	m_Wrapper = nullptr;
}

YBehavior::Entity::~Entity()
{
	if (m_Wrapper)
	{
		m_Wrapper->SetValid(false);
		delete m_Wrapper;
	}
}

YBehavior::STRING YBehavior::Entity::ToString() const
{
	return Utility::StringEmpty;
}

const YBehavior::EntityWrapper& YBehavior::Entity::GetWrapper()
{
	if (m_Wrapper == nullptr)
		m_Wrapper = new EntityWrapper(this);

	return *m_Wrapper;
}

