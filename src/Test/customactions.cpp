#include "customactions.h"

void MyLaunchCore::RegisterActions() const
{
	_Register<GetNameAction>();
}

YBehavior::NodeState GetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	YBehavior::NodeState ns = YBehavior::NS_SUCCESS;
	
	LOG_BEGIN << ((XAgent*)pAgent)->GetEntity()->GetName() << LOG_END;


	return ns;
}
