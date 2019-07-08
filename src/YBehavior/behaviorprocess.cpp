#include "YBehavior/behaviorprocess.h"
#include "YBehavior/logger.h"
#include "YBehavior/fsm/machinetreemappingmgr.h"
#include "YBehavior/agent.h"
#include "YBehavior/mgrs.h"

namespace YBehavior
{
	bool BehaviorProcessHelper::GetBehaviorProcess(const ProcessKey& key, BehaviorProcess& behaviorProcess)
	{
		MachineTreeMapping* pMapping = Mgrs::Instance()->GetMappingMgr()->GetMapping(key);
		if (!pMapping)
			return false;
		behaviorProcess.machineContext.SetMapping(pMapping);

		behaviorProcess.memory.GetMainData()->CloneFrom(*pMapping->GetMemory()->GetMainData());

		return true;
	}

	void BehaviorProcessHelper::Release(BehaviorProcess& behaviorProcess)
	{
		if (behaviorProcess.machineContext.GetMapping())
		{
			Mgrs::Instance()->GetMappingMgr()->ReturnMapping(behaviorProcess.machineContext.GetMapping());
			behaviorProcess.machineContext.Reset();

			behaviorProcess.memory.GetMainData()->Clear();
			behaviorProcess.memory.GetStack().clear();
		}
	}

	void BehaviorProcessHelper::Execute(AgentPtr pAgent)
	{
		if (pAgent->GetMachineContext()->GetMapping() == nullptr)
			return;

		pAgent->GetMachineContext()->GetMapping()->GetFSM()->Update(0.0f, pAgent);
	}

}
