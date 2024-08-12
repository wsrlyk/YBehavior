#ifdef YSHARP
#ifndef _SHARPNODE_H_
#define _SHARPNODE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	typedef void(STDCALL* OnSharpNodeContextInitDelegate)(TreeNode* pNode, int index, UINT contextUID);
	typedef NodeState(STDCALL* OnSharpNodeContextUpdateDelegate)(TreeNode* pNode, AgentPtr pAgent, int index, UINT contextUID, NodeState lastState);

	typedef bool(STDCALL *OnSharpNodeLoadedDelegate)(TreeNode* pNode, const pugi::xml_node* data, int index);
	typedef NodeState(STDCALL *OnSharpNodeUpdateDelegate)(TreeNode* pNode, AgentPtr pAgent, int index);

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
		void SetIndexInSharp(int index) { m_IndexInSharp = index; }
		void SetHasContext(bool hasContext) { m_HasContext = hasContext; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;
		int m_IndexInSharp{};
		bool m_HasContext{};

		static OnSharpNodeLoadedDelegate s_OnLoadCallback;
		static OnSharpNodeUpdateDelegate s_OnUpdateCallback;
	};
}


#endif // _SHARPNODE_H_
#endif