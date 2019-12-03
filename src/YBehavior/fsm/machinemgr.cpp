#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/behaviorprocess.h"
#include <iostream>
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/fsm/metastate.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	MachineMgr::~MachineMgr()
	{
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
		m_VersionMgr.Return(pFSM);
	}

	void _BuildStateTreeMapping(FSM* pFSM);

	FSM* MachineMgr::GetFSM(const STRING& name)
	{
		FSM *pFSM;
		MachineInfoType* info;
		if (!m_VersionMgr.GetData(name, pFSM, info))
		{
			pFSM = _LoadFSM(name);
			if (!pFSM)
				return nullptr;

			info->SetLatest(pFSM);
			info->ChangeReferenceCount(true);
			_BuildStateTreeMapping(pFSM);

		}

		return pFSM;
	}

	FSM * MachineMgr::_LoadFSM(const STRING& name)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result = doc.load_file((m_WorkingDir + name + ".fsm").c_str());
		LOG_BEGIN << "Loading: " << name << ".fsm" << LOG_END;
		if (result.status)
		{
			ERROR_BEGIN << "Load result: " << result.description() << ERROR_END;
			return nullptr;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;
		
		FSM* pFSM = new FSM(name);
		RootMachine* pMachine = pFSM->CreateMachine();
		///> at most 8 levels, start from 0
		UINT uid = 0;

		if (!_LoadMachine(pMachine, rootData.first_child(), uid))
		{
			ERROR_BEGIN << "Load FSM Failed: " << name << ERROR_END;
			delete pFSM;
			return nullptr;
		}
#ifdef DEBUGGER
		xml_string_writer writer;
		rootData.print(writer, PUGIXML_TEXT("\t"), pugi::format_indent | pugi::format_raw);
		writer.result.erase(std::remove_if(writer.result.begin(), writer.result.end(), ::isspace), writer.result.end());
		pFSM->SetHash(Utility::Hash(writer.result));
		LOG_BEGIN << writer.result << LOG_END;
#endif
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
		switch (type)
		{
		case YBehavior::MST_Entry:
			pState = pMachine->GetEntry();
			pState->SetName(name);
			break;
		case YBehavior::MST_Exit:
			pState = pMachine->GetExit();
			pState->SetName(name);
			break;
		case YBehavior::MST_Meta:
			pState = new MetaState(name);
			break;
		case YBehavior::MST_Normal:
			pState = new MachineState(name, type);
			break;
		default:
			break;
		}

		if (pState == nullptr)
			return nullptr;

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

		STRING name(it.value());
		it = node.attribute("To");
		if (it.empty())
		{
			ERROR_BEGIN << "Trans has no destination: " << name << ERROR_END;
			return false;
		}

		MachineState* pTo;
		if (it.empty())
		{
			///> This Trans to ExitState
			pTo = pMachine->GetExit();
		}
		else
		{
			pTo = pMachine->GetRootMachine()->FindState(it.value());
		}

		if (pTo == nullptr)
		{
			ERROR_BEGIN << "Cant find ToState " << it.value() << " when processing Trans " << name << ERROR_END;
			return false;
		}

		MachineState* pFrom = nullptr;
		it = node.attribute("From");
		if (!it.empty())
		{
			pFrom = pMachine->GetRootMachine()->FindState(it.value());
			if (pFrom == nullptr)
			{
				ERROR_BEGIN << "Cant find FromState " << it.value() << " when processing Trans " << name << ERROR_END;
				return false;
			}
		}

		TransitionMapKey key;
		key.fromState = pFrom;
		key.trans.SetConditionMgr(pMachine->GetRootMachine()->GetFSM()->GetConditionMgr());
		TransitionMapValue value;
		value.toState = pTo;

		for (auto chi = node.begin(); chi != node.end(); ++chi)
		{
			key.trans.Set(chi->name());
		}

		pMachine->GetRootMachine()->InsertTrans(key, value);

		return true;
	}

	bool MachineMgr::_CreateSpecialStates(StateMachine* pMachine)
	{
		MachineState* pState;

		pState = new MachineState(Utility::StringEmpty, MST_Entry);
		if (!pMachine->SetSpecialState(pState) || !pMachine->GetRootMachine()->InsertState(pState))
		{
			delete pState;
			return false;
		}
		pState->SetParentMachine(pMachine);

		pState = new MachineState(Utility::StringEmpty, MST_Exit);
		if (!pMachine->SetSpecialState(pState) || !pMachine->GetRootMachine()->InsertState(pState))
		{
			delete pState;
			return false;
		}
		pState->SetParentMachine(pMachine);

		return true;
	}

	bool MachineMgr::_LoadMachine(StateMachine* pMachine, const pugi::xml_node& data, UINT& uid)
	{
		if (pMachine == nullptr)
		{
			ERROR_BEGIN << "NULL machine when load " << data.name() << ERROR_END;
			return false;
		}

		if (!_CreateSpecialStates(pMachine))
		{
			ERROR_BEGIN << "Create special states failed. " << data.name() << ERROR_END;
			return false;
		}

		uid += pMachine->GetUIDOffset();

		bool bErr = false;
		///> Start from 3. 1 for EntryState, and 2 for ExitState
		int stateNum = 2;
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
				
				if (pState->GetType() != MST_Entry && pState->GetType() != MST_Exit)
				{
					pState->SetParentMachine(pMachine);

					pState->SetUID(++uid);

					if (!pMachine->GetRootMachine()->InsertState(pState))
					{
						ERROR_BEGIN << "Insert State Failed. Maybe Duplicate State Name " << pState->GetName() << ERROR_END;
						delete pState;
						pState = nullptr;
						bErr = true;
						break;
					}
				}

				LOG_BEGIN << "Load State " << pState->ToString() << LOG_END;

				if (pState->GetType() == MST_Meta)
				{
					StateMachine* pSubMachine = new StateMachine(
						uid,
						pMachine->GetLevel() + 1);
					pSubMachine->SetMetaState((MetaState*)pState);
					((MetaState*)pState)->SetSubMachine(pSubMachine);

					if (!_LoadMachine(pSubMachine, it->first_child(), uid))
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

		return true;
	}

	void MachineMgr::ReloadMachine(const STRING& name)
	{
		m_VersionMgr.Reload(name);
	}

	void MachineMgr::ReloadAll()
	{
		m_VersionMgr.ReloadAll();
	}

	void MachineMgr::Print()
	{
		std::cout << "Print all fsms" << std::endl;
		for (auto it = m_VersionMgr.GetInfos().begin(); it != m_VersionMgr.GetInfos().end(); ++it)
		{
			std::cout << it->first << std::endl;
			it->second->Print();
		}
		std::cout << "Print all fsms end." << std::endl;
	}

	void _BuildStateTreeMapping(FSM* pFSM)
	{
		RootMachine* pMachine = pFSM->GetMachine();
		
		std::vector<MachineState *>& allStates = pMachine->GetAllStates();
		for (auto it = allStates.begin(); it != allStates.end(); ++it)
		{
			MachineState* pState = *it;
			STRING outName;
			if (pState->GetName().empty())
			{
				if (!pState->GetTree().empty())
					pFSM->GetTreeMap().Node2Trees[pState] = pState->GetTree();
			}
			else
			{
				pFSM->GetTreeMap().Name2Trees[{pState, pState->GetName()}] = pState->GetTree();
			}
		}
	}
}
