#ifdef YSHARP
#ifndef _SHARPNODE_H_
#define _SHARPNODE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	typedef bool(STDCALL *OnSharpNodeLoadedDelegate)(TreeNode* pNode, const pugi::xml_node* data);
	typedef NodeState(STDCALL *OnSharpNodeUpdateDelegate)(TreeNode* pNode, AgentPtr pAgent);

	class SharpNode : public LeafNode<>
	{
	public:
		void SetOnLoadCallback(OnSharpNodeLoadedDelegate callback) { _OnLoadCallback = callback; }
		void SetOnUpdateCallback(OnSharpNodeUpdateDelegate callback) { _OnUpdateCallback = callback; }
		void SetName(const STRING& name) { m_ClassName = name; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		OnSharpNodeLoadedDelegate _OnLoadCallback;
		OnSharpNodeUpdateDelegate _OnUpdateCallback;
	};
}


#endif // _SHARPNODE_H_
#endif