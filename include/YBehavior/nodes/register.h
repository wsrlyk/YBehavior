#ifndef _YBEHAVIOR_REGISTER_H_
#define _YBEHAVIOR_REGISTER_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/sharedvariableex.h"

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
		SharedVariableEx<STRING>* m_Event;
		SharedVariableEx<VecInt>* m_Int;
		SharedVariableEx<VecFloat>* m_Float;
		SharedVariableEx<VecBool>* m_Bool;
		SharedVariableEx<VecUlong>* m_Ulong;
		SharedVariableEx<VecString>* m_String;
	};

	class WriteRegister : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "WriteRegister"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual void OnLoaded(const pugi::xml_node& data);

	private:
		SharedVariableEx<STRING>* m_Event;
		SharedVariableEx<VecInt>* m_Int;
		SharedVariableEx<VecFloat>* m_Float;
		SharedVariableEx<VecBool>* m_Bool;
		SharedVariableEx<VecUlong>* m_Ulong;
		SharedVariableEx<VecString>* m_String;
	};
}

#endif
