#ifndef _YBEHAVIOR_MACHINETREEMAPPING_H_
#define _YBEHAVIOR_MACHINETREEMAPPING_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"
#include <unordered_map>

namespace YBehavior
{
	class BehaviorTree;
	class FSM;
	class Memory;
	class BehaviorID;
	class MachineID;
	struct ProcessKey;
	struct MachineTreeMappingVersion;

	class MachineTreeMappingID
	{
		STRING m_MachineName;

		UINT m_ID;
		MachineID* m_pFSMID;
		StdVector<BehaviorID*> m_pTreeIDs;
	public:
		MachineTreeMappingID(const STRING& name) : m_MachineName(name), m_ID(0) {}
		inline UINT GetID() const { return m_ID; }
		inline const STRING& GetName() const { return m_MachineName; }

		inline bool operator< (const MachineTreeMappingID& other) const { return m_ID < other.m_ID; }
		inline bool operator== (const MachineTreeMappingID& other) const { return m_ID == other.m_ID; }

		void SetFSMID(MachineID* id) { m_pFSMID = id; }
		void AddTreeID(BehaviorID* id) { m_pTreeIDs.push_back(id); }
		bool IsSame(const ProcessKey& key) const;
		void BuildID();
		void Reset();
	};

	typedef std::unordered_map<FSMUIDType, BehaviorTree*> MachineTreeMappingType;
	class MachineTreeMapping
	{
		MachineTreeMappingVersion* m_Version;
		MachineTreeMappingID* m_ID;

		FSM* m_pFSM;
		MachineTreeMappingType m_Mapping;
		///> Merged memory for all trees
		Memory* m_pMemory;

	public:
		inline void SetVersion(MachineTreeMappingVersion* version) { m_Version = version; }
		inline void SetID(MachineTreeMappingID* id) { m_ID = id; }
		MachineTreeMappingType& GetMapping() { return m_Mapping; }
	};

}

#endif