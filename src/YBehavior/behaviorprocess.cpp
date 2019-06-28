#include "YBehavior/behaviorprocess.h"
#include "YBehavior/logger.h"
#include "YBehavior/fsm/machinetreemappingmgr.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	bool BehaviorProcessHelper::GetBehaviorProcess(const ProcessKey& key, BehaviorProcess& behaviorProcess)
	{
		MachineTreeMapping* pMapping = MachineTreeMappingMgr::Instance()->GetMapping(key);
		if (!pMapping)
			return false;
		behaviorProcess.machineContext.SetMapping(pMapping);

		behaviorProcess.memory.GetMainData()->CloneFrom(*pMapping->GetMemory()->GetMainData());

		return true;
	}

	void BehaviorProcessHelper::Execute(AgentPtr pAgent)
	{
		if (pAgent->GetMachineContext()->GetMapping() == nullptr)
			return;

		pAgent->GetMachineContext()->GetMapping()->GetFSM()->Update(0.0f, pAgent);
	}

}
