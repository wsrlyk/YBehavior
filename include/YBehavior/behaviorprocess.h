#ifndef _YBEHAVIOR_BEHAVIORPROCESS_H_
#define _YBEHAVIOR_BEHAVIORPROCESS_H_

#include "YBehavior/types.h"
#include "fsm/statemachine.h"
#include "memory.h"
#include "fsm/context.h"

namespace YBehavior
{
	struct BehaviorKey
	{
		BehaviorKey(const STRING& machinename, const StdVector<STRING>* pStateTrees, const StdVector<STRING>* pSubTrees)
			: machineName(machinename)
			, stateTrees(pStateTrees)
			, subTrees(pSubTrees)
		{
			std::stringstream ss;
			ss << machineName;
			ss << Utility::ListSpliter;
			if (stateTrees)
			{
				for (auto& it : *stateTrees)
				{
					ss << it << Utility::SequenceSpliter;
				}
			}
			ss << Utility::ListSpliter;
			if (subTrees)
			{
				for (auto& it : *subTrees)
				{
					ss << it << Utility::SequenceSpliter;
				}
			}

			m_hash = Utility::Hash(ss.str());
		}

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
		Memory memory;
		Behavior* pBehavior{};
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
	};
}

#endif