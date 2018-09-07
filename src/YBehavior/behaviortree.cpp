#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/debugger.h"
#include "YBehavior/shareddataex.h"
#include "YBehavior/agent.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
#ifdef DEBUGGER
#define DEBUG_RETURN(helper, res)\
	{\
		NodeState eRes = res;\
		helper.SetResult(eRes);\
		return (eRes);\
	}
#else
#define DEBUG_RETURN(helper, res)\
	return (res)
#endif
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	Bimap<NodeState, STRING, EnumClassHash> s_NodeStateMap = {
		{ NS_SUCCESS, "SUCCESS" },{ NS_FAILURE, "FAILURE" },{ NS_RUNNING, "RUNNING" },{ NS_BREAK, "BREAK" },{ NS_INVALID, "INVALID" }
	};

	BehaviorNode::BehaviorNode()
	{
		m_Parent = nullptr;
		m_Condition = nullptr;
		m_RunningContext = nullptr;
		m_ContextCreator = nullptr;
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
		_TryDeleteRC();
	}

	std::unordered_set<STRING> BehaviorNode::KEY_WORDS = { "Class", "Pos", "NickName" };

	BehaviorNode* BehaviorNode::CreateNodeByName(const STRING& name)
	{
		return NodeFactory::Instance()->Get(name);
	}

	YBehavior::NodeState BehaviorNode::Execute(AgentPtr pAgent, NodeState parentState)
	{
#ifdef DEBUGGER
		DebugHelper dbgHelper(pAgent, this);
		m_pDebugHelper = &dbgHelper;
#endif
		if (parentState == NS_RUNNING)
			m_RunningContext = pAgent->PopRC();
		else
			m_RunningContext = nullptr;
		///> check condition
		if (m_Condition != nullptr)
		{
			NodeState res = m_Condition->Execute(pAgent, m_RunningContext && m_RunningContext->IsRunningInCondition() ? NS_RUNNING : NS_INVALID);
			switch (res)
			{
			case YBehavior::NS_FAILURE:
				_TryDeleteRC();
				DEBUG_RETURN(dbgHelper, res);
				break;
			case YBehavior::NS_RUNNING:
				TryCreateRC();
				m_RunningContext->SetRunningInCondition(true);
				_TryPushRC(pAgent);
				DEBUG_RETURN(dbgHelper, res);
				break;
			default:
				break;
			}
			if (m_RunningContext)
				m_RunningContext->SetRunningInCondition(false);
		}

		///> check breakpoint
#ifdef DEBUGGER
		dbgHelper.TestBreaking();
#endif

		//////////////////////////////////////////////////////////////////////////


		NodeState state = this->Update(pAgent);

		switch (state)
		{
		case YBehavior::NS_RUNNING:
			TryCreateRC();
			m_RunningContext->SetRunningInCondition(false);
			_TryPushRC(pAgent);
			break;
		default:
			_TryDeleteRC();
			break;
		}

		///> postprocessing
#ifdef DEBUGGER
		DEBUG_LOG_INFO(" Return" << " " << s_NodeStateMap.GetValue(state, Types::StringEmpty));
		m_pDebugHelper = nullptr;
#endif

		DEBUG_RETURN(dbgHelper, state);
	}

	bool BehaviorNode::Load(const pugi::xml_node& data)
	{
		return OnLoaded(data);
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

			ERROR_BEGIN << "Too many Condition Node for node " << this->GetNodeInfoForPrint() << ERROR_END;
			return false;
		}
		else
		{
			return _AddChild(child, connection);
		}
	}

	bool BehaviorNode::_AddChild(BehaviorNode* child, const STRING& connection)
	{
		ERROR_BEGIN << "Cant add child to this node: " << ERROR_END;
		return false;
	}

	bool BehaviorNode::ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, std::vector<STRING>& buffer, int single, char variableType)
	{
		auto tempChar = attri.value();
		///> split all spaces
		Utility::SplitString(tempChar, buffer, Utility::SpaceSpliter);
		if (buffer.size() == 0 || buffer[0].length() != 3)
		{
			ERROR_BEGIN << "Format Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
			return false;
		}

		if (single >= 0)
		{
			if (!((single == 1) ^ (buffer[0][0] == buffer[0][1])))
			{
				ERROR_BEGIN << "Single or Vector Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
				return false;
			}
		}

		if (variableType != 0)
		{
			if (buffer[0][2] != variableType)
			{
				ERROR_BEGIN << "VariableType Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
				return false;
			}
		}

		if (buffer.size() == 1)
			buffer.push_back("");

		return true;
	}

	YBehavior::RunningContext* BehaviorNode::_CreateRC() const
	{
		if (m_ContextCreator)
			return m_ContextCreator->NewRC();
		return new RunningContext();
	}

	void BehaviorNode::TryCreateRC()
	{
		if (!m_RunningContext)
		{
			m_RunningContext = _CreateRC();
			m_RunningContext->SetUID(m_UID);
		}
	}

	void BehaviorNode::_TryDeleteRC()
	{
		if (m_RunningContext)
		{
			delete m_RunningContext;
			m_RunningContext = nullptr;
		}
	}

	void BehaviorNode::_TryPushRC(AgentPtr agent)
	{
		if (m_RunningContext && agent)
		{
			agent->PushRC(m_RunningContext);
			m_RunningContext = nullptr;
		}
	}

	void BehaviorNode::_TryPopRC(AgentPtr agent)
	{
		_TryDeleteRC();
		if (agent)
		{
			m_RunningContext = agent->PopRC();
		}
	}

	STRING BehaviorNode::GetValue(const STRING& attriName, const pugi::xml_node& data)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return "";
		}
		std::vector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, 1, Utility::CONST_CHAR))
			return "";

		return buffer[1];
	}

	TYPEID BehaviorNode::CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle, char variableType)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return -1;
		}
		std::vector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, bSingle ? 1 : 0, variableType))
			return -1;

		ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
		if (helper != nullptr)
		{
			op = helper->CreateVariable();
			m_Variables.push_back(op);
#ifdef DEBUGGER
			op->SetName(attriName);
#endif
			///> Vector Index
			if (buffer.size() >= 5 && buffer[2] == "VI")
			{
				op->SetVectorIndex(buffer[3], buffer[4]);
			}

			if (buffer[0][2] == Utility::POINTER_CHAR)
				op->SetKeyFromString(buffer[1]);
			else
				op->SetValueFromString(buffer[1]);

			return op->GetTypeID();
		}
		else
		{
			return -1;
		}
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////


	BehaviorTree::BehaviorTree(const STRING& name)
	{
		m_TreeNameWithPath = name;
		{
			auto it = name.find_last_of('/');
			if (it != STRING::npos)
				m_TreeName = name.substr(it + 1);
			else
				m_TreeName = name;
			it = m_TreeName.find_last_of('\\');
			if (it != STRING::npos)
				m_TreeName = m_TreeName.substr(it + 1);
		}
		m_SharedData = new SharedDataEx();
		//m_NameKeyMgr = new NameKeyMgr();
	}

	BehaviorTree::~BehaviorTree()
	{
		delete m_SharedData;
		//delete m_NameKeyMgr;
	}

	bool BehaviorTree::OnLoaded(const pugi::xml_node& data)
	{
		std::vector<STRING> buffer;

		for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
		{
			if (KEY_WORDS.count(it->name()))
				continue;
			if (!ParseVariable(*it, data, buffer, -1))
				return false;
			ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
			if (helper == nullptr)
				continue;

			helper->SetSharedData(m_SharedData, it->name(), buffer[1]);
		}

		return true;
	}

	void BehaviorTree::CloneData(SharedDataEx& destination)
	{
		destination.Clone(*m_SharedData);
	}


	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	SingleChildNode::SingleChildNode()
		: m_Child(nullptr)
	{

	}

	void SingleChildNode::OnAddChild(BehaviorNode* child, const STRING& connection)
	{
		if (m_Child == nullptr)
			m_Child = child;
		else
		{
			ERROR_BEGIN << "There're more than 1 children for this sinlgechild node: " << GetNodeInfoForPrint() << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState SingleChildNode::Update(AgentPtr pAgent)
	{
		if (m_Child)
			return m_Child->Execute(pAgent, m_RunningContext ? NS_RUNNING : NS_INVALID);

		return NS_FAILURE;
	}


	BranchNode::BranchNode()
	{
		m_Childs = nullptr;
	}

	BranchNode::~BranchNode()
	{
		_DestroyChilds();
	}

	BehaviorNodePtr BranchNode::GetChild(UINT index)
	{
		if (m_Childs && index < m_Childs->size())
			return (*m_Childs)[index];

		return nullptr;
	}


	bool BranchNode::_AddChild(BehaviorNode* child, const STRING& connection)
	{
		if (child == nullptr)
			return false;

		if (!m_Childs)
			m_Childs = new std::vector<BehaviorNodePtr>();

		m_Childs->push_back(child);
		child->SetParent(this);

		OnAddChild(child, connection);

		return true;
	}

	void BranchNode::_DestroyChilds()
	{
		if (m_Childs)
		{
			for(auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
			{
				delete (*it);
			}
			delete m_Childs;
		}
	}

}
