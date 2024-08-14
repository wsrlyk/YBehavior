#ifndef _YBEHAVIOR_SUBTREE_H_
#define _YBEHAVIOR_SUBTREE_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class LocalMemoryInOut;
	class SubTreeNodeContext : public TreeNodeContext
	{
	protected:
		void _OnInit() override;
		void _OnDestroy() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		int m_Stage;
		LocalMemoryInOut* m_pInOut{};
	};
	class SubTree : public LeafNode<SubTreeNodeContext>
	{
		friend SubTreeNodeContext;
	public:
		TREENODE_DEFINE(SubTree)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		bool OnLoadChild(const pugi::xml_node& data) override;
		bool _TryCreateFromTo(const pugi::xml_node& data, std::vector<IPin*>& container, bool isInput);
	private:
		Pin<STRING>* m_TreeName;
		Pin<STRING>* m_Identification;

		std::vector<IPin* > m_Inputs;
		std::vector<IPin* > m_Outputs;
	};
}

#endif
