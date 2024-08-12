#ifdef YSHARP

#include "YBehavior/sharp/sharpnode.h"

namespace YBehavior
{

	void SharpNodeContext::_OnInit()
	{
		auto pNode = (SharpNode*)m_pNode;
		if (pNode->m_HasContext && s_OnInitCallback)
		{
			m_UID = ++s_UID;
			s_OnInitCallback(pNode, pNode->m_IndexInSharp, m_UID);
		}
	}

	NodeState SharpNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		if (m_UID && s_OnUpdateCallback)
		{
			auto pNode = (SharpNode*)m_pNode;
			return s_OnUpdateCallback(pNode, pAgent, pNode->m_IndexInSharp, m_UID, lastState);
		}
		return m_pNode->Execute(pAgent, lastState);
	}

	UINT SharpNodeContext::s_UID{};

	YBehavior::OnSharpNodeContextInitDelegate SharpNodeContext::s_OnInitCallback;
	YBehavior::OnSharpNodeContextUpdateDelegate SharpNodeContext::s_OnUpdateCallback;

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