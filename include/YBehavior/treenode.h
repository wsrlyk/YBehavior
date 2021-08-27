#ifndef _YBEHAVIOR_TREENODE_H_
#define _YBEHAVIOR_TREENODE_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <unordered_map>
#include <unordered_set>
#include "sharedvariableex.h"
#include "3rd/pugixml/pugixml.hpp"

//namespace pugi
//{
//	class xml_node;
//	class xml_attribute;
//}

namespace YBehavior
{
	enum NodeState
	{
		NS_INVALID = -1,
		NS_SUCCESS = 0,
		NS_FAILURE = 1,
		NS_BREAK = 2,
		NS_RUNNING = 3,
	};

	enum ReturnType
	{
		RT_DEFAULT,
		RT_INVERT,
		RT_SUCCESS,
		RT_FAILURE,
	};

	enum SingleType
	{
		ST_NONE,
		ST_SINGLE,
		ST_ARRAY,
	};

	class ISharedVariableEx;
	class SharedDataEx;
	struct NameKeyMgr;
#ifdef YDEBUGGER
	class DebugTreeHelper;
#endif
#ifdef YPROFILER
	namespace Profiler
	{
		class TreeNodeProfileHelper;
	}
#endif
	class BehaviorTree;
	class TreeNode;
	class TreeNodeContext;
	typedef TreeNode* TreeNodePtr;

#define ERROR_BEGIN_NODE_HEAD ERROR_BEGIN << this->m_UID << "." << this->GetClassName() << " "
#define LOG_BEGIN_NODE_HEAD LOG_BEGIN << this->m_UID << "." << this->GetClassName() << " "
#define ERROR_BEGIN_NODE(pTreeNode) ERROR_BEGIN << pTreeNode->GetUID() << "." << pTreeNode->GetClassName() << " "
#define LOG_BEGIN_NODE(pTreeNode) LOG_BEGIN << pTreeNode->GetUID() << "." << pTreeNode->GetClassName() << " "

#define TREENODE_DEFINE(classname) classname() { m_ClassName = #classname; }
	class TreeNode
	{
	protected:
		TreeNodePtr m_Parent;
		TreeNodePtr m_Condition;
		UINT m_UID;	// Unique in a tree
		StdVector<ISharedVariableEx*> m_Variables;	///> Just for destructions of variables
		static std::unordered_set<STRING> KEY_WORDS;
		BehaviorTree* m_Root;
		ReturnType m_ReturnType;
		STRING m_ClassName;

#ifdef YDEBUGGER
		DebugTreeHelper* m_pDebugHelper{};
#endif

#ifdef YPROFILER
	protected:
		Profiler::TreeNodeProfileHelper* m_pProfileHelper;
#endif

	public:
		TreeNode();
		virtual ~TreeNode();

		inline TreeNodePtr GetParent() { return m_Parent;}
		inline void SetParent(TreeNodePtr parent) { m_Parent = parent;}

		inline UINT GetUID() const { return m_UID; }
		inline void SetUID(UINT uid) { m_UID = uid; }
		inline BehaviorTree* GetRoot() const { return m_Root; }
		inline void SetRoot(BehaviorTree* root) { m_Root = root; }
		inline TreeNodePtr GetCondition() const { return m_Condition; }
		inline ReturnType GetReturnType() const { return m_ReturnType; }

		const STRING& GetClassName() const { return m_ClassName; }
		const STRING& GetTreeName() const;

		bool Load(const pugi::xml_node& data);
		bool LoadChild(const pugi::xml_node& data);
		void LoadFinish();
		NodeState Execute(AgentPtr pAgent, NodeState parentState);

		static TreeNode* CreateNodeByName(const STRING& name);
		bool AddChild(TreeNode* child, const STRING& connection);

		TreeNodeContext* CreateContext();
		void DestroyContext(TreeNodeContext*&);

