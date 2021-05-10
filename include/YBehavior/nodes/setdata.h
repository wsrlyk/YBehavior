#ifndef _YBEHAVIOR_SETDATA_H_
#define _YBEHAVIOR_SETDATA_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class SetData : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(SetData)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr;
	};
}

#endif
