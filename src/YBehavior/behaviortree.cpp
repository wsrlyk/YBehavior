#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	BehaviorNode::BehaviorNode()
	{
		m_Parent = nullptr;
	}


	BehaviorNode::~BehaviorNode()
	{
	}

	std::unordered_set<STRING> BehaviorNode::KEY_WORDS = { "Class", "Pos", "NickName" };

	BehaviorNode* BehaviorNode::CreateNodeByName(const STRING& name)
	{
		return NodeFactory::Instance()->Get(name);
	}

	YBehavior::NodeState BehaviorNode::Execute(AgentPtr pAgent)
	{
		///> 检查各种条件 或者 断点
		//////////////////////////////////////////////////////////////////////////


		NodeState state = this->Update(pAgent);
		m_State = state;

		///> 更新后处理

		return state;
	}

	void BehaviorNode::Load(const pugi::xml_node& data)
	{
		OnLoaded(data);
	}

	void BehaviorNode::AddChild(BehaviorNode* child, const STRING& connection)
	{
		ERROR_BEGIN << "Cant add child to this node: " << ERROR_END;
	}

	bool BehaviorNode::ParseVariable(const pugi::xml_attribute& attri, const pugi::xml_node& data, std::vector<STRING>& buffer, int single)
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

		if (buffer.size() == 1)
			buffer.push_back("");

		return true;
	}

	STRING BehaviorNode::GetValue(const STRING& attriName, const pugi::xml_node& data)
	{
		pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return "";
		}
		std::vector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, 1))
			return "";

		return buffer[1];
	}
	int BehaviorNode::CreateVariable(ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle)
	{
		pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return -1;
		}
		std::vector<STRING> buffer;
		if (!ParseVariable(attrOptr, data, buffer, bSingle ? 1 : 0))
			return -1;

		ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
		if (helper != nullptr)
		{
			op = helper->CreateVariable();

			///> Vector Index
			if (buffer.size() >= 5 && buffer[2] == "VI")
			{
				op->SetVectorIndex(buffer[3], buffer[4]);
			}

			if (buffer[0][2] == 'S')
				op->SetIndexFromString(buffer[1]);
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


	BehaviorTree::BehaviorTree()
	{
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

		return NS_FAILED;
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


	void BranchNode::AddChild(BehaviorNode* child, const STRING& connection)
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
