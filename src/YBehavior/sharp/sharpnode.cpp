#ifdef YSHARP

#include "YBehavior/sharp/sharpnode.h"

namespace YBehavior
{
	NodeState SharpNode::Update(AgentPtr pAgent)
	{
		if (s_OnUpdateCallback)
		{
			//LOG_BEGIN << "SharpNode Update" << LOG_END;
			return s_OnUpdateCallback(this, pAgent, m_IndexInSharp);
		}
		return NS_SUCCESS;
	}


	bool SharpNode::OnLoaded(const pugi::xml_node& data)
	{
		if (s_OnLoadCallback)
		{
			//LOG_BEGIN << "SharpNode OnLoaded" << LOG_END;
			return s_OnLoadCallback(this, &data, m_IndexInSharp);
		}
		return true;
	}

	YBehavior::OnSharpNodeLoadedDelegate SharpNode::s_OnLoadCallback;
	YBehavior::OnSharpNodeUpdateDelegate SharpNode::s_OnUpdateCallback;

}

#endif