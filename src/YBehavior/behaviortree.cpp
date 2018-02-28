#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariablecreatehelper.h"

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

	void BehaviorNode::AddChild(BehaviorNode* child)
	{
		ERROR_BEGIN << "Cant add child to this node: " << ERROR_END;
	}

	TypeAB BehaviorNode::CreateVariable(ISharedVariable*& op, const STRING& attriName, const pugi::xml_node& data, bool bSingle)
	{
		pugi::xml_attribute& attrOptr = data.attribute("Opl");

		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return Types::NoneAB;
		}
		std::vector<STRING> buffer;

		auto tempChar = attrOptr.value();
		///> Only split the first space
		Utility::SplitString(tempChar, buffer, Utility::SpaceSpliter, 1);
		if (buffer.size() != 2 || buffer[0].length() != 3)
		{
			ERROR_BEGIN << "Format Error, " << attriName << " in " << data.name() << ": " << tempChar << ERROR_END;
			return Types::NoneAB;
		}

		if (!(bSingle ^ (buffer[0][0] == buffer[0][1])))
		{
			ERROR_BEGIN << "Single or Vector Error, " << attriName << " in " << data.name() << ": " << tempChar << ERROR_END;
			return Types::NoneAB;
		}

		ISharedVariableCreateHelper* helper = Utility::CreateVariableCreateHelper(buffer[0][1], buffer[0][0]);
		if (helper != NULL)
		{
			op = helper->CreateVariable();
			if (buffer[0][2] == 'S')
				helper->SetIndex(op, buffer[1]);
			else
				op->SetValueFromString(buffer[1]);

			TypeAB type = helper->GetType();
			delete helper;
			return type;
		}
		else
		{
			return Types::NoneAB;
		}
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////


	BehaviorTree::BehaviorTree()
	{
		m_SharedData = new SharedData();
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
			STRING name(it->name());
			if(name.size() >= 4)
			{
				STRING truename(name.begin() + 3, name.end());

				STRING prefix(name.begin(), name.begin() + 2);	///> 两个字符
				
				if (prefix[0] == '_')
				{
					switch(prefix[1])
					{
					case 'F':
						{
							INT index = NodeFactory::Instance()->CreateIndexByName<Float>(truename);
							m_SharedData->SetFloat(index, it->as_float());
						}
						break;
					case 'B':
						{
							INT index = NodeFactory::Instance()->CreateIndexByName<Bool>(truename);
							m_SharedData->SetBool(index, it->as_bool());
						}
						break;
					case 'I':
						{
							INT index = NodeFactory::Instance()->CreateIndexByName<Int>(truename);
							m_SharedData->SetInt(index, it->as_int());
						}
						break;
					case 'L':
						{
							INT index = NodeFactory::Instance()->CreateIndexByName<Uint64>(truename);
							m_SharedData->SetUint64(index, it->as_ullong());
						}
						break;
					case 'S':
						{
							INT index = NodeFactory::Instance()->CreateIndexByName<String>(truename);
							m_SharedData->SetString(index, it->value());
						}
						break;
					case 'V':
						{
							Vector3 tempVec;
							Utility::SplitString(it->value(), buffer, Utility::SequenceSpliter);
							Utility::CreateVector3(buffer, tempVec);
							INT index = NodeFactory::Instance()->CreateIndexByName<Vector3>(truename);
							m_SharedData->SetVector3(index, std::move(tempVec));
						}
						break;
					default:
						break;
					}
				}
				else
				{
					std::vector<STRING> buffer2;
					Utility::SplitString(it->value(), buffer2, Utility::ListSpliter);
					std::stringstream ss;
					switch(prefix[1])
					{
					case 'F':
						{
							std::vector<float> ffs;
							for (auto it = buffer2.begin(); it != buffer2.end(); ++it)
							{
								float temp;
								ss.clear();
								ss << *it;
								ss >> temp;
								ffs.push_back(temp);
							}
							INT index = NodeFactory::Instance()->CreateIndexByName<VecFloat>(truename);
							m_SharedData->SetVecFloat(index, std::move(ffs));
						}
						break;
					case 'B':
						{
							std::vector<bool> bbs;
							for (auto it = buffer2.begin(); it != buffer2.end(); ++it)
							{
								bool temp;
								ss << *it;
								ss >> temp;
								bbs.push_back(temp);
							}
							INT index = NodeFactory::Instance()->CreateIndexByName<VecBool>(truename);
							m_SharedData->SetVecBool(index, std::move(bbs));
						}
						break;
					default:
						break;
					}
				}
			}
		}
	}

	void BehaviorTree::CloneData(SharedData& destination)
	{
		destination.Clone(*m_SharedData);
	}


	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	SingleChildNode::SingleChildNode()
		: m_Child(nullptr)
	{

	}

	void SingleChildNode::OnAddChild(BehaviorNode* child)
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


	void BranchNode::AddChild(BehaviorNode* child)
	{
		if (child == nullptr)
			return;

		if (!m_Childs)
			m_Childs = new std::vector<BehaviorNodePtr>();

		m_Childs->push_back(child);
		child->SetParent(this);

		OnAddChild(child);
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
