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

bool YBehavior::Agent::SetTree(const STRING& name)
{
	m_Tree = TreeMgr::Instance()->GetTree(name);
	if (!m_Tree)
		return false;
	m_Tree->CloneData(*m_SharedData);
	return true;
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

YBehavior::Agent::Agent(Entity* entity)
	: m_Tree(nullptr)
	, m_RegisterData(nullptr)
	, m_Entity(entity)
{
	m_UID = ++s_UID;
	m_SharedData = new SharedDataEx();
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

	delete m_SharedData;

}

YBehavior::Entity::Entity()
{
	m_WrapperList = nullptr;
}

YBehavior::Entity::~Entity()
{
	if (m_WrapperList != nullptr)
	{
		LinkedListNode<EntityWrapper>* node = m_WrapperList->GetNext();
		while (node != nullptr)
		{
			node->GetValue().SetValid(false);
			node = node->GetNext();
		}
		delete m_WrapperList;
	}
}

YBehavior::STRING YBehavior::Entity::ToString() const
{
	return Utility::StringEmpty;
}

YBehavior::EntityWrapper YBehavior::Entity::CreateWrapper()
{
	EntityWrapper wrapper(this);

	if (m_WrapperList == nullptr)
		m_WrapperList = new	LinkedList<EntityWrapper>();

	LinkedListNode<EntityWrapper>* node = m_WrapperList->Append(wrapper);
	wrapper.SetReference(node);
	return wrapper;
}

void YBehavior::Entity::DeleteWrapper(LinkedListNode<EntityWrapper>* node)
{
	if (m_WrapperList != nullptr)
		m_WrapperList->Remove(node);
}
