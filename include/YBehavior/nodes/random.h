#ifndef _YBEHAVIOR_RANDOM_H_
#define _YBEHAVIOR_RANDOM_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class Random : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "Random"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual void OnLoaded(const pugi::xml_node& data);

	private:
		ISharedVariableEx* m_Opr1;
		ISharedVariableEx* m_Opr2;
		ISharedVariableEx* m_Opl;

		INT m_DataType;
	};
}

#endif
