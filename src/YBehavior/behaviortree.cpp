#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/nodefactory.h"

namespace YBehavior
{

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////
	BehaviorNode::BehaviorNode()
	{
		m_Parent = nullptr;
		m_Childs = nullptr;
	}


	BehaviorNode::~BehaviorNode()
	{
		_DestroyChilds();
	}

	BehaviorNode::BehaviorNodePtr BehaviorNode::GetChild(UINT index)
	{
		if (m_Childs && index < m_Childs->size())
			return (*m_Childs)[index];

		return nullptr;
	}


	void BehaviorNode::AddChild(BehaviorNode* child)
	{
		if (child == nullptr)
			return;

		if (!m_Childs)
			m_Childs = new std::vector<BehaviorNodePtr>();

		m_Childs->push_back(child);
	}

	void BehaviorNode::_DestroyChilds()
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

	BehaviorNode* BehaviorNode::CreateNodeByName(const STRING& name)
	{
		return NodeFactory::Instance()->Get(name);
	}

	YBehavior::NodeState BehaviorNode::Execute(AgentPtr pAgent)
	{
		///> 检查各种条件 或者 断点
		//////////////////////////////////////////////////////////////////////////


		NodeState state = this->Update(pAgent);


		///> 更新后处理

		return state;
	}

	void BehaviorNode::Load(const pugi::xml_node& data)
	{
		OnLoaded(data);
	}

	BehaviorTree::BehaviorTree()
	{
		m_SharedData = new SharedData();
	}

	BehaviorTree::~BehaviorTree()
	{
		delete m_SharedData;
	}

	YBehavior::NodeState BehaviorTree::Update(AgentPtr pAgent)
	{
		return NS_SUCCESS;
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

}
