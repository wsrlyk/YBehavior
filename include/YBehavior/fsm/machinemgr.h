#ifndef _YBEHAVIOR_MACHINEMGR_H_
#define _YBEHAVIOR_MACHINEMGR_H_

#include "YBehavior/types.h"
#include "YBehavior/singleton.h"
#include <unordered_map>
#include "statemachine.h"
#include "YBehavior/behaviorid.h"

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
	typedef std::unordered_map<FSMUIDType, STRING> StateTreeMapType;
	class MachineID : public BehaviorID
	{
		StateTreeMapType m_StateTreeMap;
	public:
		MachineID(const STRING& name) : BehaviorID(name) {}
		inline StateTreeMapType& GetStateTreeMap() { return m_StateTreeMap; }
	};

	struct MachineVersion
	{
		int version = -1;
		FSM* pFSM = nullptr;
		int agentReferenceCount = 0;
		int GetReferenceCount() { return agentReferenceCount; }
	};

	class MachineInfo
	{
	public:
		MachineInfo();
		~MachineInfo();

		void TryRemoveVersion(MachineVersion* version);
		void RemoveVersion(MachineVersion* version);
		MachineVersion* CreateVersion();
		void RevertVersion();
		FSM* GetLatestFSM() { return m_LatestVersion ? m_LatestVersion->pFSM : nullptr; }
		inline MachineVersion* GetLatestVersion() { return m_LatestVersion; }
		void IncreaseLatestVesion();

		void SetLatestFSM(FSM* pFSM);
		void ChangeReferenceCount(bool bInc, MachineVersion* version = nullptr);
		void Print();

		inline std::unordered_map<int, MachineVersion*>& GetVersions() { return m_MachineVersions; }
	private:
		MachineVersion * m_LatestVersion;
		MachineVersion* m_PreviousVersion;
		std::unordered_map<int, MachineVersion*> m_MachineVersions;
	};

	struct ProcessKey;

	class YBEHAVIOR_API MachineMgr
	{
		std::unordered_map<MachineID*, MachineInfo*> m_Machines;
		std::unordered_map<STRING, StdVector<MachineID*>> m_MachineIDs;

		STRING m_WorkingDir;

	public:
		~MachineMgr();

		bool GetFSM(const ProcessKey& key, FSM* &pFSM, MachineID* &id);
		void SetWorkingDir(const STRING& dir);
		void ReturnFSM(FSM* pFSM);
	protected:
		FSM * _LoadFSM(MachineID* id);
		bool _CreateSpecialStates(StateMachine* pMachine);
		bool _LoadMachine(StateMachine* pMachine, const pugi::xml_node& data, int levelMachineCount[]);
	};
}

#endif