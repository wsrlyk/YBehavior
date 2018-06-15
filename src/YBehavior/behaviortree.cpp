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

	YBehavior::NodeState BehaviorNode::Execute(AgentPtr pAgent)
	{
#ifdef DEBUGGER
		DebugHelper dbgHelper(pAgent, this);
		m_pDebugHelper = &dbgHelper;
#endif
		///> check condition
		if (m_Condition != nullptr)
		{
			NodeState res = m_Condition->Execute(pAgent);
			if (res == NS_FAILURE)
				DEBUG_RETURN(dbgHelper, res);
		}

		///> check breakpoint
#ifdef DEBUGGER
		dbgHelper.TryHitBreakPoint();
#endif

		//////////////////////////////////////////////////////////////////////////


		NodeState state = this->Update(pAgent);
		m_State = state;

		///> postprocessing
#ifdef DEBUGGER
		DEBUG_LOG_INFO(" Return" << " " << s_NodeStateMap.GetValue(state, Utility::StringEmpty));
		m_pDebugHelper = nullptr;
#endif

		DEBUG_RETURN(dbgHelper, state);
	}

	void BehaviorNode::Load(const pugi::xml_node& data)
	{
		OnLoaded(data);
	}

	void BehaviorNode::AddChild(BehaviorNode* child, const STRING& connection)
	{
		if (connection == "condition")
		{
			m_Condition = child;
			child->SetParent(this);
		}
		else
		{
			_AddChild(child, connection);
		}
	}

	void BehaviorNode::_AddChild(BehaviorNode* child, const STRING& connection)
	{
		ERROR_BEGIN << "Cant add child to this node: " << ERROR_END;
	}

	BehaviorNode::BehaviorNodePtr BehaviorNode::GetRoot()
	{
		BehaviorNodePtr root = this;
		BehaviorNodePtr parent = GetParent();
		while (parent != nullptr)
		{
			root = parent;
			parent = parent->GetParent();
		}
		return root;
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

	STRING BehaviorNode::GetValue(const STRING& attriName, const pugi::xml_node& data)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return "";
		}
		std::vector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, 1, CONST))
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
			op->SetName(attriName);
			///> Vector Index
			if (buffer.size() >= 5 && buffer[2] == "VI")
			{
				op->SetVectorIndex(buffer[3], buffer[4]);
			}

			if (buffer[0][2] == POINTER)
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
		m_TreeName = name;
		m_SharedData = new SharedDataEx();
	}

	BehaviorTree::~BehaviorTree()
	{
		delete m_SharedData;
	}

	void BehaviorTree::OnLoaded(const pugi::xml_node& data)
	{
		std::vector<STRING> buffer;

		for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
		{
			if (KEY_WORDS.count(it->name()))
				continue;
			if (!ParseVariable(*it, data, buffer, -1))
				continue;
			ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
			if (helper == nullptr)
				continue;

			helper->SetSharedData(m_SharedData, it->name(), buffer[1]);
		}
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
			return m_Child->Execute(pAgent);

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

	BehaviorNode::BehaviorNodePtr BranchNode::GetChild(UINT index)
	{
		if (m_Childs && index < m_Childs->size())
			return (*m_Childs)[index];

		return nullptr;
	}


	void BranchNode::_AddChild(BehaviorNode* child, const STRING& connection)
	{
		if (child == nullptr)
			return;

		if (!m_Childs)
			m_Childs = new std::vector<BehaviorNodePtr>();

		m_Childs->push_back(child);
		child->SetParent(this);

		OnAddChild(child, connection);
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
