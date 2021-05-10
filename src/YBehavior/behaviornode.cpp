#include "YBehavior/behaviornode.h"
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

	BehaviorNode::BehaviorNode()
	{
		m_Parent = nullptr;
		m_Condition = nullptr;
		m_Root = nullptr;
		m_ReturnType = RT_DEFAULT;
	}


	BehaviorNode::~BehaviorNode()
	{
		if (m_Condition != nullptr)
			delete m_Condition;

		for (auto it = m_Variables.begin(); it != m_Variables.end(); ++it)
		{
			delete *it;
		}
		m_Variables.clear();
	}

	std::unordered_set<STRING> BehaviorNode::KEY_WORDS = { "Class", "Pos", "NickName" };

	BehaviorNode* BehaviorNode::CreateNodeByName(const STRING& name)
	{
		return NodeFactory::Instance()->Get(name);
	}

//	YBehavior::NodeState BehaviorNode::Execute(AgentPtr pAgent, NodeState parentState)
//	{
//#ifdef YPROFILER
//		Profiler::TreeNodeProfileHelper pfHelper(this);
//		m_pProfileHelper = &pfHelper;
//#endif
//#ifdef YDEBUGGER
//		DebugTreeHelper dbgHelper(pAgent, this);
//		m_pDebugHelper = &dbgHelper;
//#endif
//		NodeState state = NS_INVALID;
//		do
//		{
//
//			if (parentState == NS_RUNNING)
//				m_RunningContext = pAgent->PopRC();
//			else
//				m_RunningContext = nullptr;
//			///> check condition
//			if (m_Condition != nullptr)
//			{
//				bool bBreak = false;
//				PROFILER_PAUSE;
//				state = m_Condition->Execute(pAgent, m_RunningContext && m_RunningContext->IsRunningInCondition() ? NS_RUNNING : NS_INVALID);
//				PROFILER_RESUME;
//
//				switch (state)
//				{
//				case YBehavior::NS_FAILURE:
//					_TryDeleteRC();
//					//DEBUG_RETURN(dbgHelper, res);
//					bBreak = true;
//					break;
//				case YBehavior::NS_RUNNING:
//					TryCreateRC();
//					m_RunningContext->SetRunningInCondition(true);
//					_TryPushRC(pAgent);
//					//DEBUG_RETURN(dbgHelper, res);
//					bBreak = true;
//					break;
//				default:
//					break;
//				}
//				if (m_RunningContext)
//					m_RunningContext->SetRunningInCondition(false);
//
//				if (bBreak)
//					break;
//			}
//
//			///> check breakpoint
//#ifdef YDEBUGGER
//			dbgHelper.TryBreaking();
//#endif
//
//			//////////////////////////////////////////////////////////////////////////
//
//
//			state = this->Update(pAgent);
//			///> If not resume in the Update, resume here
//			PROFILER_RESUME;
//
//			switch (state)
//			{
//			case YBehavior::NS_RUNNING:
//				TryCreateRC();
//				m_RunningContext->SetRunningInCondition(false);
//				_TryPushRC(pAgent);
//				break;
//			default:
//				_TryDeleteRC();
//				break;
//			}
//
//			///> postprocessing
//#ifdef YDEBUGGER
//			dbgHelper.TryPause();
//			m_pDebugHelper = nullptr;
//#endif
//		} while (false);
//
//#ifdef YPROFILER
//		m_pProfileHelper = nullptr;
//#endif
//
//		NodeState finalState = state;
//
//		if (state != NS_RUNNING && state != NS_BREAK)
//		{
//			switch (m_ReturnType)
//			{
//			case YBehavior::RT_INVERT:
//				if (state == NS_SUCCESS)
//					finalState = NS_FAILURE;
//				else if (state == NS_FAILURE)
//					finalState = NS_SUCCESS;
//				break;
//			case YBehavior::RT_SUCCESS:
//				finalState = NS_SUCCESS;
//				break;
//			case YBehavior::RT_FAILURE:
//				finalState = NS_FAILURE;
//				break;
//			default:
//				break;
//			}
//		}
//		DEBUG_RETURN(dbgHelper, state, finalState);
//	}

	YBehavior::NodeState BehaviorNode::Execute(AgentPtr pAgent, NodeState parentState)
	{
		return Update(pAgent);
	}

	bool BehaviorNode::Load(const pugi::xml_node& data)
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

	bool BehaviorNode::LoadChild(const pugi::xml_node& data)
	{
		return OnLoadChild(data);
	}

	void BehaviorNode::LoadFinish()
	{
		OnLoadFinish();
	}

	bool BehaviorNode::AddChild(BehaviorNode* child, const STRING& connection)
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

	bool BehaviorNode::_AddChild(BehaviorNode* child, const STRING& connection)
	{
		ERROR_BEGIN_NODE_HEAD << "Cant add child to this node" << ERROR_END;
		return false;
	}

	bool BehaviorNode::ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, StdVector<STRING>& buffer, SingleType single, char variableType)
	{
		auto tempChar = attri.value();
		///> split all spaces
		Utility::SplitString(tempChar, buffer, Utility::SpaceSpliter);
		if (buffer.size() == 0 || buffer[0].length() < 3)
		{
			ERROR_BEGIN_NODE_HEAD << "Format Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
			return false;
		}

		if (single != ST_NONE)
		{
			if (!((single == ST_SINGLE) ^ (buffer[0][0] == buffer[0][1])))
			{
				ERROR_BEGIN_NODE_HEAD << "Single or Vector Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
				return false;
			}
		}

		if (variableType != 0)
		{
			if (Utility::ToLower(buffer[0][2]) != Utility::ToLower(variableType))
			{
				ERROR_BEGIN_NODE_HEAD << "VariableType Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
				return false;
			}
		}

		if (buffer.size() == 1)
			buffer.push_back("");

		return true;
	}

	TreeNodeContext* BehaviorNode::CreateContext()
	{
		TreeNodeContext* pContext = _CreateContext();
		_InitContext(pContext);
		pContext->Init(this);
		return pContext;
	}


	void BehaviorNode::DestroyContext(TreeNodeContext* pContext)
	{
		pContext->Destroy();
		_DestroyContext(pContext);
	}

	STRING BehaviorNode::GetValue(const STRING& attriName, const pugi::xml_node& data)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN_NODE_HEAD << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return "";
		}
		StdVector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, ST_SINGLE, Utility::CONST_CHAR))
			return "";

		return buffer[1];
	}

	bool BehaviorNode::TryGetValue(const STRING & attriName, const pugi::xml_node & data, STRING& output)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
			return false;
		StdVector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, ST_SINGLE, Utility::CONST_CHAR))
			return false;

		output = buffer[1];
		return true;
	}

	TYPEID BehaviorNode::CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, char variableType, const STRING& defaultCreateStr)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
		{
			if (variableType != Utility::POINTER_CHAR && defaultCreateStr.length() > 0)
			{
				const ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(defaultCreateStr);
				if (helper != nullptr)
				{
					op = helper->CreateVariable();
					m_Variables.push_back(op);

#ifdef YDEBUGGER
					op->SetName(attriName, this->GetClassName());
#endif

					return op->TypeID();
				}
				else
				{
					ERROR_BEGIN_NODE_HEAD << "DefaultCreateStr " << defaultCreateStr << " ERROR for attribute" << attriName << " in " << data.name() << ERROR_END;
					return -1;
				}
			}
			ERROR_BEGIN_NODE_HEAD << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return -1;
		}
		return _CreateVariable(op, attrOptr, data, variableType);
	}

	YBehavior::TYPEID BehaviorNode::CreateVariableIfExist(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, char variableType /*= 0*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
			return -1;
		return _CreateVariable(op, attrOptr, data, variableType);
	}

	TYPEID BehaviorNode::_CreateVariable(ISharedVariableEx*& op, const pugi::xml_attribute& attrOptr, const pugi::xml_node& data, char variableType)
	{
		StdVector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, ST_NONE, variableType))
			return -1;

		const ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
		if (helper != nullptr)
		{
			op = helper->CreateVariable();
			m_Variables.push_back(op);

			///> Vector Index
			if (buffer.size() >= 5 && buffer[2] == "VI")
			{
				op->SetVectorIndex(buffer[3], buffer[4]);
			}

			if (Utility::ToUpper(buffer[0][2]) == Utility::POINTER_CHAR)
			{
				op->SetKeyFromString(buffer[1]);
				op->SetIsLocal(Utility::IsLower(buffer[0][2]));
			}
			else
				op->SetValueFromString(buffer[1]);

#ifdef YDEBUGGER
			op->SetName(attrOptr.name(), this->GetClassName());
#endif

			return op->TypeID();
		}
		else
		{
			ERROR_BEGIN_NODE_HEAD << "Get VariableCreateHelper Failed: " << buffer[0].substr(0, 2) << ERROR_END;
			return -1;
		}
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
#ifdef YDEBUGGER
		delete m_pDebugHelper;
#endif
	}


	void TreeNodeContext::Init(BehaviorNodePtr pNode)
	{
		m_pNode = pNode; 
		m_RootStage = RootStage::None;

		_OnInit();
	}

	void TreeNodeContext::Destroy()
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
				return NS_RUNNING;
			}
			else
			{
				m_RootStage = RootStage::Main;
			}
			break;
		case RootStage::Condition:
			if (lastState == NS_FAILURE)
				state = NS_FAILURE;
			else
			{
				m_RootStage = RootStage::Main;
			}
			break;
		case RootStage::Main:
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
			return state;

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
