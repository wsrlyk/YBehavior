#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/behaviorprocess.h"
#include "YBehavior/behaviorid.h"
#include <iostream>
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/fsm/metastate.h"

namespace YBehavior
{
	void MachineInfo::Print()
	{
		for (auto it = m_MachineVersions.begin(); it != m_MachineVersions.end(); ++it)
		{
			std::cout << "version " << it->first << ", agentcount " << it->second->agentReferenceCount << std::endl;
		}
	}

	MachineInfo::MachineInfo()
		: m_LatestVersion(nullptr)
		, m_PreviousVersion(nullptr)
	{
		CreateVersion();
	}

	MachineInfo::~MachineInfo()
	{
		for (auto it = m_MachineVersions.begin(); it != m_MachineVersions.end(); ++it)
		{
			delete it->second->pFSM;
			delete it->second;
		}
		m_MachineVersions.clear();
	}

	void MachineInfo::TryRemoveVersion(MachineVersion* version)
	{
		if (version == nullptr || version->pFSM == nullptr || version->GetReferenceCount() > 0)
			return;
		RemoveVersion(version);
	}

	void MachineInfo::RemoveVersion(MachineVersion* version)
	{
		if (version)
		{
			m_MachineVersions.erase(version->version);

			if (version->pFSM)
				delete version->pFSM;

			if (m_PreviousVersion == version)
				m_PreviousVersion = nullptr;
			if (m_LatestVersion == version)
				m_LatestVersion = nullptr;
			delete version;
		}
	}

	MachineVersion* MachineInfo::CreateVersion()
	{
		MachineVersion* pVersion = new MachineVersion();
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
		m_MachineVersions[pVersion->version] = pVersion;

		return pVersion;
	}

	void MachineInfo::RevertVersion()
	{
		TryRemoveVersion(m_LatestVersion);
		m_LatestVersion = m_PreviousVersion;
		m_PreviousVersion = nullptr;
	}

	void MachineInfo::IncreaseLatestVesion()
	{
		if (m_LatestVersion == nullptr || m_LatestVersion->pFSM == nullptr)
			return;

		CreateVersion();
	}

	void MachineInfo::SetLatestFSM(FSM* pFSM)
	{
		if (m_LatestVersion == nullptr)
			CreateVersion();
		m_LatestVersion->pFSM = pFSM;
		pFSM->SetVersion(m_LatestVersion);
	}

