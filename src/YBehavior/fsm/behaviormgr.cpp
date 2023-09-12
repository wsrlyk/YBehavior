#include "YBehavior/fsm/behaviormgr.h"
#include "YBehavior/fsm/behavior.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/behaviorprocess.h"
#include <iostream>
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/mgrs.h"

namespace YBehavior
{
	Behavior* _LoadNewBehavior(const BehaviorKey& key);

	Behavior* BehaviorMgr::GetBehavior(const BehaviorKey& key)
	{
		Behavior* pRes = nullptr;
		BehaviorInfoType* info = nullptr;

		if (!m_VersionMgr.GetData(key.Hash(), pRes, info))
		{
			auto v = info->GetLatestVersion();
			if (v && v->Invalid())
				return nullptr;

			pRes = _LoadNewBehavior(key);

			info->SetLatest(pRes);
			if (pRes)
				info->ChangeReferenceCount(true);
		}

		return pRes;
	}

	void _GetToLoadTrees(const TreeMap& treemap, std::list<std::tuple<NodePtr, STRING>>& toLoadTrees, small_map<STRING, STRING>& n2t)
	{
		for (auto it : treemap.Node2Trees)
		{
			if (!it.second.empty())
				toLoadTrees.emplace_back(it.first, it.second);
		}
		for (auto it : treemap.Name2Trees)
		{
			STRING name(it.second);
			auto it2 = n2t.find(std::get<1>(it.first));
			if (it2 != n2t.end())
				name = it2->second;
			if (name.empty())
			{
				//ERROR_BEGIN << "No tree for node " << std::get<1>(it.first) << ERROR_END;
				continue;
			}
			toLoadTrees.emplace_back(std::get<0>(it.first), name);
		}
	}

	Behavior* _LoadNewBehavior(const BehaviorKey& key)
	{
		if (key.StateTrees() && key.StateTrees()->size() % 2 != 0)
		{
			ERROR_BEGIN << "Even amount of inputs is required. But now it's " << key.StateTrees()->size() << " When Load Behavior " << key.Name() << ERROR_END;
			return nullptr;
		}
		if (key.SubTrees() && key.SubTrees()->size() % 2 != 0)
		{
			ERROR_BEGIN << "Even amount of inputs is required. But now it's " << key.SubTrees()->size() << " When Load Behavior " << key.Name() << ERROR_END;
			return nullptr;
		}
		small_map<STRING, STRING> m2t;
		small_map<STRING, STRING> t2t;
		if (key.StateTrees())
		{
			for (UINT i = 0; i < key.StateTrees()->size(); i += 2)
			{
				m2t[(*key.StateTrees())[i]] = (*key.StateTrees())[i + 1];
			}
		}
		if (key.SubTrees())
		{
			for (UINT i = 0; i < key.SubTrees()->size(); i += 2)
			{
				t2t[(*key.SubTrees())[i]] = (*key.SubTrees())[i + 1];
			}
		}

		//////////////////////////////////////////////////////////////////////////
		std::unique_ptr<Behavior> behavior(new Behavior());
		FSM* pFSM = Mgrs::Instance()->GetMachineMgr()->GetFSM(key.Name());
		if (!pFSM)
		{
			ERROR_BEGIN << "Cant Get FSM " << key.Name() << " When Load Behavior." << ERROR_END;
			return nullptr;
		}

		std::list<std::tuple<NodePtr, STRING>> toLoadTrees;
		std::set<STRING> loadedTrees;

		_GetToLoadTrees(pFSM->GetTreeMap(), toLoadTrees, m2t);

		while (!toLoadTrees.empty())
		{
			auto& it = toLoadTrees.front();
			auto nodeptr = std::get<0>(it);
			auto& name = std::get<1>(it);
			bool visited = !loadedTrees.insert(name).second;

			BehaviorTree* tree = Mgrs::Instance()->GetTreeMgr()->GetTree(name);
			if (tree == nullptr)
			{
				ERROR_BEGIN << "Cant Get Tree " << name << " When Load Behavior " << key.Name() << ERROR_END;
				return nullptr;
			}
			behavior->GetTreeMapping()[nodeptr] = tree;

			toLoadTrees.pop_front();

			if (!visited)
			{
				_GetToLoadTrees(tree->GetTreeMap(), toLoadTrees, t2t);
			}

		}

		//////////////////////////////////////////////////////////////////////////
		for (auto it : behavior->GetTreeMapping())
		{
			behavior->Merge(it.second);
			//it.second()->MergeDataTo(*behavior->GetMemory()->GetMainData());
			//it.second()->MergeEventsTo(behavior->GetValidEvents());
		}
		behavior->SetID(key.Hash());
		behavior->SetFSM(pFSM);

		return behavior.release();
	}

	void BehaviorMgr::ReturnBehavior(Behavior* pBehavior)
	{
		return m_VersionMgr.Return(pBehavior);
	}

	void BehaviorMgr::ReloadTree(const STRING& name)
	{
		for (auto & it : m_VersionMgr.GetInfos())
		{
			auto b = it.second->GetLatest();
			if (b == nullptr)
				continue;

			for (auto & it2 : b->GetTreeMapping())
			{
				if (it2.second->GetKey() == name)
				{
					it.second->IncreaseLatestVesion();
					break;
				}
			}
		}
	}

	void BehaviorMgr::ReloadMachine(const STRING& name)
	{
		for (auto & it : m_VersionMgr.GetInfos())
		{
			auto b = it.second->GetLatest();
			if (b == nullptr)
				continue;

			if (b->GetFSM()->GetKey() == name)
				it.second->IncreaseLatestVesion();
		}
	}

	void BehaviorMgr::ReloadAll()
	{
		m_VersionMgr.ReloadAll();
	}

	void BehaviorMgr::Print()
	{
		std::cout << "Print all behaviors" << std::endl;
		for (auto it = m_VersionMgr.GetInfos().begin(); it != m_VersionMgr.GetInfos().end(); ++it)
		{
			std::cout << it->first << std::endl;
			it->second->Print();
		}
		std::cout << "Print all fsms end." << std::endl;
	}

	void BehaviorMgr::Clear()
	{
		m_VersionMgr.Clear();
	}

}
