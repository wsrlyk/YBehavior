#include "YBehavior/fsm/machinetreemappingmgr.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/behaviorprocess.h"
#include <iostream>
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/behaviorid.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
///>//////////////////////////////////////////////////////////////////////////////////////////
///>//////////////////////////////////////////////////////////////////////////////////////////
///>//////////////////////////////////////////////////////////////////////////////////////////

	void MachineTreeMappingInfo::Print()
	{
		for (auto it = m_MachineTreeMappingVersions.begin(); it != m_MachineTreeMappingVersions.end(); ++it)
		{
			std::cout << "version " << it->first << ", agentcount " << it->second->agentReferenceCount << std::endl;
		}
	}

	MachineTreeMappingInfo::MachineTreeMappingInfo()
		: m_LatestVersion(nullptr)
		, m_PreviousVersion(nullptr)
	{
		CreateVersion();
	}

	MachineTreeMappingInfo::~MachineTreeMappingInfo()
	{
		for (auto it = m_MachineTreeMappingVersions.begin(); it != m_MachineTreeMappingVersions.end(); ++it)
		{
			delete it->second->pMapping;
			delete it->second;
		}
		m_MachineTreeMappingVersions.clear();
	}

	void MachineTreeMappingInfo::TryRemoveVersion(MachineTreeMappingVersion* version)
	{
		if (version == nullptr || version->pMapping == nullptr || version->GetReferenceCount() > 0)
			return;
		RemoveVersion(version);
	}

	void MachineTreeMappingInfo::RemoveVersion(MachineTreeMappingVersion* version)
	{
		if (version)
		{
			m_MachineTreeMappingVersions.erase(version->version);

			if (version->pMapping)
				delete version->pMapping;

			if (m_PreviousVersion == version)
				m_PreviousVersion = nullptr;
			if (m_LatestVersion == version)
				m_LatestVersion = nullptr;
			delete version;
		}
	}

	MachineTreeMappingVersion* MachineTreeMappingInfo::CreateVersion()
	{
		MachineTreeMappingVersion* pVersion = new MachineTreeMappingVersion();
		if (m_LatestVersion == nullptr)
		{
			pVersion->version = 0;
		}
		else
		{
			pVersion->version = m_LatestVersion->version + 1;

			///> Check if the current latest version has no reference. Remove it if true
			{
				TryRemoveVersion(m_LatestVersion);
			}
		}
		m_PreviousVersion = m_LatestVersion;
		m_LatestVersion = pVersion;
		m_MachineTreeMappingVersions[pVersion->version] = pVersion;

		return pVersion;
	}

	void MachineTreeMappingInfo::RevertVersion()
	{
		TryRemoveVersion(m_LatestVersion);
		m_LatestVersion = m_PreviousVersion;
		m_PreviousVersion = nullptr;
	}

	void MachineTreeMappingInfo::IncreaseLatestVesion()
	{
		if (m_LatestVersion == nullptr || m_LatestVersion->pMapping == nullptr)
			return;

		CreateVersion();
	}

	void MachineTreeMappingInfo::SetLatest(MachineTreeMapping* pMapping)
	{
		if (m_LatestVersion == nullptr)
			CreateVersion();
		m_LatestVersion->pMapping = pMapping;
		pMapping->SetVersion(m_LatestVersion);
	}

	void MachineTreeMappingInfo::ChangeReferenceCount(bool bInc, MachineTreeMappingVersion* version)
	{
		if (version == nullptr)
			version = m_LatestVersion;
		else
		{
			auto it = m_MachineTreeMappingVersions.find(version->version);
			if (it != m_MachineTreeMappingVersions.end() && it->second != version)
				version = nullptr;
		}

		if (version == nullptr)
			return;

		if (bInc)
		{
			++(version->agentReferenceCount);
		}
		else
		{
			--(version->agentReferenceCount);

			if (m_LatestVersion != version)
			{
				///> Old version has no reference, remove it
				TryRemoveVersion(version);
			}
		}
	}

	MachineTreeMapping* _LoadNewMappingID(MachineTreeMappingID* id, const ProcessKey& key);

	MachineTreeMapping* MachineTreeMappingMgr::GetMapping(const ProcessKey& key)
	{
		MachineTreeMapping* pRes = nullptr;
		MachineTreeMappingInfo* info = nullptr;

		auto it = m_MappingIDs.find(key.machineName);
		if (it == m_MappingIDs.end())
		{
			LOG_BEGIN << "Cant Find MachineTreeMapping with MachineName " << key.machineName << LOG_END;
			return nullptr;
		}

		MachineTreeMappingID* targetID = nullptr;
		for (auto it2 = it->second.begin(); it2 != it->second.end(); ++it2)
		{
			MachineTreeMappingID* id = *it2;
			if (!id->IsSame(key))
				continue;
			targetID = id;
			auto it3 = m_Mappings.find(targetID);
			if (it3 != m_Mappings.end())
			{
				info = it3->second;
				pRes = info->GetLatest();
				if (pRes != nullptr)
					return pRes;
			}
			else
			{
				ERROR_BEGIN << "Has Mapping ID but no Mapping Info. " << ERROR_END;
			}
			break;
		}

		if (targetID == nullptr || info == nullptr)
		{
			targetID = new MachineTreeMappingID(key.machineName);
			info = new MachineTreeMappingInfo();

			m_MappingIDs[key.machineName].push_back(targetID);
			m_Mappings[targetID] = info;

		}
		else
		{
			targetID->Reset();
		}

		pRes = _LoadNewMappingID(targetID, key);

		return pRes;
	}

	MachineTreeMapping* _LoadNewMappingID(MachineTreeMappingID* id, const ProcessKey& key)
	{
		FSM* pFSM = nullptr;
		MachineID* pFSMID = nullptr;
		if (!MachineMgr::Instance()->GetFSM(key, pFSM, pFSMID))
		{
			ERROR_BEGIN << "Cant Get FSM " << key.machineName << " When Load machinetreemapping." << ERROR_END;
			return nullptr;
		}
		id->SetFSMID(pFSMID);

		MachineTreeMappingType machinetreemapping;
		for (auto it = pFSMID->GetStateTreeMap().begin(); it != pFSMID->GetStateTreeMap().end(); ++it)
		{
			BehaviorTree* tree = TreeMgr::Instance()->GetTree(it->second, key.subTrees);
			if (tree == nullptr)
			{
				ERROR_BEGIN << "Cant Get Tree " << it->second << " When Load machinetreemapping " << key.machineName << ERROR_END;
				return nullptr;
			}
			id->AddTreeID(tree->GetTreeID());
			machinetreemapping[it->first] = tree;
		}

		id->BuildID();

		MachineTreeMapping* mapping = new MachineTreeMapping();
		mapping->SetID(id);
		mapping->GetMapping() = std::move(machinetreemapping);
		return mapping;
	}
}