		void AddVariable(ISharedVariableEx* pVariable);
#ifdef YDEBUGGER
		void SetDebugHelper(DebugTreeHelper* pDebugHelper) { m_pDebugHelper = pDebugHelper; };
		inline DebugTreeHelper* GetDebugHelper() const { return m_pDebugHelper; }
#endif
	protected:
		virtual bool _AddChild(TreeNode* child, const STRING& connection);
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual bool OnLoaded(const pugi::xml_node& data) { return true; }
		virtual bool OnLoadChild(const pugi::xml_node& data) { return true; }
		virtual void OnLoadFinish() {}
		virtual void OnAddChild(TreeNode* child, const STRING& connection) {}
		virtual TreeNodeContext* _CreateContext() { return nullptr; } //TODO: =0 }
		virtual void _DestroyContext(TreeNodeContext*& pContext) { }//TODO: =0 }
		virtual void _InitContext(TreeNodeContext* pContext) {}

	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	class TreeNodeContext
	{
	protected:
		enum struct RootStage
		{
			None,
			Condition,
			Main,
		};
		TreeNodePtr m_pNode{};
		RootStage m_RootStage{ RootStage::None };

	public:
		TreeNodeContext();
		virtual ~TreeNodeContext();
		void Init(TreeNodePtr pNode);
		static void Destroy(TreeNodeContext*& pContext);
		void OnDestroy();
		inline TreeNodePtr GetTreeNode() { return m_pNode; }
		NodeState Execute(AgentPtr pAgent, NodeState lastState);
	protected:
		virtual NodeState _Update(AgentPtr pAgent, NodeState lastState) { return m_pNode->Execute(pAgent, lastState); }
		virtual void _OnInit() {};
		virtual void _OnDestroy() {};

#ifdef YDEBUGGER
	public:
		static bool HasDebugPoint(DebugTreeHelper* pDebugHelper);
		static std::stringstream& GetLogInfo(DebugTreeHelper* pDebugHelper);
		static void LogVariable(DebugTreeHelper* pDebugHelper, ISharedVariableEx* pVariable, bool bBefore);
		void SendLog();
	protected:
		DebugTreeHelper* m_pDebugHelper{};
#endif
	};

#ifdef YDEBUGGER
#define YB_IF_HAS_DEBUG_POINT if (YBehavior::TreeNodeContext::HasDebugPoint(m_pDebugHelper))
#define YB_LOG_INFO(info)\
	{\
		YB_IF_HAS_DEBUG_POINT\
			YBehavior::TreeNodeContext::GetLogInfo(m_pDebugHelper) << info;\
	}
#define YB_LOG_INFO_WITH_END(info)\
	{\
		YB_IF_HAS_DEBUG_POINT\
			YBehavior::TreeNodeContext::GetLogInfo(m_pDebugHelper) << info << "; ";\
	}
#define YB_LOG_VARIABLE(variable, isbefore)\
		YBehavior::TreeNodeContext::LogVariable(m_pDebugHelper, variable, isbefore);

#define YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(variable, isbefore) \
	{\
		YB_IF_HAS_DEBUG_POINT\
			YB_LOG_VARIABLE(variable, isbefore);\
	}

#define YB_LOG_VARIABLE_BEFORE(variable)\
	YB_LOG_VARIABLE(variable, true)
#define YB_LOG_VARIABLE_AFTER(variable)\
	YB_LOG_VARIABLE(variable, false)
#define YB_LOG_VARIABLE_BEFORE_IF_HAS_DEBUG_POINT(variable)\
	YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(variable, true)
#define YB_LOG_VARIABLE_AFTER_IF_HAS_DEBUG_POINT(variable)\
	YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(variable, false)

#else
#define YB_LOG_INFO(info);
#define YB_LOG_INFO_WITH_END(info);
#define YB_IF_HAS_DEBUG_POINT
#define YB_LOG_VARIABLE(variable, isbefore)
#define YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(variable, isbefore)
#define YB_LOG_VARIABLE_BEFORE(variable)
#define YB_LOG_VARIABLE_AFTER(variable)
#define YB_LOG_VARIABLE_BEFORE_IF_HAS_DEBUG_POINT(variable)
#define YB_LOG_VARIABLE_AFTER_IF_HAS_DEBUG_POINT(variable)
#endif 

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	template<typename ContextType = TreeNodeContext>
	class BehaviorContextNode : public TreeNode
	{
	protected:
		using NodeContextType = ContextType;
		TreeNodeContext* _CreateContext() override;
		void _DestroyContext(TreeNodeContext*& pContext) override;
	
	};

