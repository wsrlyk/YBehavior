#ifndef _YBEHAVIOR_ARRAY_H_
#define _YBEHAVIOR_ARRAY_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class GetArrayLength : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "GetArrayLength"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
		SharedVariableEx<INT>* m_Length;
	};

	class ClearArray : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "ClearArray"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
	};

	class ArrayPushElement : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "ArrayPushElement"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
		ISharedVariableEx* m_Element;
	};

	class IsArrayEmpty : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "IsArrayEmpty"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
	};

	class GenIndexArray : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "GenIndexArray"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Input;
		SharedVariableEx<VecInt>* m_Output;
	};

}

#endif
