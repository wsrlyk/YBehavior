#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddata.h"
#include <unordered_map>
namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	enum NodeState
	{
		NS_SUCCESS,
		NS_FAILED
	};

	class YBEHAVIOR_API BehaviorNode
	{
	protected:
		typedef BehaviorNode* BehaviorNodePtr;
		BehaviorNodePtr m_Parent;
		std::vector<BehaviorNodePtr>* m_Childs;
		NodeState m_State;
		INT m_UID;
	public:
		BehaviorNode();
		virtual ~BehaviorNode();

		inline BehaviorNodePtr GetParent() { return m_Parent;}
		inline INT GetUID() { return m_UID; }
		BehaviorNodePtr GetChild(UINT index);
		void AddChild(BehaviorNode* child);
		void Load(const pugi::xml_node& data);
		NodeState Execute(AgentPtr pAgent);
		static BehaviorNode* CreateNodeByName(const STRING& name);

		virtual STRING GetNodeInfoForPrint() { return "";}
	protected:
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual void OnLoaded(const pugi::xml_node& data) {}
		void _DestroyChilds();
	};

	class YBEHAVIOR_API BehaviorTree : public BehaviorNode
	{
	private:
		SharedData* m_SharedData;	///> 原始数据，每个使用此树的Agent都从这拷数据作为初始化
	public:
		BehaviorTree();
		~BehaviorTree();
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);
	};
}

#endif