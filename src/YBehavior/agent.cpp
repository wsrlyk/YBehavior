#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/shareddataex.h"
#include "YBehavior/registerdata.h"

YBehavior::UINT YBehavior::Agent::s_UID = 0;

YBehavior::RegisterData* YBehavior::Agent::GetRegister()
{
	if (m_RegisterData == nullptr)
		m_RegisterData = new RegisterData();
	return m_RegisterData;
}

void YBehavior::Agent::SetTree(const STRING& name)
{
	m_Tree = TreeMgr::Instance()->GetTree(name);
	m_Tree->CloneData(*m_SharedData);
}

void YBehavior::Agent::Tick()
{
	if (m_Tree)
		m_Tree->Execute(this);
}

YBehavior::Agent::Agent()
	: m_Tree(nullptr)
	, m_UID(++s_UID)
	, m_RegisterData(nullptr)
{
	m_SharedData = new SharedDataEx();
}

YBehavior::Agent::~Agent()
{
	if (m_Tree)
	{
		TreeMgr::Instance()->ReturnTree(m_Tree);
		m_Tree = nullptr;
	}
	if (m_RegisterData)
		delete m_RegisterData;

	delete m_SharedData;
}
