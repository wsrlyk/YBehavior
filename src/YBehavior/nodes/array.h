#ifndef _YBEHAVIOR_ARRAY_H_
#define _YBEHAVIOR_ARRAY_H_

#include "YBehavior/treenode.h"
#include "../operations/dataarrayoperation.h"

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
		IPin* m_Array;
		Pin<INT>* m_Length;
	};

	class ClearArray : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ClearArray)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Array;
	};

	class ArrayPushElement : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayPushElement)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Array;
		IPin* m_Element;
	};

	class ArrayRemoveElement : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayRemoveElement)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Array;
		IPin* m_Element;
		Pin<BOOL>* m_IsAll;
	};

	class ArrayHasElement : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayHasElement)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Array;
		IPin* m_Element;
		Pin<INT>* m_Count;
		Pin<INT>* m_Index;
	};

	class IsArrayEmpty : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(IsArrayEmpty)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Array;
	};

	class GenIndexArray : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(GenIndexArray)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Input;
		Pin<VecInt>* m_Output;
	};

	class ArrayOperation : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(ArrayOperation)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ArrayOperationType m_Operator;
		IPin* m_Output;
		IPin* m_Input1;
		IPin* m_Input2;

		const IDataArrayOperationHelper* m_pHelper{};
	};

}

#endif
