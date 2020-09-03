#ifndef _YBEHAVIOR_MACHINEMGR_H_
#define _YBEHAVIOR_MACHINEMGR_H_

#include "YBehavior/types.h"
#include "YBehavior/singleton.h"
#include <unordered_map>
#include "statemachine.h"
#include "YBehavior/version.h"
#include "YBehavior/mgrs.h"

///>///////////////////////////////////////////////////////////////////////
///> Machine->MacTreeMap->Tree
///> We wont support SubMachineNode like SubTreeNode in Tree, so all info about a machine is in a single
///> file, with external params. And the mapping beween machines and trees is put in a separate class,
///> thus when trees change or mappings change, the machine which is focused on the relations 
///> of the states, will keep unchanged.

namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	typedef VersionMgr<FSM, STRING> MachineVersionMgrType;
	typedef typename MachineVersionMgrType::InfoType MachineInfoType;
	typedef typename MachineVersionMgrType::InfoType::VersionType MachineVersionType;

	class MachineMgr
	{
		MachineVersionMgrType m_VersionMgr;

		STRING m_WorkingDir;

	public:
		~MachineMgr();

		FSM* GetFSM(const STRING& key);
		void SetWorkingDir(const STRING& dir);
		void ReturnFSM(FSM* pFSM);
		void ReloadMachine(const STRING& name);
		void ReloadAll();
		void Print();
		void SetLoadDataCallback(LoadDataDelegate callback) { m_LoadDataCallback = callback; }
	
		void Clear();
	protected:
		FSM * _LoadFSM(const STRING& name);
		bool _CreateSpecialStates(StateMachine* pMachine, UINT uid);
		bool _LoadMachine(StateMachine* pMachine, const pugi::xml_node& data, UINT& uid);

		LoadDataDelegate m_LoadDataCallback = nullptr;
	};
}

#endif