#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/shareddataex.h"
#include "YBehavior/registerdata.h"
#include "YBehavior/nodefactory.h"

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

	//TreeKeyMgr::Instance()->SetActiveTree(m_Tree->GetNameKeyMgr(), false);
}

void YBehavior::Agent::Tick()
{
	if (m_Tree)
	{
		//TreeKeyMgr::Instance()->SetActiveTree(m_Tree->GetNameKeyMgr(), false);
		m_Tree->Execute(this);
	}
}

void YBehavior::Agent::ProcessRegister()
{
		_OnProcessRegister();
		m_RegisterData->GetSendData().Clear();
}

YBehavior::AgentWrapper YBehavior::Agent::CreateWrapper()
{
	AgentWrapper wrapper(this);

	if (m_WrapperList == nullptr)
		m_WrapperList = new	LinkedList<AgentWrapper>();

	LinkedListNode<AgentWrapper>* node = m_WrapperList->Append(wrapper);
	wrapper.SetReference(node);
	return wrapper;
}

void YBehavior::Agent::DeleteWrapper(LinkedListNode<AgentWrapper>* node)
{
	if (m_WrapperList != nullptr)
		m_WrapperList->Remove(node);
}

YBehavior::STRING YBehavior::Agent::ToString() const
{
	return Utility::ToString(m_UID);
}

YBehavior::Agent::Agent()
	: m_Tree(nullptr)
	, m_UID(++s_UID)
	, m_RegisterData(nullptr)
{
	m_SharedData = new SharedDataEx();
	m_WrapperList = nullptr;
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

	if (m_WrapperList != nullptr)
	{
		LinkedListNode<AgentWrapper>* node = m_WrapperList->GetNext();
		while (node != nullptr)
		{
			node->GetValue().SetValid(false);
			node = node->GetNext();
		}
		delete m_WrapperList;
	}
}
