#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"

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

	void TreeMgr::LoadOneTree(const STRING& name)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result = doc.load_file((name + ".xml").c_str());
		if (!result)
		{
			LOG_BEGIN << "Load result: " << result.description() << LOG_END;
			return;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return;

		BehaviorNode::GetNodeFactory()->SetActiveTree(name);
		BehaviorTree* tree = new BehaviorTree();
		if (!_LoadOneNode(tree, rootData.first_child()))
		{
			LOG_BEGIN << "Load xml failed: " << name << LOG_END;
		}
		else
		{

		}

	}

	bool TreeMgr::_LoadOneNode(BehaviorNode* node, const pugi::xml_node& data)
	{
		if (node == nullptr)
			return false;

		node->OnLoaded(data);
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			BehaviorNode* childNode = BehaviorNode::CreateNodeByName(it->name());
			if (childNode == nullptr)
			{
				LOG_BEGIN << "Cant create node " << it->name() << " cause its not registered;" << LOG_END;
				continue;
			}

			node->AddChild(childNode);
			_LoadOneNode(childNode, data);
		}

		return true;
	}

	TreeMgr* TreeMgr::s_Instance;

}
