#ifndef _YBEHAVIOR_UNARYOPERATION_H_
#define _YBEHAVIOR_UNARYOPERATION_H_

#include "YBehavior/treenode.h"
#include "../operations/dataunaryop.h"

namespace YBehavior
{
	class UnaryOperation : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(UnaryOperation)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		UnaryOpType m_Operator;
		IPin* m_Output;
		IPin* m_Input;

		const IDataUnaryOpHelper* m_pHelper{};
	};
}

#endif
