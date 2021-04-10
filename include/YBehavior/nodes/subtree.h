#ifndef _YBEHAVIOR_SUBTREE_H_
#define _YBEHAVIOR_SUBTREE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class SubTree : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "SubTree"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;
		bool OnLoadChild(const pugi::xml_node& data) override;
		bool _TryCreateFromTo(const pugi::xml_node& data, std::vector<ISharedVariableEx*>& container);
	private:
		SharedVariableEx<STRING>* m_TreeName;
		SharedVariableEx<STRING>* m_Identification;

		std::vector<ISharedVariableEx* > m_Inputs;
		std::vector<ISharedVariableEx* > m_Outputs;

		STRING m_FinalTreeName;
	};
}

#endif
