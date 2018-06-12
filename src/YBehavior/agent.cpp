#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/shareddataex.h"

YBehavior::UINT YBehavior::Agent::s_UID = 0;

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

	delete m_SharedData;
}
