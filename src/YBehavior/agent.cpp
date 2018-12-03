#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/shareddataex.h"
#include "YBehavior/registerdata.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/runningcontext.h"
#include "YBehavior/memory.h"

YBehavior::UINT64 YBehavior::Agent::s_UID = 0;

YBehavior::RegisterData* YBehavior::Agent::GetRegister()
{
	if (m_RegisterData == nullptr)
		m_RegisterData = new RegisterData();
	return m_RegisterData;
}

bool YBehavior::Agent::SetTree(const STRING& name)
{
	UnloadTree();

	m_Tree = TreeMgr::Instance()->GetTree(name);
	if (!m_Tree)
		return false;
	//m_Tree->CloneData(*m_SharedData);
	m_Tree->CloneDataTo(*m_Memory->GetMainData());
	return true;
	//TreeKeyMgr::Instance()->SetActiveTree(m_Tree->GetNameKeyMgr(), false);
}

void YBehavior::Agent::UnloadTree()
{
	if (m_Tree)
	{
		ClearRC();
		TreeMgr::Instance()->ReturnTree(m_Tree, true);
		m_Tree = nullptr;

		///> m_SharedData will be written by new tree, or be deleted at destruction
	}
}

void YBehavior::Agent::Tick()
{
	if (m_Tree)
	{
		//TreeKeyMgr::Instance()->SetActiveTree(m_Tree->GetNameKeyMgr(), false);
		m_Tree->RootExecute(this, m_RunningContexts.empty() ? NS_INVALID : NS_RUNNING);
	}
}

void YBehavior::Agent::ProcessRegister()
{
	_OnProcessRegister();
	m_RegisterData->GetSendData().Clear();
}

YBehavior::RunningContext* YBehavior::Agent::PopRC()
{
	RunningContext* context = nullptr;
	if (!m_RunningContexts.empty())
	{
		context = m_RunningContexts.top();
		m_RunningContexts.pop();
	}
	return context;
}

void YBehavior::Agent::PushRC(RunningContext* context)
{
	m_RunningContexts.push(context);
}

void YBehavior::Agent::ClearRC()
{
	while (!m_RunningContexts.empty())
	{
		RunningContext* context = m_RunningContexts.top();
		m_RunningContexts.pop();
		delete context;
	}
}

YBehavior::Agent::Agent(Entity* entity)
	: m_Tree(nullptr)
	, m_RegisterData(nullptr)
	, m_Entity(entity)
{
	m_UID = ++s_UID;
	//m_SharedData = new SharedDataEx();
	m_Memory = new Memory();
}

YBehavior::Agent::~Agent()
{
	if (m_Tree)
	{
		TreeMgr::Instance()->ReturnTree(m_Tree, true);
		m_Tree = nullptr;
	}
	if (m_RegisterData)
		delete m_RegisterData;

	delete m_Memory;
	//delete m_SharedData;
	ClearRC();
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

