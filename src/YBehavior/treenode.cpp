#include "YBehavior/treenode.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/sharedvariableex.h"
#ifdef YDEBUGGER
#include "YBehavior/debugger.h"
#endif
#include "YBehavior/shareddataex.h"
#include "YBehavior/agent.h"
#include <string.h>
#include "YBehavior/profile/profileheader.h"
#include "YBehavior/variablecreation.h"


namespace YBehavior
{
#ifdef YDEBUGGER
#define DEBUG_RETURN(helper, rawres, finalres)\
	{\
		helper.SetResult(rawres, finalres);\
		return (finalres);\
	}
#else
#define DEBUG_RETURN(helper, rawres, finalres)\
	return (finalres)
#endif
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//Bimap<NodeState, STRING, EnumClassHash> s_NodeStateMap = {
	//	{ NS_SUCCESS, "SUCCESS" },{ NS_FAILURE, "FAILURE" },{ NS_RUNNING, "RUNNING" },{ NS_BREAK, "BREAK" },{ NS_INVALID, "INVALID" }
	//};

	TreeNode::TreeNode()
	{
		m_Parent = nullptr;
		m_Condition = nullptr;
		m_Root = nullptr;
		m_ReturnType = RT_DEFAULT;
	}


	TreeNode::~TreeNode()
	{
		if (m_Condition != nullptr)
			delete m_Condition;

		for (auto it = m_Variables.begin(); it != m_Variables.end(); ++it)
		{
			delete *it;
		}
		m_Variables.clear();
	}

	std::unordered_set<STRING> TreeNode::KEY_WORDS = { "Class", "Pos", "NickName" };

	TreeNode* TreeNode::CreateNodeByName(const STRING& name)
	{
		return NodeFactory::Instance()->Get(name);
	}

	YBehavior::NodeState TreeNode::Execute(AgentPtr pAgent, NodeState parentState)
	{
		return Update(pAgent);
	}

	const YBehavior::STRING& TreeNode::GetTreeName() const
	{
		return GetRoot()->GetTreeName();
	}

	bool TreeNode::Load(const pugi::xml_node& data)
	{
		auto returnType = data.attribute("Return");
		if (!returnType.empty())
		{
			STRING s(returnType.value());
			if (s == "Invert")
				m_ReturnType = RT_INVERT;
			else if (s == "Success")
				m_ReturnType = RT_SUCCESS;
			else if (s == "Failure")
				m_ReturnType = RT_FAILURE;
			else
				m_ReturnType = RT_DEFAULT;
		}

		return OnLoaded(data);
	}

	bool TreeNode::LoadChild(const pugi::xml_node& data)
	{
		return OnLoadChild(data);
	}

	void TreeNode::LoadFinish()
	{
		OnLoadFinish();
	}

	bool TreeNode::AddChild(TreeNode* child, const STRING& connection)
	{
		if (connection == "condition")
		{
			if (m_Condition == nullptr)
			{
				m_Condition = child;
				child->SetParent(this);
				return true;
			}

			ERROR_BEGIN_NODE_HEAD << "Too many Condition Node" << ERROR_END;
			return false;
		}
		else
		{
			return _AddChild(child, connection);
		}
	}

	bool TreeNode::_AddChild(TreeNode* child, const STRING& connection)
	{
		ERROR_BEGIN_NODE_HEAD << "Cant add child to this node" << ERROR_END;
		return false;
	}

	TreeNodeContext* TreeNode::CreateContext()
	{
		TreeNodeContext* pContext = _CreateContext();
		_InitContext(pContext);
		pContext->Init(this);
		return pContext;
	}


	void TreeNode::DestroyContext(TreeNodeContext*& pContext)
	{
		pContext->OnDestroy();
		_DestroyContext(pContext);
	}

	void TreeNode::AddVariable(ISharedVariableEx* pVariable)
	{
		if (pVariable)
		{
			pVariable->SetIndex((UINT)m_Variables.size());
			m_Variables.push_back(pVariable);
		}
	}

