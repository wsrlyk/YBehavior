#ifndef _YBEHAVIOR_RANDOM_H_
#define _YBEHAVIOR_RANDOM_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class Random : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "Random"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual bool OnLoaded(const pugi::xml_node& data);

	private:
		ISharedVariableEx* m_Opr1;
		ISharedVariableEx* m_Opr2;
		ISharedVariableEx* m_Opl;

		TYPEID m_DataType;
	};

	class RandomSelect : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "RandomSelect"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		ISharedVariableEx* m_Input;
		ISharedVariableEx* m_Output;

		TYPEID m_DataType;
	};

	class Shuffle : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "Shuffle"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		ISharedVariableEx* m_Input;
		ISharedVariableEx* m_Output;

		bool m_bSameArray;
	};

}

#endif
