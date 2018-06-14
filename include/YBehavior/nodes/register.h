#ifndef _YBEHAVIOR_REGISTER_H_
#define _YBEHAVIOR_REGISTER_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class ReadRegister : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "ReadRegister"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual void OnLoaded(const pugi::xml_node& data);

	private:
		ISharedVariableEx* m_Event;
		ISharedVariableEx* m_Int;
		ISharedVariableEx* m_Float;
		ISharedVariableEx* m_Bool;
		ISharedVariableEx* m_Ulong;
		ISharedVariableEx* m_String;
	};
}

#endif
