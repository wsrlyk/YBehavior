#ifdef YPROFILER
#include "YBehavior/profile/profileprocessor.h"
#include "YBehavior/profile/profiler.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	namespace Profiler
	{
		StatisticResult StatisticResultHelper::Calc(float ratio)
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
			res.Avg /= (UINT)size;

			res.Ratio = ratio;
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
			time.Push(tick.duration.durationMiliSelf);

			for (auto it = tick.trees.begin(); it != tick.trees.end(); ++it)
			{
				const BehaviorTree* pTree = (const BehaviorTree*)it->first;
				TreeData& treeData = trees[pTree->GetKey()];
				auto& data = it->second;
				treeData.treeName = pTree->GetKey();
				treeData.runCount.Push(data.count);
				treeData.selfTime.Push(data.duration.durationMiliSelf);
				if (data.duration.durationMiliTotal >= data.duration.durationMiliSelf)
					treeData.totalTime.Push(data.duration.durationMiliTotal);

				for (auto it2 = data.nodes.begin(); it2 != data.nodes.end(); ++it2)
				{
					const BehaviorNode* pNode = (const BehaviorNode*)it2->first;
					auto& nodeData = it2->second;
					TreeNodeData& treeNodeData = treeData.nodes[pNode];
					treeNodeData.nodeName = pNode->GetClassName();
					treeNodeData.uid = pNode->GetUID();
					treeNodeData.selfTime.Push(nodeData.duration.durationMiliSelf);
					if (nodeData.duration.durationMiliTotal >= nodeData.duration.durationMiliSelf)
						treeNodeData.totalTime.Push(nodeData.duration.durationMiliTotal);
					treeNodeData.runCount.Push(nodeData.count);
				}
			}
		}

		void ProfileProcessor::_ProcessResult()
		{
			tickCount = time.size();

			m_Statistic.agentUID = agentUID;
			m_Statistic.name = name;
			m_Statistic.time = time.Calc(0.001f);
			m_Statistic.tickCount = tickCount;

			for (auto it = trees.begin(); it != trees.end(); ++it)
			{
				auto& treeData = it->second;
				
				ProfileStatistic::TreeStatistic tree;
				tree.treeName = treeData.treeName;
				tree.runCount = treeData.runCount.Calc();
				tree.selfTime = treeData.selfTime.Calc(0.001f);
				tree.totalTime = treeData.totalTime.Calc(0.001f);

				for (auto it2 = treeData.nodes.begin(); it2 != treeData.nodes.end(); ++it2)
				{
					auto& nodeData = it2->second;

					ProfileStatistic::TreeNodeStatistic node;
					node.nodeName = nodeData.nodeName;
					node.uid = nodeData.uid;
					node.runCount = nodeData.runCount.Calc();
					node.selfTime = nodeData.selfTime.Calc(0.001f);
					node.totalTime = nodeData.totalTime.Calc(0.001f);
					tree.nodes.push_back(node);
				}
				std::sort(tree.nodes.begin(), tree.nodes.end(), [](const ProfileStatistic::TreeNodeStatistic& a, const ProfileStatistic::TreeNodeStatistic& b)
					{
						return a.uid < b.uid;
					}
				);
				m_Statistic.trees.push_back(tree);
			}
			std::sort(m_Statistic.trees.begin(), m_Statistic.trees.end(), [](const ProfileStatistic::TreeStatistic& a, const ProfileStatistic::TreeStatistic& b)
				{
					return a.treeName < b.treeName;
				}
			);
		}

	}
}
#endif // YDEBUGGER
