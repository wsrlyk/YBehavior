#include "YBehavior/behaviortree.h"
#include "YBehavior/nodes/Sequence.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>

namespace YBehavior
{
	INT NodeFactory::CreateIndexByName(const STRING& name)
	{
		auto it = mCommonNameIndexInfo.mNameHash.find(name);
		if (it != mCommonNameIndexInfo.mNameHash.end())
			return it->second;

		if (mpCurActiveNameIndexInfo == NULL)
			return -1;

		///> common已经找过，不用再找一遍
		if (mpCurActiveNameIndexInfo == &mCommonNameIndexInfo || mpCurActiveNameIndexInfo->mNameHash.find(name) == mpCurActiveNameIndexInfo->mNameHash.end())
		{
			LOG_BEGIN << "ADD node: " << name << "index: " << mpCurActiveNameIndexInfo->mNameIndex << LOG_END;
			mpCurActiveNameIndexInfo->mNameHash[name] = mpCurActiveNameIndexInfo->mNameIndex;
			mpCurActiveNameIndexInfo->mNameIndex ++;
		}

		return mpCurActiveNameIndexInfo->mNameHash[name];
	}
	void NodeFactory::SetActiveTree(const STRING& tree)
	{
		LOG_BEGIN << "SetActiveTree: " << tree.c_str() << LOG_END;

		mCurActiveTreeName = tree;

		if (tree.empty())
		{
			mpCurActiveNameIndexInfo = &mCommonNameIndexInfo;
			return;
		}
		else
		{
			mpCurActiveNameIndexInfo = &mTempNameIndexInfo;
		}

		mpCurActiveNameIndexInfo->Reset();
		mpCurActiveNameIndexInfo->mNameIndex = mCommonNameIndexInfo.mNameIndex;
	}

	NodeFactory::NodeFactory()
		: Factory<BehaviorNode>()
	{
		mCommonNameIndexInfo.Reset();
		mTempNameIndexInfo.Reset();
	}

	NodeFactory* CreateNodeFactory()
	{
		NodeFactory* factory = new NodeFactory();
		REGISTER_TYPE(factory, Sequence);

		return factory;
	}

	NodeFactory* BehaviorNode::s_NodeFactory = CreateNodeFactory();

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
		return s_NodeFactory->Get(name);
	}


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
				INT index = BehaviorNode::GetNodeFactory()->CreateIndexByName(truename);

				STRING prefix(name.begin(), name.begin() + 2);	///> 两个字符
				
				if (prefix == "_F")
				{
					m_SharedData->SetFloat(index, it->as_float());
				}
				else if (prefix == "_B")
				{
					m_SharedData->SetBool(index, it->as_bool());
				}
				else if (prefix == "_I")
				{
					m_SharedData->SetInt(index, it->as_int());
				}
				else if (prefix == "_S")
				{
					m_SharedData->SetString(index, it->value());
				}
				else if (prefix == "_V")
				{
					Vector3 tempVec;
					Utility::SplitString(it->value(), buffer, Utility::SequenceSpliter);
					Utility::CreateVector3(buffer, tempVec);
					m_SharedData->SetVector3(index, std::move(tempVec));
				}
				else
				{
					std::vector<STRING> buffer2;
					Utility::SplitString(it->value(), buffer2, Utility::ListSpliter);
					std::stringstream ss;
					if (prefix == "FF")
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
						m_SharedData->SetVecFloat(index, std::move(ffs));
					}
					else if (prefix == "BB")
					{
						std::vector<bool> bbs;
						for (auto it = buffer2.begin(); it != buffer2.end(); ++it)
						{
							float temp;
							ss << *it;
							ss >> temp;
							bbs.push_back(temp);
						}
						m_SharedData->SetVecBool(index, std::move(bbs));
					}
				}
			}
		}
	}

}
