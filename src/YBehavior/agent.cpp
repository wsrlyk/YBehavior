#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"


void YBehavior::Agent::SetTree(const STRING& name)
{
	m_Tree = TreeMgr::Instance()->GetTree(name);
	m_Tree->CloneData(m_SharedData);
}

void YBehavior::Agent::Tick()
{
	if (m_Tree)
		m_Tree->Execute(this);
}

YBehavior::Agent::Agent()
	: m_Tree(nullptr)
{

}
