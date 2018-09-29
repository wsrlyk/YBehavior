#ifndef _YBEHAVIOR_ARRAY_H_
#define _YBEHAVIOR_ARRAY_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class YBEHAVIOR_API GetArrayLength : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "GetArrayLength"; }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ISharedVariableEx* m_Array;
		SharedVariableEx<INT>* m_Length;
	};

	class YBEHAVIOR_API ClearArray : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "ClearArray"; }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ISharedVariableEx* m_Array;
	};

	class YBEHAVIOR_API ArrayPushElement : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "ArrayPushElement"; }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ISharedVariableEx* m_Array;
		ISharedVariableEx* m_Element;
	};

}

#endif
