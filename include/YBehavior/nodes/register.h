#ifndef _YBEHAVIOR_REGISTER_H_
#define _YBEHAVIOR_REGISTER_H_

#include "YBehavior/treenode.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	class ReadRegister : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ReadRegister)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		SharedVariableEx<STRING>* m_Event;
		SharedVariableEx<VecInt>* m_Int;
		SharedVariableEx<VecFloat>* m_Float;
		SharedVariableEx<VecBool>* m_Bool;
		SharedVariableEx<VecUlong>* m_Ulong;
		SharedVariableEx<VecString>* m_String;
	};

	class WriteRegister : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(WriteRegister)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

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
