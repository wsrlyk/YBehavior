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
		IPin* m_Output;
		IPin* m_Input1;
		IPin* m_Input2;

		const IDataCalculateHelper* m_pHelper{};
	};
}

#endif
