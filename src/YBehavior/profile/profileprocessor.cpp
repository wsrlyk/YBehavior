#ifdef YPROFILER
#include "YBehavior/profile/profileprocessor.h"
#include "YBehavior/profile/profiler.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	namespace Profiler
	{
		StatisticResult StatisticResultHelper::Calc()
		{
			StatisticResult res;
			auto size = nums.size();
			if (size == 0)
				return res;

			std::sort(nums.begin(), nums.end());

			res.Min = *nums.begin();
			res.Max = *nums.rbegin();

			if (size % 2 == 1)
				res.Med = nums[size >> 1];
			else
				res.Med = (nums[size >> 1] + nums[(size >> 1) - 1]) >> 1;

			for (auto n : nums)
				res.Avg += n;
			res.Avg /= size;

			return res;
		}

		ProfileProcessor::ProfileProcessor(const Profile& data)
		{
			agentUID = data.agentUID;
			name = data.name;

			for (auto& tick : data.profiles)
			{
				_ProcessTick(tick);
			}

			_ProcessResult();
		}

		void ProfileProcessor::_ProcessTick(const ProfileTick& tick)
		{
			time.Push(tick.duration.durationMiliTotal);

			for (auto it = tick.trees.begin(); it != tick.trees.end(); ++it)
			{
				const BehaviorTree* pTree = (const BehaviorTree*)it->first;
				TreeData& treeData = trees[pTree->GetKey()];
				treeData.treeName = pTree->GetKey();
				treeData.runCount.Push(it->second.count);
				treeData.time.Push(it->second.duration.durationMiliTotal);

				for (auto it2 = it->second.nodes.begin(); it2 != it->second.nodes.end(); ++it2)
				{
					const BehaviorNode* pNode = (const BehaviorNode*)it2->first;
					TreeNodeData& treeNodeData = treeData.nodes[pNode];
					treeNodeData.nodeName = pNode->GetClassName();
					treeNodeData.uid = pNode->GetUID();
					treeNodeData.selfTime.Push(it2->second.duration.durationMiliSelf);
					treeNodeData.totalTime.Push(it2->second.duration.durationMiliTotal);
					treeNodeData.runCount.Push(it2->second.count);
				}
			}
		}

		void ProfileProcessor::_ProcessResult()
		{
			tickCount = time.size();

			m_Statistic.agentUID = agentUID;
			m_Statistic.name = name;
			m_Statistic.time = time.Calc();
			m_Statistic.tickCount = tickCount;

			for (auto it = trees.begin(); it != trees.end(); ++it)
			{
				auto& treeData = it->second;
				
				ProfileStatistic::TreeStatistic tree;
				tree.treeName = treeData.treeName;
				tree.runCount = treeData.runCount.Calc();
				tree.time = treeData.time.Calc();

				for (auto it2 = treeData.nodes.begin(); it2 != treeData.nodes.end(); ++it2)
				{
					auto& nodeData = it2->second;

					ProfileStatistic::TreeNodeStatistic node;
					node.nodeName = nodeData.nodeName;
					node.uid = nodeData.uid;
					node.runCount = nodeData.runCount.Calc();
					node.selfTime = nodeData.selfTime.Calc();
					node.totalTime = nodeData.totalTime.Calc();
					tree.nodes.push_back(node);
				}
				m_Statistic.trees.push_back(tree);
			}
		}

	}
}
#endif // YDEBUGGER
