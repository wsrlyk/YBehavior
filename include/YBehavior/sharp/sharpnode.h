#ifdef YSHARP
#ifndef _SHARPNODE_H_
#define _SHARPNODE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	typedef void(STDCALL* OnSharpNodeContextInitDelegate)(TreeNode* pNode, int staticIndex, int dynamicIndex, UINT contextUID);
	typedef NodeState(STDCALL* OnSharpNodeContextUpdateDelegate)(TreeNode* pNode, AgentPtr pAgent, int agentIndex, int staticIndex, int dynamicIndex, UINT contextUID, NodeState lastState);

	typedef int(STDCALL *OnSharpNodeLoadedDelegate)(TreeNode* pNode, const pugi::xml_node* data, int staticIndex);
	typedef NodeState(STDCALL *OnSharpNodeUpdateDelegate)(TreeNode* pNode, AgentPtr pAgent, int agentIndex, int staticIndex, int dynamicIndex);

	class SharpNodeContext : public TreeNodeContext
	{
	public:
		static void SetCallback(OnSharpNodeContextInitDelegate oninit, OnSharpNodeContextUpdateDelegate onupdate) { s_OnInitCallback = oninit; s_OnUpdateCallback = onupdate; }

	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		UINT m_UID;
		static UINT s_UID;

		static OnSharpNodeContextInitDelegate s_OnInitCallback;
		static OnSharpNodeContextUpdateDelegate s_OnUpdateCallback;
	};

	class SharpNode : public LeafNode<SharpNodeContext>
	{
		friend SharpNodeContext;
	public:
		static void SetCallback(OnSharpNodeLoadedDelegate onload, OnSharpNodeUpdateDelegate onupdate) { s_OnLoadCallback = onload; s_OnUpdateCallback = onupdate; }

		void SetName(const STRING& name) { m_ClassName = name; }
		void SetIndexInSharp(int index) { m_StaticIndexInSharp = index; }
		void SetHasContext(bool hasContext) { m_HasContext = hasContext; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;
		int m_StaticIndexInSharp{};
		int m_DynamicIndexInSharp;
		bool m_HasContext{};

		static OnSharpNodeLoadedDelegate s_OnLoadCallback;
		static OnSharpNodeUpdateDelegate s_OnUpdateCallback;
	};
}


#endif // _SHARPNODE_H_
#endif