	YBehavior::ISharedVariableEx* TreeNode::GetVariable(const STRING& name) const
	{
		for (auto it : m_Variables)
		{
			if (it->GetName() == name)
				return it;
		}
		return nullptr;
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	TreeNodeContext::TreeNodeContext()
	{
#ifdef YDEBUGGER
		m_pDebugHelper = new DebugTreeHelper(this);
#endif
	}

	TreeNodeContext::~TreeNodeContext()
	{
		m_pNode = nullptr;
#ifdef YDEBUGGER
		delete m_pDebugHelper;
#endif
	}


	void TreeNodeContext::Init(TreeNodePtr pNode)
	{
		m_pNode = pNode; 
		m_RootStage = RootStage::None;

		_OnInit();
	}

	void TreeNodeContext::Destroy(TreeNodeContext*& pContext)
	{
		pContext->m_pNode->DestroyContext(pContext);
	}

	void TreeNodeContext::OnDestroy()
	{
#ifdef YDEBUGGER
		m_pDebugHelper->Dispose();
#endif
		_OnDestroy();
	}

	NodeState TreeNodeContext::Execute(AgentPtr pAgent, NodeState lastState)
	{
		NodeState state = NS_INVALID;
		auto oldStage = m_RootStage;
		switch (oldStage)
		{
		case RootStage::None:
#ifdef YDEBUGGER
			m_pDebugHelper->Init(pAgent);
#endif
			if (m_pNode->GetCondition())
			{
				pAgent->GetTreeContext()->PushCallStack(m_pNode->GetCondition()->CreateContext());
				m_RootStage = RootStage::Condition;
				DEBUG_RETURN((*m_pDebugHelper), NS_RUNNING, NS_RUNNING);
			}
			else
			{
				m_RootStage = RootStage::Main;
			}
			break;
		case RootStage::Condition:
#ifdef YDEBUGGER
			///> Run info may be cleared by DebugMgr, so make sure it's created.
			m_pDebugHelper->TryRefresh();
#endif
			if (lastState == NS_FAILURE)
				state = NS_FAILURE;
			else
			{
				m_RootStage = RootStage::Main;
			}
			break;
		case RootStage::Main:
#ifdef YDEBUGGER
			///> Run info may be cleared by DebugMgr, so make sure it's created.
			m_pDebugHelper->TryRefresh();
#endif
			///> Nodes like Wait, each run should check the condition
			if (lastState == NS_RUNNING)
			{
				if (m_pNode->GetCondition())
				{
					pAgent->GetTreeContext()->PushCallStack(m_pNode->GetCondition()->CreateContext());
					m_RootStage = RootStage::Condition;
					DEBUG_RETURN((*m_pDebugHelper), NS_RUNNING, NS_RUNNING);
				}
			}
			break;
		default:
			break;
		}

		if (m_RootStage == RootStage::Main)
		{
#ifdef YDEBUGGER
			///> Only the first run here will trigger the breaking
			if (oldStage != RootStage::Main)
				m_pDebugHelper->TryBreaking();
			m_pNode->SetDebugHelper(m_pDebugHelper);
#endif
			state = _Update(pAgent, lastState);
		}

		if (state == NS_INVALID || state == NS_BREAK || state == NS_RUNNING)
			DEBUG_RETURN((*m_pDebugHelper), state, state);

#ifdef YDEBUGGER
		///> Only the last run here will trigger the pause.
		///> This is only used for the parent nodes. 
		///  When child finishes executing, the debugger should pause at the parent, 
		///  OR the debugger may go directly to the root
		m_pDebugHelper->TryPause();
#endif

		NodeState finalState = state;

		switch (m_pNode->GetReturnType())
		{
		case YBehavior::RT_INVERT:
			if (state == NS_SUCCESS)
				finalState = NS_FAILURE;
			else if (state == NS_FAILURE)
				finalState = NS_SUCCESS;
			break;
		case YBehavior::RT_SUCCESS:
			finalState = NS_SUCCESS;
			break;
		case YBehavior::RT_FAILURE:
			finalState = NS_FAILURE;
			break;
		default:
			break;
		}

		DEBUG_RETURN((*m_pDebugHelper), state, finalState);
	}
#ifdef YDEBUGGER
	bool TreeNodeContext::HasDebugPoint(DebugTreeHelper* pDebugHelper)
	{
		return pDebugHelper && pDebugHelper->HasDebugPoint();
	}

	std::stringstream& TreeNodeContext::GetLogInfo(DebugTreeHelper* pDebugHelper)
	{
		return pDebugHelper->GetDebugLogInfo();
	}

	void TreeNodeContext::LogVariable(DebugTreeHelper* pDebugHelper, ISharedVariableEx* pVariable, bool bBefore)
	{
		pDebugHelper->LogSharedData(pVariable, bBefore);
	}

	void TreeNodeContext::SendLog()
	{
		if (m_pDebugHelper->IsValid())
			m_pDebugHelper->SendLogPoint();
	}

#endif
	NodeState SingleChildNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		switch (m_Stage)
		{
		case 0:
			++m_Stage;
			if (m_pChild)
			{
				pAgent->GetTreeContext()->PushCallStack(m_pChild->CreateContext());
				return NS_RUNNING;
			}
			return NS_FAILURE;
		case 1:
			++m_Stage;
			return lastState;
		default:
			break;
		}

		return NS_FAILURE;
	}

}
