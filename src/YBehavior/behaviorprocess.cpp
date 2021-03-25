#include "YBehavior/behaviorprocess.h"
#include "YBehavior/logger.h"
#include "YBehavior/fsm/behaviormgr.h"
#include "YBehavior/agent.h"
#include "YBehavior/mgrs.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/fsm/machinemgr.h"

namespace YBehavior
{
	bool BehaviorProcessHelper::GetBehaviorProcess(const BehaviorKey& key, BehaviorProcess& behaviorProcess)
	{
		Behavior* pBehavior = Mgrs::Instance()->GetBehaviorMgr()->GetBehavior(key);
		if (!pBehavior)
			return false;
		behaviorProcess.pBehavior = pBehavior;
		behaviorProcess.machineContext.Init(pBehavior);

		behaviorProcess.memory.GetMainData()->CloneFrom(*pBehavior->GetMemory()->GetMainData());

		return true;
	}

	void BehaviorProcessHelper::Release(BehaviorProcess& behaviorProcess)
	{
		if (behaviorProcess.pBehavior)
		{
			behaviorProcess.memory.GetStack().clear();
			behaviorProcess.memory.GetMainData()->Clear();

			Mgrs::Instance()->GetBehaviorMgr()->ReturnBehavior(behaviorProcess.pBehavior);
			behaviorProcess.machineContext.Reset();
			behaviorProcess.pBehavior = nullptr;
		}
	}

	void BehaviorProcessHelper::Execute(AgentPtr pAgent)
	{
		if (pAgent->GetBehavior() == nullptr)
			return;

		pAgent->GetBehavior()->GetFSM()->Update(0.0f, pAgent);
	}

	void BehaviorProcessHelper::ReloadTree(const STRING& name)
	{
		Mgrs::Instance()->GetTreeMgr()->ReloadTree(name);
		Mgrs::Instance()->GetBehaviorMgr()->ReloadTree(name);
	}

	void BehaviorProcessHelper::ReloadMachine(const STRING& name)
	{
		Mgrs::Instance()->GetMachineMgr()->ReloadMachine(name);
		Mgrs::Instance()->GetBehaviorMgr()->ReloadMachine(name);
	}

	void BehaviorProcessHelper::ReloadAll()
	{
		Mgrs::Instance()->GetTreeMgr()->ReloadAll();
		Mgrs::Instance()->GetMachineMgr()->ReloadAll();
		Mgrs::Instance()->GetBehaviorMgr()->ReloadAll();
	}

	void _GetToLoadTrees(const TreeMap* treemap, std::list<STRING>& toLoadTrees)
	{
		for (auto it : treemap->Node2Trees)
		{
			if (!it.second.empty())
				toLoadTrees.push_back(it.second);
		}
		for (auto it : treemap->Name2Trees)
		{
			if (!it.second.empty())
				toLoadTrees.push_back(it.second);
		}
	}

	void BehaviorProcessHelper::Load(const std::set<STRING>& fsmNames, const std::set<STRING>& treeNames)
	{
		std::list<STRING> toLoadTrees;
		for (const auto& s : fsmNames)
		{
			const TreeMap* treeMap = nullptr;
			if (!Mgrs::Instance()->GetMachineMgr()->LoadFSM(s, treeMap))
			{
				ERROR_BEGIN << "Cant Load FSM " << s << ERROR_END;
				return;
			}
			_GetToLoadTrees(treeMap, toLoadTrees);
		}

		for (const auto& s : treeNames)
		{
			toLoadTrees.push_back(s);
		}

		std::unordered_set<STRING> loadedTrees;

		while (!toLoadTrees.empty())
		{
			auto& treename = toLoadTrees.front();
			bool visited = !loadedTrees.insert(treename).second;

			if (!visited)
			{
				const TreeMap* treeMap = nullptr;
				if (!Mgrs::Instance()->GetTreeMgr()->LoadTree(treename, treeMap))
				{
					ERROR_BEGIN << "Cant Load Tree " << treename << ERROR_END;
					return;
				}

				_GetToLoadTrees(treeMap, toLoadTrees);
			}

			toLoadTrees.pop_front();
		}
	}

}
