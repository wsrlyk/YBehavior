#ifndef _YBEHAVIOR_BEHAVIORPROCESS_H_
#define _YBEHAVIOR_BEHAVIORPROCESS_H_

#include "YBehavior/types/types.h"
#include "YBehavior/memory.h"
#include "YBehavior/fsm/context.h"
#include <set>
#include "YBehavior/eventqueue.h"
namespace YBehavior
{
	struct BehaviorKey
	{
		BehaviorKey(const STRING& machinename, const StdVector<STRING>* pStateTrees, const StdVector<STRING>* pSubTrees);

		inline const STRING& Name() const { return machineName; }
		inline const StdVector<STRING>* StateTrees() const { return stateTrees; }
		inline const StdVector<STRING>* SubTrees() const { return subTrees; }
		inline UINT Hash() const { return m_hash; }
	private:
		STRING machineName;
		const StdVector<STRING>* stateTrees;
		const StdVector<STRING>* subTrees;
		UINT m_hash;
	};

	struct BehaviorProcess
	{
		MachineContext machineContext;
		TreeContext treeContext;
		Memory memory;
		Behavior* pBehavior{};
		EventQueue eventQueue{};
	};

	class BehaviorProcessHelper
	{
	public:
		static bool GetBehaviorProcess(const BehaviorKey& key, BehaviorProcess& behaviorProcess);
		static void Release(BehaviorProcess& behaviorProcess);
		static void Execute(AgentPtr pAgent);
		static void ReloadTree(const STRING& name);
		static void ReloadMachine(const STRING& name);
		static void ReloadAll();

		static void Load(const std::set<STRING>& fsmNames, const std::set<STRING>& treeNames);
	};
}

#endif