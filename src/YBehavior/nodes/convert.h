#ifndef _YBEHAVIOR_CONVERT_H_
#define _YBEHAVIOR_CONVERT_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IDataConvertHelper;
	class Convert : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Convert)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		IPin* m_Source;
		IPin* m_Target;

		const IDataConvertHelper* m_pConvert{};
	};
}

#endif
