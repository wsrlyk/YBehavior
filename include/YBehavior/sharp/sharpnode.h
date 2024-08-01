#ifdef YSHARP
#ifndef _SHARPNODE_H_
#define _SHARPNODE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	typedef bool(STDCALL *OnSharpNodeLoadedDelegate)(TreeNode* pNode, const pugi::xml_node* data, int index);
	typedef NodeState(STDCALL *OnSharpNodeUpdateDelegate)(TreeNode* pNode, AgentPtr pAgent, int index);

	class SharpNode : public LeafNode<>
	{
	public:
		static void SetCallback(OnSharpNodeLoadedDelegate onload, OnSharpNodeUpdateDelegate onupdate) { s_OnLoadCallback = onload; s_OnUpdateCallback = onupdate; }

		void SetName(const STRING& name) { m_ClassName = name; }
		void SetIndexInSharp(int index) { m_IndexInSharp = index; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;
		int m_IndexInSharp;

		static OnSharpNodeLoadedDelegate s_OnLoadCallback;
		static OnSharpNodeUpdateDelegate s_OnUpdateCallback;
	};
}


#endif // _SHARPNODE_H_
#endif