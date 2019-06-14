#ifndef _YBEHAVIOR_BEHAVIORPROCESS_H_
#define _YBEHAVIOR_BEHAVIORPROCESS_H_

#include "YBehavior/types.h"

namespace YBehavior
{
	class MachineTreeMapping;
	class MachineContext;
	class Memory;

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

	class BehaviorProcess
	{
		MachineTreeMapping* m_pMapping;
		MachineContext* m_pMachineContext;
		Memory* m_pMemory;
	};

	class BehaviorProcessHelper
	{
	public:
		static bool GetBehaviorProcess(const ProcessKey& key, BehaviorProcess& pBehaviorProcess);
	};
}

#endif