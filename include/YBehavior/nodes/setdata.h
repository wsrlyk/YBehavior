#ifndef _YBEHAVIOR_SETDATA_H_
#define _YBEHAVIOR_SETDATA_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class SetData : public LeafNode
	{
	public:
		STRING GetName() const override { return "SetData"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual void OnLoaded(const pugi::xml_node& data);

	private:
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr;

		INT m_DataType;
	};
}

#endif
