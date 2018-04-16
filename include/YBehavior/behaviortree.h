#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddataex.h"
#include <unordered_map>
#include <unordered_set>
namespace pugi
{
	class xml_node;
	class xml_attribute;
}

namespace YBehavior
{
	enum NodeState
	{
		NS_INVALID = -1,
		NS_SUCCESS,
		NS_FAILED,
		NS_BREAK,
		NS_RUNNING,
	};

	class ISharedVariableEx;
	class YBEHAVIOR_API BehaviorNode
	{
	protected:
		typedef BehaviorNode* BehaviorNodePtr;
		BehaviorNodePtr m_Parent;
		NodeState m_State;
		UINT m_UID;	// Unique in a tree

		static std::unordered_set<STRING> KEY_WORDS;

	public:
		BehaviorNode();
		virtual ~BehaviorNode();

		inline BehaviorNodePtr GetParent() { return m_Parent;}
		inline void SetParent(BehaviorNodePtr parent) { m_Parent = parent;}

		inline UINT GetUID() { return m_UID; }
		inline void SetUID(UINT uid) { m_UID = uid; }

		void Load(const pugi::xml_node& data);
		NodeState Execute(AgentPtr pAgent);
		static BehaviorNode* CreateNodeByName(const STRING& name);
		virtual void AddChild(BehaviorNode* child, const STRING& connection);

		virtual STRING GetNodeInfoForPrint() { return "";}

		BehaviorNodePtr GetRoot();
	protected:
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual void OnLoaded(const pugi::xml_node& data) {}
		virtual void OnAddChild(BehaviorNode* child, const STRING& connection) {}
		STRING GetValue(const STRING & attriName, const pugi::xml_node & data);
		int CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle);

		///>
		/// single: 1, single; 0, vector; -1, dont care
		bool ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, std::vector<STRING>& buffer, int single = -1);
	};

	class YBEHAVIOR_API BranchNode: public BehaviorNode
	{
	public:
		BranchNode();
		~BranchNode();
		BehaviorNodePtr GetChild(UINT index);
	protected:
		std::vector<BehaviorNodePtr>* m_Childs;

		void AddChild(BehaviorNode* child, const STRING& connection) override;
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
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;
		NodeState Update(AgentPtr pAgent) override;
	};

	class YBEHAVIOR_API CompositeNode: public BranchNode
	{

	};
	class YBEHAVIOR_API BehaviorTree : public SingleChildNode
	{
	private:
		SharedDataEx* m_SharedData;	///> 原始数据，每个使用此树的Agent都从这拷数据作为初始化
		STRING m_Name;
	public:
		BehaviorTree(const STRING& name);
		~BehaviorTree();
		inline const STRING& GetName() { return m_Name; }
		inline SharedDataEx* GetSharedData() { return m_SharedData; }

		void CloneData(SharedDataEx& destination);
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
	};
}

#endif