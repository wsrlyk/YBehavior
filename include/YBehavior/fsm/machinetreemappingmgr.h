#ifndef _YBEHAVIOR_MACHINETREEMAPPINGMGR_H_
#define _YBEHAVIOR_MACHINETREEMAPPINGMGR_H_

#include "YBehavior/types.h"
#include "YBehavior/singleton.h"
#include <unordered_map>
#include "machinetreemapping.h"

///>///////////////////////////////////////////////////////////////////////
///> Machine->MacTreeMap->Tree
///> We wont support SubMachineNode like SubTreeNode in Tree, so all info about a machine is in a single
///> file, with external params. And the mapping beween machines and trees is put in a separate class,
///> thus when trees change or mappings change, the machine which is focused on the relations 
///> of the states, will keep unchanged.

namespace YBehavior
{
	struct MachineTreeMappingVersion
	{
		int version = -1;
		MachineTreeMapping* pMapping = nullptr;
		int agentReferenceCount = 0;
		int GetReferenceCount() { return agentReferenceCount; }
	};

	class MachineTreeMappingInfo
	{
	public:
		MachineTreeMappingInfo();
		~MachineTreeMappingInfo();

		void TryRemoveVersion(MachineTreeMappingVersion* version);
		void RemoveVersion(MachineTreeMappingVersion* version);
		MachineTreeMappingVersion* CreateVersion();
		void RevertVersion();
		MachineTreeMapping* GetLatest() { return m_LatestVersion ? m_LatestVersion->pMapping : nullptr; }
		inline MachineTreeMappingVersion* GetLatestVersion() { return m_LatestVersion; }
		void IncreaseLatestVesion();

		void SetLatest(MachineTreeMapping* pMapping);
		void ChangeReferenceCount(bool bInc, MachineTreeMappingVersion* version = nullptr);
		void Print();

		inline std::unordered_map<int, MachineTreeMappingVersion*>& GetVersions() { return m_MachineTreeMappingVersions; }
	private:
		MachineTreeMappingVersion * m_LatestVersion;
		MachineTreeMappingVersion* m_PreviousVersion;
		std::unordered_map<int, MachineTreeMappingVersion*> m_MachineTreeMappingVersions;
	};

	struct ProcessKey;
	class YBEHAVIOR_API MachineTreeMappingMgr
	{
		std::unordered_map<MachineTreeMappingID*, MachineTreeMappingInfo*> m_Mappings;
		std::unordered_map<STRING, StdVector<MachineTreeMappingID*>> m_MappingIDs;

	public:
		MachineTreeMapping* GetMapping(const ProcessKey& key);
		void ReturnMapping(MachineTreeMapping* pMapping);
		~MachineTreeMappingMgr();
	};
}

#endif