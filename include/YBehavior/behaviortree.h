#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <unordered_map>
#include <unordered_set>
#include "sharedvariableex.h"
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
		NS_FAILURE,
		NS_BREAK,
		NS_RUNNING,
	};

	class ISharedVariableEx;
	class SharedDataEx;
	struct NameKeyMgr;
#ifdef DEBUGGER
	class DebugHelper;
#endif
	class BehaviorTree;
	class RunningContext;
	class IContextCreator;
	class BehaviorNode;
	typedef BehaviorNode* BehaviorNodePtr;

	class YBEHAVIOR_API BehaviorNode
	{
	protected:
		BehaviorNodePtr m_Parent;
		BehaviorNodePtr m_Condition;
		UINT m_UID;	// Unique in a tree
		std::vector<ISharedVariableEx*> m_Variables;	///> Just for destructions of variables
		static std::unordered_set<STRING> KEY_WORDS;
		BehaviorTree* m_Root;
		RunningContext* m_RunningContext;
		IContextCreator* m_ContextCreator;
#ifdef DEBUGGER
	protected:
		std::stringstream m_DebugLogInfo;
		DebugHelper* m_pDebugHelper;
#define IF_HAS_LOG_POINT if (m_pDebugHelper && m_pDebugHelper->HasLogPoint())
#define DEBUG_LOG_INFO(info)\
	IF_HAS_LOG_POINT\
		m_DebugLogInfo << info;
#define LOG_SHARED_DATA(variable, isbefore) m_pDebugHelper->LogSharedData(variable, isbefore);
#define LOG_SHARED_DATA_IF_HAS_LOG_POINT(variable, isbefore) \
		IF_HAS_LOG_POINT\
			LOG_SHARED_DATA(variable, isbefore);
	public:
		std::stringstream& GetDebugLogInfo() { return m_DebugLogInfo; }
#else
#define DEBUG_LOG_INFO(info);
#define IF_HAS_LOG_POINT
#define LOG_SHARED_DATA(variable, isbefore)
#define LOG_SHARED_DATA_IF_HAS_LOG_POINT(variable, isbefore)
#endif 
	public:
		BehaviorNode();
		virtual ~BehaviorNode();

		inline BehaviorNodePtr GetParent() { return m_Parent;}
		inline void SetParent(BehaviorNodePtr parent) { m_Parent = parent;}

		inline UINT GetUID() const { return m_UID; }
		inline void SetUID(UINT uid) { m_UID = uid; }
		inline BehaviorTree* GetRoot() const { return m_Root; }
		inline void SetRoot(BehaviorTree* root) { m_Root = root; }

		virtual STRING GetClassName() const = 0;

		bool Load(const pugi::xml_node& data);
		void LoadFinish();
		NodeState Execute(AgentPtr pAgent, NodeState parentState);
		static BehaviorNode* CreateNodeByName(const STRING& name);
		bool AddChild(BehaviorNode* child, const STRING& connection);

		TYPEID CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle, char variableType = 0);

		virtual STRING GetNodeInfoForPrint() { return "";}

		void TryCreateRC();
		RunningContext* GetRC() { return m_RunningContext; }
		void SetRCCreator(IContextCreator* rcc) { m_ContextCreator = rcc; }
	protected:
		virtual bool _AddChild(BehaviorNode* child, const STRING& connection);
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual bool OnLoaded(const pugi::xml_node& data) { return true; }
		virtual void OnLoadFinish() {}
		virtual void OnAddChild(BehaviorNode* child, const STRING& connection) {}
		STRING GetValue(const STRING & attriName, const pugi::xml_node & data);
		template <typename T> 
		TYPEID CreateVariable(SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle, char variableType = 0);
		///>
		/// single: 1, single; 0, vector; -1, dont care
		bool ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, std::vector<STRING>& buffer, int single = -1, char variableType = 0);
		RunningContext* _CreateRC() const;
		void _TryDeleteRC();
		void _TryPushRC(AgentPtr agent);
		void _TryPopRC(AgentPtr agent);
	};

	template <typename T>
	TYPEID BehaviorNode::CreateVariable(SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle, char variableType)
	{
		ISharedVariableEx* pTemp;
		TYPEID typeID = CreateVariable(pTemp, attriName, data, bSingle, variableType);
		if (typeID == GetClassTypeNumberId<T>())
		{
			op = (SharedVariableEx<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN << "Invalid type for " << attriName << " with type: " << typeID << ERROR_END;
			delete pTemp;
		}
		return typeID;
	}

	class YBEHAVIOR_API BranchNode : public BehaviorNode
	{
	public:
		BranchNode();
		~BranchNode();
		BehaviorNodePtr GetChild(UINT index);
	protected:
		std::vector<BehaviorNodePtr>* m_Childs;

		bool _AddChild(BehaviorNode* child, const STRING& connection) override;
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

	struct TreeVersion;
	class YBEHAVIOR_API BehaviorTree : public SingleChildNode
	{
	public:
		STRING GetClassName() const override { return "Tree"; }
		inline void SetVersion(TreeVersion* v) { m_Version = v; }
		inline TreeVersion* GetVersion() const { return m_Version; }
#ifdef DEBUGGER
		inline UINT GetHash() { return m_Hash; }
		inline void SetHash(UINT hash) { m_Hash = hash; }
#endif
	private:
		SharedDataEx* m_SharedData;	///> Original data, copied to each agent using this tree
		//NameKeyMgr* m_NameKeyMgr;
		STRING m_TreeNameWithPath;	///> Full Path
		STRING m_TreeName;	///> Only File
		TreeVersion* m_Version;
#ifdef DEBUGGER
		UINT m_Hash;
#endif

		std::vector<BehaviorTree*> m_SubTrees;
	public:
		BehaviorTree(const STRING& name);
		~BehaviorTree();
		inline const STRING& GetTreeNameWithPath() { return m_TreeNameWithPath; }
		inline const STRING& GetTreeName() { return m_TreeName; }
		inline SharedDataEx* GetSharedData() { return m_SharedData; }
		//inline NameKeyMgr* GetNameKeyMgr() { return m_NameKeyMgr; }
		void CloneData(SharedDataEx& destination);

		void AddSubTree(BehaviorTree* sub) { m_SubTrees.push_back(sub); }
		inline std::vector<BehaviorTree*>& GetSubTrees() { return m_SubTrees; }

		///> CAUTION: this function can only be called in garbage collection
		void ClearSubTree() { m_SubTrees.clear(); }
	protected:
		virtual bool OnLoaded(const pugi::xml_node& data);
	};
}

#endif