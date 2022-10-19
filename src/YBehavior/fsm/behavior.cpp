#include "YBehavior/fsm/behavior.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/memory.h"
#include "YBehavior/behaviortreemgr.h"

namespace YBehavior
{
	Behavior::Behavior()
		: m_Version(nullptr)
		, m_ID(0)
		, m_pFSM(nullptr)
	{
		m_pMemory = new Memory();
	}

	Behavior::~Behavior()
	{
		delete m_pMemory;

		for (auto it = m_Node2TreeMapping.begin(); it != m_Node2TreeMapping.end(); ++it)
		{
			Mgrs::Instance()->GetTreeMgr()->ReturnTree(it->second());
		}
		m_Node2TreeMapping.clear();

		if (m_pFSM)
		{
			Mgrs::Instance()->GetMachineMgr()->ReturnFSM(m_pFSM);
			m_pFSM = nullptr;
		}
	}

	YBehavior::BehaviorTree* Behavior::GetMappedTree(NodePtr pNode)
	{
		auto it = m_Node2TreeMapping.find(pNode);
		if (it != m_Node2TreeMapping.end())
			return it->second();
		return nullptr;
	}
}