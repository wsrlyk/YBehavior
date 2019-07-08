#ifndef _YBEHAVIOR_BEHAVIORPROCESS_H_
#define _YBEHAVIOR_BEHAVIORPROCESS_H_

#include "YBehavior/types.h"
#include "fsm/statemachine.h"
#include "memory.h"

namespace YBehavior
{
	struct ProcessKey
	{
		STRING machineName;
		const StdVector<STRING>* stateTrees;
		const StdVector<STRING>* subTrees;

		ProcessKey()
			: machineName("")
			, stateTrees(nullptr)
			, subTrees(nullptr)
		{}
	};

	struct BehaviorProcess
	{
		MachineContext machineContext;
		Memory memory;
	};

	class BehaviorProcessHelper
	{
	public:
		static bool GetBehaviorProcess(const ProcessKey& key, BehaviorProcess& behaviorProcess);
		static void Release(BehaviorProcess& behaviorProcess);
		static void Execute(AgentPtr pAgent);
	};
}

#endif