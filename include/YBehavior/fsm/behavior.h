#ifndef _YBEHAVIOR_BEHAVIOR_H_
#define _YBEHAVIOR_BEHAVIOR_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"
#include <unordered_map>

namespace YBehavior
{
	class BehaviorTree;
	class FSM;
	class Memory;

	typedef std::unordered_map<NodePtr, BehaviorTree*> Node2TreeMapType;
	class Behavior
	{
		void* m_Version;
		UINT m_ID;

		FSM* m_pFSM;
		Node2TreeMapType m_Node2TreeMapping;
		///> Merged memory for all trees
		Memory* m_pMemory;

	public:
		Behavior();
		~Behavior();
		inline void SetVersion(void* version) { m_Version = version; }
		inline void* GetVersion() const { return m_Version; }
		inline void SetID(UINT id) { m_ID = id; }
		inline UINT GetID() const { return m_ID; }
		inline UINT GetKey() const { return m_ID; }
		Node2TreeMapType& GetTreeMapping() { return m_Node2TreeMapping; }
		BehaviorTree* GetMappedTree(NodePtr pNode);
		inline void SetFSM(FSM* pFSM) { m_pFSM = pFSM; }
		inline FSM* GetFSM() { return m_pFSM; }
		inline Memory* GetMemory() { return m_pMemory; }
	};

}

#endif