#ifndef _YBEHAVIOR_CALCULATOR_H_
#define _YBEHAVIOR_CALCULATOR_H_

#include "YBehavior/treenode.h"
#include "YBehavior/variables/variablecalculate.h"

namespace YBehavior
{
	class Calculator : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Calculator)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		CalculateType m_Operator;
		ISharedVariableEx* m_Output;
		ISharedVariableEx* m_Input1;
		ISharedVariableEx* m_Input2;

		const IVariableCalculateHelper* m_pHelper{};
	};
}

#endif
