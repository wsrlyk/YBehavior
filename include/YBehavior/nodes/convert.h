#ifndef _YBEHAVIOR_CONVERT_H_
#define _YBEHAVIOR_CONVERT_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IVariableConvertHelper;
	class Convert : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Convert)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		ISharedVariableEx* m_Source;
		ISharedVariableEx* m_Target;

		const IVariableConvertHelper* m_pConvert{};
	};
}

#endif
