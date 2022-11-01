#include "YBehavior/fsm/behavior.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/memory.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"

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

	void Behavior::Merge(BehaviorTree* pTree)
	{
		GetMemory()->GetMainData()->MergeFrom(*pTree->GetSharedData(), true);
		m_ValidEvents.merge(pTree->GetValidEvents());
	}

	void Behavior::RegiseterEvent(UINT e, UINT count)
	{
		m_ValidEvents.insert(e, count);
	}

	UINT Behavior::IsValidEvent(UINT hash) const
	{
		auto it = m_ValidEvents.find(hash);
		if (it == m_ValidEvents.end())
			return 0;
		return it->second();
	}

}