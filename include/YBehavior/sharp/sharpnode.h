#ifdef SHARP
#ifndef _SHARPNODE_H_
#define _SHARPNODE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	typedef bool(STDCALL *OnSharpNodeLoadedDelegate)(BehaviorNode* pNode, const pugi::xml_node* data);
	typedef NodeState(STDCALL *OnSharpNodeUpdateDelegate)(BehaviorNode* pNode, AgentPtr pAgent);

	class SharpNode : public LeafNode
	{
	public:
		STRING GetClassName() const override { return m_Name; }

		SharpNode() {}
		~SharpNode() {}

		void SetOnLoadCallback(OnSharpNodeLoadedDelegate callback) { _OnLoadCallback = callback; }
		void SetOnUpdateCallback(OnSharpNodeUpdateDelegate callback) { _OnUpdateCallback = callback; }
		void SetName(const STRING& name) { m_Name = name; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		OnSharpNodeLoadedDelegate _OnLoadCallback;
		OnSharpNodeUpdateDelegate _OnUpdateCallback;
		STRING m_Name;
	};
}


#endif // _SHARPNODE_H_
#endif