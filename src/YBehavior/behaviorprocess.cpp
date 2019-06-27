#include "YBehavior/behaviorprocess.h"
#include "YBehavior/logger.h"
#include "YBehavior/fsm/machinetreemappingmgr.h"

namespace YBehavior
{
	bool BehaviorProcessHelper::GetBehaviorProcess(const ProcessKey& key, BehaviorProcess& behaviorProcess)
	{
		MachineTreeMapping* pMapping = MachineTreeMappingMgr::Instance()->GetMapping(key);
		behaviorProcess.machineContext.SetMapping(pMapping);

		behaviorProcess.memory.GetMainData()->CloneFrom(*pMapping->GetMemory()->GetMainData());

		return true;
	}
}
