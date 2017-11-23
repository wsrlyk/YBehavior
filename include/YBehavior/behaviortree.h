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
		NodeState m_State;
		INT m_UID;
	public:
		BehaviorNode();
		virtual ~BehaviorNode();

		inline BehaviorNodePtr GetParent() { return m_Parent;}
		inline void SetParent(BehaviorNodePtr parent) { m_Parent = parent;}
		inline INT GetUID() { return m_UID; }

		void Load(const pugi::xml_node& data);
		NodeState Execute(AgentPtr pAgent);
		static BehaviorNode* CreateNodeByName(const STRING& name);
		virtual void AddChild(BehaviorNode* child);

		virtual STRING GetNodeInfoForPrint() { return "";}
	protected:
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual void OnLoaded(const pugi::xml_node& data) {}
		virtual void OnAddChild(BehaviorNode* child) {}
	};

	class YBEHAVIOR_API BranchNode: public BehaviorNode
	{
	public:
		BranchNode();
		~BranchNode();
		BehaviorNodePtr GetChild(UINT index);
	protected:
		std::vector<BehaviorNodePtr>* m_Childs;

		virtual void AddChild(BehaviorNode* child);
		void _DestroyChilds();
	};

	class YBEHAVIOR_API LeafNode: public BehaviorNode
	{

	};
	class YBEHAVIOR_API SingleChildNode: public BranchNode
	{
	public:
		SingleChildNode();
		~SingleChildNode(){}
	protected:
		BehaviorNode* m_Child;
		virtual void OnAddChild(BehaviorNode* child);
		virtual NodeState Update(AgentPtr pAgent);
	};

	class YBEHAVIOR_API CompositeNode: public BranchNode
	{

	};
	class YBEHAVIOR_API BehaviorTree : public SingleChildNode
	{
	private:
		SharedData* m_SharedData;	///> 原始数据，每个使用此树的Agent都从这拷数据作为初始化
	public:
		BehaviorTree();
		~BehaviorTree();
		void CloneData(SharedData& destination);
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
	};
}

#endif