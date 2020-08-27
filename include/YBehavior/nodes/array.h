#ifndef _YBEHAVIOR_ARRAY_H_
#define _YBEHAVIOR_ARRAY_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class GetArrayLength : public LeafNode
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

	class ClearArray : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "ClearArray"; }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ISharedVariableEx* m_Array;
	};

	class ArrayPushElement : public LeafNode
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

	class IsArrayEmpty : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "IsArrayEmpty"; }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ISharedVariableEx* m_Array;
	};

	class GenIndexArray : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "GenIndexArray"; }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ISharedVariableEx* m_Input;
		SharedVariableEx<VecInt>* m_Output;
	};

}

#endif
