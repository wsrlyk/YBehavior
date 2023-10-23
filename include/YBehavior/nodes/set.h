#ifndef _YBEHAVIOR_SET_H_
#define _YBEHAVIOR_SET_H_

#include "YBehavior/treenode.h"
#include "YBehavior/variables/variablesetoperation.h"

namespace YBehavior
{
	class SetOperation : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(SetOperation)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		SetOperationType m_Operator;
		ISharedVariableEx* m_Output;
		ISharedVariableEx* m_Input1;
		ISharedVariableEx* m_Input2;

		const IVariableSetOperationHelper* m_pHelper{};
	};

}

#endif