	template<typename ContextType /*= TreeNodeContext*/>
	TreeNodeContext* YBehavior::BehaviorContextNode<ContextType>::_CreateContext()
	{
		return ObjectPoolStatic<ContextType>::Get();
	}

	template<typename ContextType /*= TreeNodeContext*/>
	void YBehavior::BehaviorContextNode<ContextType>::_DestroyContext(TreeNodeContext*& pContext)
	{
		ObjectPoolStatic<ContextType>::Recycle(static_cast<ContextType*>(pContext));
		pContext = nullptr;
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	class CompositeNodeContext : public TreeNodeContext
	{
	protected:
		StdVector<TreeNodePtr>* m_pChildren{};
		int m_Stage{};
	protected:
		void _OnInit() { TreeNodeContext::_OnInit(); m_Stage = 0; }
	public:
		void SetChildren(StdVector<TreeNodePtr>* pChildren) { m_pChildren = pChildren; }
	};

	template<typename ContextType = CompositeNodeContext>
	class CompositeNode : public BehaviorContextNode<ContextType>
	{
	public:
		~CompositeNode()
		{
			if (m_Children)
			{
				for (auto it : *m_Children)
				{
					delete it;
				}
				delete m_Children;
			}
		}
		TreeNodePtr GetChild(UINT index);
	protected:
		StdVector<TreeNodePtr>* m_Children{};
		StdVector<TreeNodePtr>* m_Childs{};

		bool _AddChild(TreeNode* child, const STRING& connection) override;
		void _InitContext(TreeNodeContext* pContext) override;
	};

	template<typename ContextType /*= CompositeNodeContext*/>
	void YBehavior::CompositeNode<ContextType>::_InitContext(TreeNodeContext* pContext)
	{
		((CompositeNodeContext*)pContext)->SetChildren(m_Children);
	}

	template<typename ContextType /*= TreeNodeContext*/>
	TreeNodePtr YBehavior::CompositeNode<ContextType>::GetChild(UINT index)
	{
		if (m_Children && index < m_Children->size())
			return (*m_Children)[index];

		return nullptr;
	}

	template<typename ContextType /*= TreeNodeContext*/>
	bool YBehavior::CompositeNode<ContextType>::_AddChild(TreeNode* child, const STRING& connection)
	{
		if (child == nullptr)
			return false;

		if (!m_Children)
			m_Children = new StdVector<TreeNodePtr>();

		m_Children->push_back(child);
		child->SetParent(this);

		this->OnAddChild(child, connection);

		return true;
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	template<typename ContextType = TreeNodeContext>
	class LeafNode: public BehaviorContextNode<ContextType>
	{
	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	class SingleChildNodeContext : public TreeNodeContext
	{
	protected:
		TreeNode* m_pChild{};
		int m_Stage{};
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		void _OnInit() { TreeNodeContext::_OnInit(); m_Stage = 0; }
	public:
		void SetChild(TreeNode* pChild) { m_pChild = pChild; }
	};
	template<typename ContextType = SingleChildNodeContext>
	class SingleChildNode : public BehaviorContextNode<ContextType>
	{
		friend SingleChildNodeContext;
	public:
		~SingleChildNode()
		{
			if (m_Child)
				delete m_Child;
			m_Child = nullptr;
		}
	protected:
		TreeNode* m_Child{};
		bool _AddChild(TreeNode* child, const STRING& connection) override;
		void _InitContext(TreeNodeContext* pContext) override;
	};

	template<typename ContextType /*= SingleChildNodeContext*/>
	bool YBehavior::SingleChildNode<ContextType>::_AddChild(TreeNode* child, const STRING& connection)
	{
		if (child == nullptr)
			return false;

		if (m_Child == nullptr)
			m_Child = child;
		else
		{
			ERROR_BEGIN_NODE_HEAD << "There are more than 1 child" << ERROR_END;
			return false;
		}

		child->SetParent(this);

		this->OnAddChild(child, connection);

		return true;
	}

	template<typename ContextType /*= SingleChildNodeContext*/>
	void YBehavior::SingleChildNode<ContextType>::_InitContext(TreeNodeContext* pContext)
	{
		((SingleChildNodeContext*)pContext)->SetChild(m_Child);
	}


}

#endif