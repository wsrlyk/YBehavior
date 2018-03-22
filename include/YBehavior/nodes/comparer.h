#ifndef _YBEHAVIOR_COMPARER_H_
#define _YBEHAVIOR_COMPARER_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/bimap.h"
#include <unordered_set>

namespace YBehavior
{
	enum ComparerOperator
	{
		CPO_NONE,
		CPO_EQUAL = 1,
		CPO_LARGER,
		CPO_SMALLER,
		CPO_INEQUAL,
		CPO_NOTSMALLER,
		CPO_NOTLARGER,
	};
	class Comparer : public BehaviorNode
	{
	public:
		static Bimap<ComparerOperator, STRING> s_OperatorMap;
		static std::unordered_set<TypeAB> s_ValidTypes;
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		ComparerOperator m_Operator;
		ISharedVariable* m_Opl;
		ISharedVariable* m_Opr;

		TypeAB m_DataType;

	};
}

#endif
