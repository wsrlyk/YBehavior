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


	bool MachineMgr::GetFSM(const ProcessKey& key, FSM* &pFSM, MachineID* &id)
	{
		id = GetFSMID(key);
		if (id != nullptr)
		{
			auto it = m_Machines.find(id->GetName());
			if (it == m_Machines.end())
				return false;
			pFSM = it->second->GetLatestFSM();
			return pFSM != nullptr;
		}
		return false;
	}

	void _BuildID(FSM* pFSM, MachineID* id);

	MachineID* MachineMgr::GetFSMID(const ProcessKey& key)
	{
		MachineInfo* info = nullptr;
		MachineID* id = nullptr;
		FSM* pFSM = nullptr;
		auto it = m_Machines.find(key.machineName);
		if (it != m_Machines.end())
		{
			info = it->second;
			pFSM = info->GetLatestFSM();

			auto it2 = m_MachineIDs.find(key.machineName);
			if (it2 != m_MachineIDs.end())
			{
				for (auto it3 = it2->second.begin(); it3 != it2->second.end(); ++it3)
				{
					id = *it3;
					if (!id->IsSame(key.stateTrees))
						continue;
					return id;
				}
			}
		}
		else
		{
			///> No such a machine, try to load it first.
			info = new MachineInfo();
			m_Machines[key.machineName] = info;
		}

		if (!pFSM)
		{
			pFSM = _LoadFSM(key.machineName);
			if (!pFSM)
				return nullptr;
			info->SetLatestFSM(pFSM);
		}

		id = new MachineID(key.machineName);
		if (key.stateTrees != nullptr)
			id->SetMappings(*key.stateTrees);
		m_MachineIDs[key.machineName].push_back(id);


		info->ChangeReferenceCount(true);

		_BuildID(pFSM, id);

		return id;
	}

	FSM * MachineMgr::_LoadFSM(const STRING& name)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result = doc.load_file((m_WorkingDir + name + ".xml").c_str());
		LOG_BEGIN << "Loading: " << name << ".xml" << LOG_END;
		if (result.status)
		{
			ERROR_BEGIN << "Load result: " << result.description() << ERROR_END;
			return nullptr;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;
		
		FSM* pFSM = new FSM(name);
		StateMachine* pMachine = pFSM->CreateMachine();
		if (!_LoadMachine(pMachine, rootData.first_child()))
		{
			ERROR_BEGIN << "Load FSM Failed: " << name << ERROR_END;
			delete pFSM;
			return nullptr;
		}
		return pFSM;
	}

	MachineState* _LoadState(
		StateMachine* pMachine, 
		const pugi::xml_node& node,
		std::unordered_map<STRING, MachineState*>& states,
		std::unordered_set<STRING>& unusedStates
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

		if (type == MST_Entry || type == MST_Exit)
		{
			pMachine->SetSpecialState(pState);
		}
		else
		{
			if (unusedStates.insert(name).second)
			{
				states[name] = pState;
			}
			else
			{
				ERROR_BEGIN << "Duplicate State Name " << name << ERROR_END;
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
		const pugi::xml_node& node,
		std::unordered_map<STRING, MachineState*>& states,
		std::unordered_set<STRING>& unusedStates
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
		auto it2 = states.find(to);
		if (it2 == states.end())
		{
			ERROR_BEGIN << "Cant find ToState " << to << " when processing Trans " << name << ERROR_END;
			return false;
		}
		MachineState* pTo = it2->second;

		MachineState* pFrom = nullptr;
		it = node.attribute("From");
		if (!it.empty())
		{
			STRING from(it.value());
			it2 = states.find(from);
			if (it2 == states.end())
			{
				ERROR_BEGIN << "Cant find FromState " << from << " when processing Trans " << name << ERROR_END;
				return false;
			}
			pFrom = it2->second;
		}

		TransitionMapKey key;
		key.fromState = pFrom;
		key.trans = name;
		TransitionMapValue value;
		value.toState = pTo;

		pMachine->InsertTrans(key, value);

		unusedStates.erase(pTo->GetName());
		if (pFrom)
			unusedStates.erase(pFrom->GetName());

		return true;
	}

	bool MachineMgr::_LoadMachine(StateMachine* pMachine, const pugi::xml_node& data)
	{
		if (pMachine == nullptr)
		{
			ERROR_BEGIN << "NULL machine when load " << data.name() << ERROR_END;
			return false;
		}

		std::unordered_map<STRING, MachineState*> states;
		std::unordered_set<STRING> unusedStates;
		FSMUIDType subMachineCount = 0;
		bool bErr = false;
		int stateNum = 0;
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			if (strcmp(it->name(), "State") == 0)
			{
				MachineState* pState = _LoadState(pMachine, *it, states, unusedStates);
				if (!pState)
				{
					ERROR_BEGIN << "Load State failed" << ERROR_END;
					bErr = true;
					break;
				}

				pState->SetSortValue(stateNum++);

				if (pState->GetType() == MST_Meta)
				{
					StateMachine* pSubMachine = new StateMachine(
						pMachine->GetUID().Layer,
						pMachine->GetUID().Level + 1,
						++subMachineCount);
					((MetaState*)pState)->SetMachine(pSubMachine);

					if (!_LoadMachine(pSubMachine, it->first_child()))
					{
						ERROR_BEGIN << "Load SubMachine Failed." << ERROR_END;
						bErr = true;
						break;
					}
				}
			}
			else if (strcmp(it->name(), "Trans") == 0)
			{
				if (!_LoadTrans(pMachine, *it, states, unusedStates))
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
				STRING def(it.value());
				auto it2 = states.find(def);
				if (it2 == states.end())
				{
					ERROR_BEGIN << "Cant find DefaultState " << def << " when processing Machine" << ERROR_END;
					return false;
				}
				pMachine->SetDefault(it2->second);
				unusedStates.erase(def);
			}
		}

		if (bErr)
		{
			for (auto it = states.begin(); it != states.end(); ++it)
			{
				delete it->second;
			}
			return false;
		}

		if (unusedStates.size() > 0)
		{
			LOG_BEGIN << "There are some states that are never used.";
			for (auto it = unusedStates.begin(); it != unusedStates.end(); ++it)
			{
				auto it2 = states.find(*it);
				if (it2 != states.end())
				{
					delete it2->second;
					LOG_BEGIN << " " << *it;
				}
			}
			LOG_BEGIN << LOG_END;
		}

		pMachine->OnLoadFinish();
		return true;
	}

	void _BuildID(FSM* pFSM, MachineID* id)
	{
		StateMachine* pMachine = pFSM->GetMachine();
		std::list<StateMachine*> l;
		l.push_back(pMachine);
		
		id->GetStateTreeMap().clear();

		while (!l.empty())
		{
			StateMachine* pCur = l.front();
			l.pop_front();

			if (pCur == nullptr)
				continue;

			std::vector<MachineState *>& allStates = pCur->GetAllStates();
			for (auto it = allStates.begin(); it != allStates.end(); ++it)
			{
				MachineState* pState = *it;
				STRING outName;
				if (pState->GetTree() != ""  && pState->GetIdentification() != "")
				{
					id->TryGet(pState->GetIdentification(), pState->GetTree(), outName);
					id->GetStateTreeMap()[pState->GetUID().Value] = outName;
				}

				if (pState->GetType() == MST_Meta)
				{
					l.push_back(((MetaState*)pState)->GetMachine());
				}
			}
		}

		id->BuildID();
	}
}
