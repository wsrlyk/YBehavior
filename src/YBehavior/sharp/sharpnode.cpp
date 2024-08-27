#ifdef YSHARP

#include "YBehavior/sharp/sharpnode.h"
#include "YBehavior/sharp/sharpagent.h"

namespace YBehavior
{

	void SharpNodeContext::_OnInit()
	{
		auto pNode = (SharpNode*)m_pNode;
		if (pNode->m_HasContext && s_OnInitCallback)
		{
			m_UID = ++s_UID;
			s_OnInitCallback(pNode, pNode->m_StaticIndexInSharp, pNode->m_DynamicIndexInSharp, m_UID);
		}
		else
		{
			m_UID = 0;
		}
	}

	NodeState SharpNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		if (m_UID && s_OnUpdateCallback)
		{
			auto pNode = (SharpNode*)m_pNode;
			auto pShartAgent = (SharpAgent*)pAgent;
			return s_OnUpdateCallback(pNode, pAgent, pShartAgent->GetIndex(), pNode->m_StaticIndexInSharp, pNode->m_DynamicIndexInSharp, m_UID, lastState);
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
			auto pShartAgent = (SharpAgent*)pAgent;
			return s_OnUpdateCallback(this, pAgent, pShartAgent->GetIndex(), m_StaticIndexInSharp, m_DynamicIndexInSharp);
		}
		return NS_SUCCESS;
	}


	bool SharpNode::OnLoaded(const pugi::xml_node& data)
	{
		if (s_OnLoadCallback)
		{
			//LOG_BEGIN << "SharpNode OnLoaded" << LOG_END;
			int res = s_OnLoadCallback(this, &data, m_StaticIndexInSharp);
			if (res >= -1)
			{
				m_DynamicIndexInSharp = res;
				return true;
			}
			else
			{
				return false;
			}
		}
		return true;
	}

	YBehavior::OnSharpNodeLoadedDelegate SharpNode::s_OnLoadCallback;
	YBehavior::OnSharpNodeUpdateDelegate SharpNode::s_OnUpdateCallback;
}

#endif