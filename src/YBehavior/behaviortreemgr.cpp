#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodefactory.h"
#include <string.h>
#include "YBehavior/tools/common.h"
#include "YBehavior/tools/treemgrhelper.hpp"
#include "YBehavior/mgrs.h"

namespace YBehavior
{
#ifdef SHARP
#define TREE_EXT TOSTRING(.bytes)
#else
#define TREE_EXT TOSTRING(.tree)
#endif
	BehaviorTree * TreeMgr::_LoadTree(const STRING& name)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result;
		if (m_LoadDataCallback != nullptr)
			result = doc.load_string(m_LoadDataCallback((m_WorkingDir + name + TREE_EXT ).c_str()));
		else
			result = doc.load_file((m_WorkingDir + name + TREE_EXT).c_str());
		if (result.status)
		{
			ERROR_BEGIN << "Loading " << name << TREE_EXT ": " << result.description() << ERROR_END;
			return nullptr;
		}

		LOG_BEGIN << "Loading: " << name << TREE_EXT << LOG_END;

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;

		BehaviorTree* tree = new BehaviorTree(name);

		UINT uid = 0;
		if (!_LoadOneNode(tree, rootData.first_child(), uid, tree))
		{
			ERROR_BEGIN << "Load tree failed: " << name << ERROR_END;
			delete tree;
			return nullptr;
		}

#ifdef YDEBUGGER
		xml_string_writer writer;
		rootData.print(writer, PUGIXML_TEXT("\t"), pugi::format_indent | pugi::format_raw);
		writer.result.erase(std::remove_if(writer.result.begin(), writer.result.end(), ::isspace), writer.result.end());
		tree->SetHash(Utility::Hash(writer.result));
#endif
		return tree;
	}

	bool TreeMgr::_LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID, BehaviorTree* root)
	{
		if (node == nullptr)
		{
			ERROR_BEGIN << "NULL node when load " << data.name() << ", uid " << (parentUID + 1) << ERROR_END;
			return false;
		}
		node->SetRoot(root);
		node->SetUID(++parentUID);
		if (!node->Load(data))
		{
			ERROR_BEGIN << "Failed when load " << data.name() << ", uid " << (parentUID) << ERROR_END;
			return false;
		}
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			if (strcmp(it->name(), "Node") == 0)
			{
				auto className = it->attribute("Class");
				if (className.empty())
				{
					ERROR_BEGIN << "Cant Find Class Name in: " << data.name() << ERROR_END;
					return false;
				}
				BehaviorNode* childNode = BehaviorNode::CreateNodeByName(className.value());
				if (childNode == nullptr)
				{
					ERROR_BEGIN << "Cant create node " << className.value() << " cause its not registered;" << ERROR_END;
					return false;
				}

				auto connectionName = it->attribute("Connection");
				if (!node->AddChild(childNode, connectionName.value()))
				{
					ERROR_BEGIN << "Failed when add child at " << connectionName.value() << ", uid " << (parentUID) << ERROR_END;
					delete childNode;
					return false;
				}

				if (!_LoadOneNode(childNode, *it, parentUID, root))
					return false;
			}
			else
			{
				if (!node->LoadChild(*it))
				{
					ERROR_BEGIN << "Failed when load child (not node) " << it->name() << ", uid " << (parentUID) << ERROR_END;
					return false;
				}
			}
		}
		node->LoadFinish();
		return true;
	}

	BehaviorTree * TreeMgr::GetTree(const STRING& name)
	{
		BehaviorTree *tree;
		TreeInfoType* info;
		if (!m_VersionMgr.GetData(name, tree, info))
		{
			tree = _LoadTree(name);
			if (!tree)
				return nullptr;

			info->SetLatest(tree);
			info->ChangeReferenceCount(true);

		}

		return tree;
	}

	bool TreeMgr::LoadTree(const STRING& name, const TreeMap*& pOutputTreeMap)
	{
		BehaviorTree *tree;
		TreeInfoType* info;
		if (!m_VersionMgr.GetData(name, tree, info))
		{
			tree = _LoadTree(name);
			if (!tree)
				return false;

			info->SetLatest(tree);
		}

		pOutputTreeMap = &tree->GetTreeMap();
		return true;
	}

	void TreeMgr::ReloadTree(const STRING& name)
	{
		m_VersionMgr.Reload(name);
	}

	void TreeMgr::ReloadAll()
	{
		m_VersionMgr.ReloadAll();
	}

	void TreeMgr::ReturnTree(BehaviorTree* tree)
	{
		m_VersionMgr.Return(tree);
	}

	TreeMgr::~TreeMgr()
	{
	}

	//TreeMgr* TreeMgr::s_Instance;

	void TreeMgr::Print()
	{
		std::cout << "Print all trees" << std::endl;
		for (auto it = m_VersionMgr.GetInfos().begin(); it != m_VersionMgr.GetInfos().end(); ++it)
		{
			std::cout << it->first << std::endl;
			it->second->Print();
		}
		std::cout << "Print all trees end." << std::endl;
	}

	void TreeMgr::SetWorkingDir(const STRING& dir)
	{
		m_WorkingDir = dir;

		if (m_WorkingDir == "")
			return;

		size_t len = m_WorkingDir.length();
		if (m_WorkingDir[len - 1] != '\\' && m_WorkingDir[len - 1] != '/')
		{
			m_WorkingDir.append(1, '/');
		}
	}

	void TreeMgr::Clear()
	{
		m_VersionMgr.Clear();
	}

}
