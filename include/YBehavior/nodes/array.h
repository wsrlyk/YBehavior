#ifndef _YBEHAVIOR_ARRAY_H_
#define _YBEHAVIOR_ARRAY_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class GetArrayLength : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(GetArrayLength)
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
		TREENODE_DEFINE(ClearArray)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
	};

	class ArrayPushElement : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayPushElement)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
		ISharedVariableEx* m_Element;
	};

	class ArrayRemoveElement : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayRemoveElement)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
		ISharedVariableEx* m_Element;
		SharedVariableEx<BOOL>* m_IsAll;
	};

	class ArrayHasElement : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayHasElement)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
		ISharedVariableEx* m_Element;
		SharedVariableEx<INT>* m_Count;
		SharedVariableEx<INT>* m_Index;
	};

	class IsArrayEmpty : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(IsArrayEmpty)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Array;
	};

	class GenIndexArray : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(GenIndexArray)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Input;
		SharedVariableEx<VecInt>* m_Output;
	};

}

#endif
