#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodefactory.h"
#include <string.h>

namespace YBehavior
{

	TreeMgr* TreeMgr::Instance()
	{
		if (!s_Instance)
		{
			s_Instance = new TreeMgr();
		}
		return s_Instance;
	}

	BehaviorTree* TreeMgr::_LoadOneTree(const STRING& name)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result = doc.load_file((name + ".xml").c_str());
		if (!result)
		{
			ERROR_BEGIN << "Load result: " << result.description() << ERROR_END;
			return nullptr;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;

		NodeFactory::Instance()->SetActiveTree(name);
		BehaviorTree* tree = new BehaviorTree(name);
		UINT uid = 0;
		if (!_LoadOneNode(tree, rootData.first_child(), uid))
		{
			ERROR_BEGIN << "Load xml failed: " << name << ERROR_END;
			return nullptr;
		}

		return tree;
	}

	bool TreeMgr::_LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID)
	{
		if (node == nullptr)
			return false;

		node->Load(data);
		node->SetUID(++parentUID);
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			if (strcmp(it->name(), "Node") == 0)
			{
				auto className = it->attribute("Class");
				if (className.empty())
				{
					ERROR_BEGIN << "Cant Find Class Name in: " << data.name() << ERROR_END;
					continue;
				}
				BehaviorNode* childNode = BehaviorNode::CreateNodeByName(className.value());
				if (childNode == nullptr)
				{
					ERROR_BEGIN << "Cant create node " << className.value() << " cause its not registered;" << ERROR_END;
					continue;
				}

				auto connectionName = it->attribute("Connection");
				node->AddChild(childNode, connectionName.value());

				_LoadOneNode(childNode, *it, parentUID);
			}
			else
			{

			}
		}

		return true;
	}

	BehaviorTree* TreeMgr::GetTree(const STRING& name)
	{
		auto it = m_Trees.find(name);
		if (it != m_Trees.end())
			return it->second->m_OriginalTree;

		BehaviorTree* tree = _LoadOneTree(name);
		TreeInfo* info = new TreeInfo();
		info->m_OriginalTree = tree;
		m_Trees[name] = info;
		return tree;
	}

	TreeMgr::~TreeMgr()
	{
		for(auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
			delete it->second;
		m_Trees.clear();
	}

	TreeMgr* TreeMgr::s_Instance;


	TreeInfo::~TreeInfo()
	{
		if (m_OriginalTree)
			delete m_OriginalTree;
	}

}
