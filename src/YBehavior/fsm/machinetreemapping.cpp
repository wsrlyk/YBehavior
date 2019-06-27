#include "YBehavior/fsm/machinetreemapping.h"
#include "YBehavior/utility.h"
#include "YBehavior/logger.h"
#include "YBehavior/behaviorprocess.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/memory.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	bool MachineTreeMappingID::IsSame(const ProcessKey& key) const
	{
		if (m_pFSMID != nullptr && key.stateTrees != nullptr)
		{
			if (!m_pFSMID->IsSame(key.stateTrees))
				return false;
		}

		if (key.subTrees != nullptr)
		{
			for (auto it = m_pTreeIDs.begin(); it != m_pTreeIDs.end(); ++it)
			{
				if (!(*it)->IsSame(key.subTrees))
					return false;
			}
		}
		return true;
	}

	void MachineTreeMappingID::BuildID()
	{
		m_ID = 0;
		if (m_pFSMID)
			m_ID += m_pFSMID->GetID();
		for (auto it = m_pTreeIDs.begin(); it != m_pTreeIDs.end(); ++it)
		{
			m_ID += (*it)->GetID();
		}
	}

	void MachineTreeMappingID::Reset()
	{
		m_ID = 0;
		m_pFSMID = nullptr;
		m_pTreeIDs.clear();
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	MachineTreeMapping::MachineTreeMapping()
	{
		m_pMemory = new Memory();
	}

	MachineTreeMapping::~MachineTreeMapping()
	{
		delete m_pMemory;
	}

	void MachineTreeMapping::Build()
	{
		for (auto it = m_Mapping.begin(); it != m_Mapping.end(); ++it)
		{
			it->second->CloneDataTo(*m_pMemory->GetMainData());
		}
	}

	YBehavior::BehaviorTree* MachineTreeMapping::GetTree(FSMUIDType fsmuid)
	{
		auto it = m_Mapping.find(fsmuid);
		if (it == m_Mapping.end())
			return nullptr;
		return it->second;
	}

}