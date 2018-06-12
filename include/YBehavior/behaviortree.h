#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
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
	class SharedDataEx;
#ifdef DEBUGGER
	class DebugHelper;
#endif
	class YBEHAVIOR_API BehaviorNode
	{
	protected:
		typedef BehaviorNode* BehaviorNodePtr;
		BehaviorNodePtr m_Parent;
		BehaviorNodePtr m_Condition;
		NodeState m_State;
		UINT m_UID;	// Unique in a tree
		std::vector<ISharedVariableEx*> m_Variables;	///> Just for destructions of variables
		static std::unordered_set<STRING> KEY_WORDS;

#ifdef DEBUGGER
	protected:
		std::stringstream m_DebugLogInfo;
		DebugHelper* m_pDebugHelper;
#define IF_HAS_LOG_POINT if (m_pDebugHelper && m_pDebugHelper->HasLogPoint())
#define DEBUG_LOG_INFO(info)\
	IF_HAS_LOG_POINT\
		m_DebugLogInfo << info;
#define LOG_SHARED_DATA(variable, isbefore) m_pDebugHelper->LogSharedData(variable, isbefore);
	public:
		std::stringstream& GetDebugLogInfo() { return m_DebugLogInfo; }
#else
#define DEBUG_LOG_INFO(info);
#define IF_HAS_LOG_POINT
#define LOG_SHARED_DATA(variable, isbefore)
#endif 
	public:
		BehaviorNode();
		virtual ~BehaviorNode();

		inline BehaviorNodePtr GetParent() { return m_Parent;}
		inline void SetParent(BehaviorNodePtr parent) { m_Parent = parent;}

		inline UINT GetUID() const { return m_UID; }
		inline void SetUID(UINT uid) { m_UID = uid; }

		virtual STRING GetClassName() const = 0;

		void Load(const pugi::xml_node& data);
		NodeState Execute(AgentPtr pAgent);
		static BehaviorNode* CreateNodeByName(const STRING& name);
		void AddChild(BehaviorNode* child, const STRING& connection);

		virtual STRING GetNodeInfoForPrint() { return "";}

		BehaviorNodePtr GetRoot();
	protected:
		virtual void _AddChild(BehaviorNode* child, const STRING& connection);
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual void OnLoaded(const pugi::xml_node& data) {}
		virtual void OnAddChild(BehaviorNode* child, const STRING& connection) {}
		STRING GetValue(const STRING & attriName, const pugi::xml_node & data);
		int CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle, char variableType = 0);

		///>
		/// single: 1, single; 0, vector; -1, dont care
		bool ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, std::vector<STRING>& buffer, int single = -1, char variableType = 0);
	};

	class YBEHAVIOR_API BranchNode: public BehaviorNode
	{
	public:
		BranchNode();
		~BranchNode();
		BehaviorNodePtr GetChild(UINT index);
	protected:
		std::vector<BehaviorNodePtr>* m_Childs;

		void _AddChild(BehaviorNode* child, const STRING& connection) override;
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
	public:
		STRING GetClassName() const override { return "Tree"; }
		inline void SetVersion(int v) { m_Version = v; }
		inline int GetVersion() const { return m_Version; }
	private:
		SharedDataEx* m_SharedData;	///> 原始数据，每个使用此树的Agent都从这拷数据作为初始化
		STRING m_TreeName;
		int m_Version;
	public:
		BehaviorTree(const STRING& name);
		~BehaviorTree();
		inline const STRING& GetTreeName() { return m_TreeName; }
		inline SharedDataEx* GetSharedData() { return m_SharedData; }

		void CloneData(SharedDataEx& destination);
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
	};
}

#endif