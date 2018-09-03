#include "YBehavior/sharp/sharpnode.h"

namespace YBehavior
{
	NodeState SharpNode::Update(AgentPtr pAgent)
	{
		if (_OnUpdateCallback)
		{
			LOG_BEGIN << "SharpNode Update" << LOG_END;
			return _OnUpdateCallback(this, pAgent);
		}
		return NS_SUCCESS;
	}


	bool SharpNode::OnLoaded(const pugi::xml_node& data)
	{
		if (_OnLoadCallback)
		{
			LOG_BEGIN << "SharpNode OnLoaded" << LOG_END;
			return _OnLoadCallback(this, &data);
		}
		return true;
	}
}