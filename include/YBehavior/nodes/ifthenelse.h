#ifndef _YBEHAVIOR_IFTHENELSE_H_
#define _YBEHAVIOR_IFTHENELSE_H_
#endif

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class YBEHAVIOR_API IfThenElse : public BranchNode
	{
	public:
		IfThenElse();
		~IfThenElse();
	protected:
		NodeState Update(AgentPtr pAgent) override;
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;

		BehaviorNode* m_If;
		BehaviorNode* m_Then;
		BehaviorNode* m_Else;
	};
}