	void MachineInfo::ChangeReferenceCount(bool bInc, MachineVersion* version)
	{
		if (version == nullptr)
			version = m_LatestVersion;
		else
		{
			auto it = m_MachineVersions.find(version->version);
			if (it != m_MachineVersions.end() && it->second != version)
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

	///>//////////////////////////////////////////////////////////////////////////////////////////
	///>//////////////////////////////////////////////////////////////////////////////////////////
	///>//////////////////////////////////////////////////////////////////////////////////////////
	MachineMgr::~MachineMgr()
	{
		for (auto it = m_Machines.begin(); it != m_Machines.end(); ++it)
		{
			delete it->first;
			delete it->second;
		}
		m_Machines.clear();
		m_MachineIDs.clear();
	}

	void MachineMgr::SetWorkingDir(const STRING& dir)
	{
		m_WorkingDir = dir;

		if (m_WorkingDir == "")
			return;

		size_t len = m_WorkingDir.length();
		if (m_WorkingDir[len - 1] != '\\' && m_WorkingDir[len - 1] != '/')
		{
			m_WorkingDir.append(1, '/');
		}
	}


	void MachineMgr::ReturnFSM(FSM* pFSM)
	{
		if (pFSM == nullptr)
			return;

		auto it = m_Machines.find(pFSM->GetID());
		if (it != m_Machines.end())
		{
			it->second->ChangeReferenceCount(false, pFSM->GetVersion());
		}

	}

	void _BuildID(FSM* pFSM, MachineID* id);

	bool MachineMgr::GetFSM(const ProcessKey& key, FSM* &pFSM, MachineID* &id)
	{
		MachineInfo* info = nullptr;
		id = nullptr;
		pFSM = nullptr;
		auto it = m_MachineIDs.find(key.machineName);
		if (it != m_MachineIDs.end())
		{
			for (auto it2 = it->second.begin(); it2 != it->second.end(); ++it2)
			{
				id = *it2;
				if (!id->IsSame(key.stateTrees))
					continue;
				auto it3 = m_Machines.find(id);
				if (it3 != m_Machines.end())
				{
					pFSM = it3->second->GetLatestFSM();
					if (pFSM)
					{
						it3->second->ChangeReferenceCount(true);
						return true;
					}
					else
						info = it3->second;

					break;
				}
			}
		}

		if (info == nullptr)
		{
			info = new MachineInfo();
			id = new MachineID(key.machineName);
			if (key.stateTrees != nullptr)
				id->SetMappings(*key.stateTrees);
			m_MachineIDs[key.machineName].push_back(id);
			m_Machines[id] = info;
		}

		if (key.stateTrees != nullptr)
			id->SetMappings(*key.stateTrees);
	
		pFSM = _LoadFSM(id);
		if (!pFSM)
			return true;

		info->SetLatestFSM(pFSM);
		info->ChangeReferenceCount(true);

		_BuildID(pFSM, id);

		return true;
	}

	FSM * MachineMgr::_LoadFSM(MachineID* id)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result = doc.load_file((m_WorkingDir + id->GetName() + ".xml").c_str());
		LOG_BEGIN << "Loading: " << id->GetName() << ".xml" << LOG_END;
		if (result.status)
		{
			ERROR_BEGIN << "Load result: " << result.description() << ERROR_END;
			return nullptr;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;
		
		FSM* pFSM = new FSM(id->GetName());
		pFSM->SetID(id);
		RootMachine* pMachine = pFSM->CreateMachine();
		///> at most 8 levels, start from 0
		int levelMachineCount[8];
		memset(levelMachineCount, 0, sizeof(int) * 8);
		if (!_LoadMachine(pMachine, rootData.first_child(), levelMachineCount))
		{
			ERROR_BEGIN << "Load FSM Failed: " << id->GetName() << ERROR_END;
			delete pFSM;
			return nullptr;
		}
		return pFSM;
	}

	MachineState* _LoadState(
		StateMachine* pMachine, 
		const pugi::xml_node& node
	)
	{
		auto attr = node.attribute("Type");
		MachineStateType type = MST_Normal;
		if (!attr.empty())
		{
			STRING s(attr.value());
			if (s == "Entry")
			{
				type = MST_Entry;
			}
			else if (s == "Exit")
			{
				type = MST_Exit;
			}
			else if (s == "Meta")
			{
				type = MST_Meta;
			}
		}

		STRING name;
		attr = node.attribute("Name");
		if (!attr.empty())
		{
			name = attr.value();
		}

		MachineState* pState;
		if (type == MST_Meta)
		{
			pState = new MetaState(name);
		}
		else
		{
			pState = new MachineState(name, type);
		}

		pState->SetParentMachine(pMachine);
		if (type == MST_Entry || type == MST_Exit)
		{
			pMachine->SetSpecialState(pState);
		}
		else
		{
			if (!pMachine->GetRootMachine()->InsertState(pState))
			{
				ERROR_BEGIN << "Insert State Failed. Maybe Duplicate State Name " << name << ERROR_END;
				delete pState;
				pState = nullptr;
			}
		}

		attr = node.attribute("Tree");
		if (!attr.empty())
		{
			pState->SetTree(attr.value());
		}

		return pState;
	}

	bool _LoadTrans(
		StateMachine* pMachine,
		const pugi::xml_node& node
	)
	{
		auto it = node.attribute("Name");
		if (it.empty())
		{
			ERROR_BEGIN << "Trans has no name: " << node.value() << ERROR_END;
			return false;
		}

		STRING name(it.value());
		it = node.attribute("To");
		if (it.empty())
		{
			ERROR_BEGIN << "Trans has no destination: " << name << ERROR_END;
			return false;
		}
		STRING to(it.value());
		MachineState* pTo = pMachine->GetRootMachine()->FindState(to);
		if (pTo == nullptr)
		{
			ERROR_BEGIN << "Cant find ToState " << to << " when processing Trans " << name << ERROR_END;
			return false;
		}

		MachineState* pFrom = nullptr;
		it = node.attribute("From");
		if (!it.empty())
		{
			STRING from(it.value());
			pFrom = pMachine->GetRootMachine()->FindState(from);
			if (pFrom == nullptr)
			{
				ERROR_BEGIN << "Cant find FromState " << from << " when processing Trans " << name << ERROR_END;
				return false;
			}
		}

		TransitionMapKey key;
		key.fromState = pFrom;
		key.trans = name;
		TransitionMapValue value;
		value.toState = pTo;

		pMachine->GetRootMachine()->InsertTrans(key, value);

		return true;
	}

	bool MachineMgr::_LoadMachine(StateMachine* pMachine, const pugi::xml_node& data, int levelMachineCount[])
	{
		if (pMachine == nullptr)
		{
			ERROR_BEGIN << "NULL machine when load " << data.name() << ERROR_END;
			return false;
		}

		bool bErr = false;
		int stateNum = 0;
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			if (strcmp(it->name(), "State") == 0)
			{
				MachineState* pState = _LoadState(pMachine, *it);
				if (!pState)
				{
					ERROR_BEGIN << "Load State failed" << ERROR_END;
					bErr = true;
					break;
				}

				pState->GetUID().Value = pMachine->GetUID().Value;
				pState->GetUID().State = ++stateNum;
				LOG_BEGIN << "Load State " << pState->ToString() << LOG_END;
				//LOG_BEGIN << "Create " << pState->ToString() << LOG_END;
				if (pState->GetType() == MST_Meta)
				{
					StateMachine* pSubMachine = new StateMachine(
						pMachine->GetUID().Layer,
						pMachine->GetUID().Level + 1,
						++levelMachineCount[pMachine->GetUID().Level]);
					pSubMachine->SetMetaState((MetaState*)pState);
					((MetaState*)pState)->SetSubMachine(pSubMachine);

					if (!_LoadMachine(pSubMachine, it->first_child(), levelMachineCount))
					{
						ERROR_BEGIN << "Load SubMachine Failed." << ERROR_END;
						bErr = true;
						break;
					}
				}
			}
			else if (strcmp(it->name(), "Trans") == 0)
			{
				if (!_LoadTrans(pMachine, *it))
				{
					ERROR_BEGIN << "Load Trans Failed." << ERROR_END;
					bErr = true;
					break;
				}
			}
		}

		if (!bErr)
		{
			auto it = data.attribute("Default");
			if (!it.empty())
			{
				MachineState* defaultState = pMachine->GetRootMachine()->FindState(it.value());
				if (!defaultState)
				{
					ERROR_BEGIN << "Cant find DefaultState " << it.value() << " when processing Machine" << ERROR_END;
					return false;
				}
				pMachine->SetDefault(defaultState);
			}
		}

		pMachine->OnLoadFinish();
		LOG_BEGIN << "Load Machine " << Utility::ToString(pMachine->GetUID()) << LOG_END;
		return true;
	}

	void _BuildID(FSM* pFSM, MachineID* id)
	{
		RootMachine* pMachine = pFSM->GetMachine();
		std::list<StateMachine*> l;
		l.push_back(pMachine);
		
		id->GetStateTreeMap().clear();

		std::vector<MachineState *>& allStates = pMachine->GetAllStates();
		for (auto it = allStates.begin(); it != allStates.end(); ++it)
		{
			MachineState* pState = *it;
			STRING outName;
			if (pState->GetTree() != ""  && pState->GetIdentification() != "")
			{
				id->TryGet(pState->GetIdentification(), pState->GetTree(), outName);
				id->GetStateTreeMap()[pState->GetUID().Value] = outName;
			}

		}

		id->BuildID();
	}
}
