#ifndef _YBEHAVIOR_BEHAVIORNODE_H_
#define _YBEHAVIOR_BEHAVIORNODE_H_

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
	class RunningContext;
	class IContextCreator;
	class BehaviorNode;
	class TreeNodeContext;
	typedef BehaviorNode* BehaviorNodePtr;

#define ERROR_BEGIN_NODE_HEAD ERROR_BEGIN << m_UID << "." << GetClassName() << " "
#define LOG_BEGIN_NODE_HEAD LOG_BEGIN << m_UID << "." << GetClassName() << " "

	class BehaviorNode
	{
	protected:
		BehaviorNodePtr m_Parent;
		BehaviorNodePtr m_Condition;
		UINT m_UID;	// Unique in a tree
		StdVector<ISharedVariableEx*> m_Variables;	///> Just for destructions of variables
		static std::unordered_set<STRING> KEY_WORDS;
		BehaviorTree* m_Root;
		RunningContext* m_RunningContext;
		IContextCreator* m_ContextCreator;
		ReturnType m_ReturnType;
#ifdef YDEBUGGER
	public:
		bool HasLogPoint();
		void LogSharedData(ISharedVariableEx* pVariable, bool bIsBefore);
		void LogDebugInfo(const STRING& str) { m_DebugLogInfo << str; }
	protected:
		std::stringstream m_DebugLogInfo;
		DebugTreeHelper* m_pDebugHelper{};
#define IF_HAS_LOG_POINT if (HasLogPoint())
#define DEBUG_LOG_INFO(info)\
	{\
		IF_HAS_LOG_POINT\
			m_DebugLogInfo << info;\
	}
#define LOG_SHARED_DATA(variable, isbefore) LogSharedData(variable, isbefore);
#define LOG_SHARED_DATA_IF_HAS_LOG_POINT(variable, isbefore) \
	{\
		IF_HAS_LOG_POINT\
			LOG_SHARED_DATA(variable, isbefore);\
	}
	public:
		std::stringstream& GetDebugLogInfo() { return m_DebugLogInfo; }
#else
#define DEBUG_LOG_INFO(info);
#define IF_HAS_LOG_POINT
#define LOG_SHARED_DATA(variable, isbefore)
#define LOG_SHARED_DATA_IF_HAS_LOG_POINT(variable, isbefore)
#endif 

#ifdef YPROFILER
	protected:
		Profiler::TreeNodeProfileHelper* m_pProfileHelper;
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
		inline BehaviorNodePtr GetCondition() const { return m_Condition; }
		inline ReturnType GetReturnType() const { return m_ReturnType; }

		virtual STRING GetClassName() const = 0;

		bool Load(const pugi::xml_node& data);
		bool LoadChild(const pugi::xml_node& data);
		void LoadFinish();
		NodeState Execute(AgentPtr pAgent, NodeState parentState);

		static BehaviorNode* CreateNodeByName(const STRING& name);
		bool AddChild(BehaviorNode* child, const STRING& connection);

		void TryCreateRC();
		RunningContext* GetRC() { return m_RunningContext; }
		void SetRCCreator(IContextCreator* rcc) { m_ContextCreator = rcc; }

		TreeNodeContext* CreateContext();
		void DestroyContext(TreeNodeContext*);

		///> If no config, a default CONST variable will be created
		TYPEID CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, char variableType = 0, const STRING& defaultCreateStr = Utility::StringEmpty);
		///> If no config, a default CONST variable will be created
		template <typename T>
		TYPEID CreateVariable(SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, char variableType = 0);
		
		///> If no config, variable will NOT be created
		TYPEID CreateVariableIfExist(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, char variableType = 0);
		///> If no config, variable will NOT be created
		template <typename T>
		TYPEID CreateVariableIfExist(SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, char variableType = 0);
	protected:
		virtual bool _AddChild(BehaviorNode* child, const STRING& connection);
		virtual NodeState Update(AgentPtr pAgent) { return NS_SUCCESS; }
		virtual bool OnLoaded(const pugi::xml_node& data) { return true; }
		virtual bool OnLoadChild(const pugi::xml_node& data) { return true; }
		virtual void OnLoadFinish() {}
		virtual void OnAddChild(BehaviorNode* child, const STRING& connection) {}
		virtual TreeNodeContext* _CreateContext() { return nullptr; } //TODO: =0 }
		virtual void _DestroyContext(TreeNodeContext* pContext) { }//TODO: =0 }
		virtual void _InitContext(TreeNodeContext* pContext) {}

		STRING GetValue(const STRING & attriName, const pugi::xml_node & data);
		bool TryGetValue(const STRING & attriName, const pugi::xml_node & data, STRING& output);
		template<typename T>
		bool GetValue(const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T defaultValue, T& outValue);
		template<typename T>
		bool GetValue(const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T& outValue);

		TYPEID _CreateVariable(ISharedVariableEx*& op, const pugi::xml_attribute& attrOptr, const pugi::xml_node& data, char variableType);
		///>
		/// single: 1, single; 0, vector; -1, dont care
		bool ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, StdVector<STRING>& buffer, SingleType single, char variableType = 0);
		RunningContext* _CreateRC() const;
		void _TryDeleteRC();
		void _TryPushRC(AgentPtr agent);
		void _TryPopRC(AgentPtr agent);
	};

	template<typename T>
	bool BehaviorNode::GetValue(const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T& outValue)
	{
		STRING s(GetValue(attriName, data));
		if (strMap.TryGetKey(s, outValue))
			return true;

		ERROR_BEGIN_NODE_HEAD << attriName << " Error: " << s << ERROR_END;
		return false;
	}

	template<typename T>
	bool BehaviorNode::GetValue(const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T defaultValue, T& outValue)
	{
		STRING s;
		if (!TryGetValue(attriName, data, s))
		{
			outValue = defaultValue;
			return true;
		}

		if (strMap.TryGetKey(s, outValue))
			return true;

		ERROR_BEGIN_NODE_HEAD << attriName << " Error: " << s << ERROR_END;
		return false;
	}

	template <typename T>
	TYPEID BehaviorNode::CreateVariable(SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, char variableType)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
		{
			if (variableType != Utility::POINTER_CHAR)
			{
				op = new SharedVariableEx<T>();
				m_Variables.push_back(op);
#ifdef YDEBUGGER
				op->SetName(attriName, GetClassName());
#endif
				return GetTypeID<T>();
			}

			ERROR_BEGIN_NODE_HEAD << "Cant create default variable for " << attriName << " with typeid = " << GetTypeID<T>() << ERROR_END;
			return -1;
		}

		ISharedVariableEx* pTemp = nullptr;
		TYPEID typeID = _CreateVariable(pTemp, attrOptr, data, variableType);
		if (typeID == GetTypeID<T>())
		{
			op = (SharedVariableEx<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN_NODE_HEAD << "Invalid type for " << attriName << " with type " << typeID << ERROR_END;
		}
		return typeID;
	}

	template <typename T>
	TYPEID BehaviorNode::CreateVariableIfExist(SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, char variableType /*= 0*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
			return -1;

		ISharedVariableEx* pTemp = nullptr;
		TYPEID typeID = _CreateVariable(pTemp, attrOptr, data, variableType);
		if (typeID == GetTypeID<T>())
		{
			op = (SharedVariableEx<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN_NODE_HEAD << "Invalid type for " << attriName << " with type " << typeID << ERROR_END;
		}
		return typeID;
	}
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
		BehaviorNodePtr m_pNode{};
		RootStage m_RootStage{ RootStage::None };

	public:
		void Init(BehaviorNodePtr pNode) { m_pNode = pNode; m_RootStage = RootStage::None; _OnInit(); }
		void Destroy() { _OnDestroy(); }
		inline BehaviorNodePtr GetTreeNode() { return m_pNode; }
		NodeState Execute(AgentPtr pAgent, NodeState lastState);
	protected:
		virtual NodeState _Update(AgentPtr pAgent, NodeState lastState) { return m_pNode->Execute(pAgent, lastState); }
		virtual void _OnInit() {};
		virtual void _OnDestroy() {};
	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	template<typename ContextType = TreeNodeContext>
	class BehaviorContextNode : public BehaviorNode
	{
	protected:
		using NodeContextType = ContextType;
		TreeNodeContext* _CreateContext() override;
		void _DestroyContext(TreeNodeContext* pContext) override;
	
	};

	template<typename ContextType /*= TreeNodeContext*/>
	TreeNodeContext* YBehavior::BehaviorContextNode<ContextType>::_CreateContext()
	{
		return ObjectPoolStatic<ContextType>::Get();
	}

	template<typename ContextType /*= TreeNodeContext*/>
	void YBehavior::BehaviorContextNode<ContextType>::_DestroyContext(TreeNodeContext* pContext)
	{
		ObjectPoolStatic<ContextType>::Recycle(static_cast<ContextType*>(pContext));
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	class CompositeNodeContext : public TreeNodeContext
	{
	protected:
		StdVector<BehaviorNodePtr>* m_pChildren{};
		int m_Stage{};
	protected:
		void _OnInit() { TreeNodeContext::_OnInit(); m_Stage = 0; }
	public:
		void SetChildren(StdVector<BehaviorNodePtr>* pChildren) { m_pChildren = pChildren; }
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
		BehaviorNodePtr GetChild(UINT index);
	protected:
		StdVector<BehaviorNodePtr>* m_Children{};
		StdVector<BehaviorNodePtr>* m_Childs{};

		bool _AddChild(BehaviorNode* child, const STRING& connection) override;
		void _InitContext(TreeNodeContext* pContext) override;
	};

	template<typename ContextType /*= CompositeNodeContext*/>
	void YBehavior::CompositeNode<ContextType>::_InitContext(TreeNodeContext* pContext)
	{
		((CompositeNodeContext*)pContext)->SetChildren(m_Children);
	}

	template<typename ContextType /*= TreeNodeContext*/>
	BehaviorNodePtr YBehavior::CompositeNode<ContextType>::GetChild(UINT index)
	{
		if (m_Children && index < m_Children->size())
			return (*m_Children)[index];

		return nullptr;
	}

	template<typename ContextType /*= TreeNodeContext*/>
	bool YBehavior::CompositeNode<ContextType>::_AddChild(BehaviorNode* child, const STRING& connection)
	{
		if (child == nullptr)
			return false;

		if (!m_Children)
			m_Children = new StdVector<BehaviorNodePtr>();

		m_Children->push_back(child);
		child->SetParent(this);

		OnAddChild(child, connection);

		return true;
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	template<typename ContextType = TreeNodeContext>
	class LeafNode: public BehaviorContextNode<ContextType>
	{
	public:
		class LeafContext : public BehaviorContextNode<ContextType>
		{

		};
	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	class SingleChildNodeContext : public TreeNodeContext
	{
	protected:
		BehaviorNode* m_pChild{};
		int m_Stage{};
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		void _OnInit() { TreeNodeContext::_OnInit(); m_Stage = 0; }
	public:
		void SetChild(BehaviorNode* pChild) { m_pChild = pChild; }
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
		BehaviorNode* m_Child{};
		bool _AddChild(BehaviorNode* child, const STRING& connection) override;
		void _InitContext(TreeNodeContext* pContext) override;
	};

	template<typename ContextType /*= SingleChildNodeContext*/>
	bool YBehavior::SingleChildNode<ContextType>::_AddChild(BehaviorNode* child, const STRING& connection)
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

		OnAddChild(child, connection);

		return true;
	}

	template<typename ContextType /*= SingleChildNodeContext*/>
	void YBehavior::SingleChildNode<ContextType>::_InitContext(TreeNodeContext* pContext)
	{
		((SingleChildNodeContext*)pContext)->SetChild(m_Child);
	}


}

#endif