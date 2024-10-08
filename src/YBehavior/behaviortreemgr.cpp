#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"
#include <cstring>
#include "YBehavior/tools/common.h"
#include "YBehavior/utility.h"
#include "YBehavior/mgrs.h"
#ifdef YSHARP
#include "YBehavior/sharp/sharputility.h"
#endif

namespace YBehavior
{
#ifdef YSHARP
#define TREE_EXT TOSTRING(.tree)
#else
#define TREE_EXT TOSTRING(.tree)
#endif
	BehaviorTree * TreeMgr::_LoadTree(const STRING& name)
	{
		pugi::xml_document doc;

#ifdef YSHARP
		pugi::xml_parse_result result = doc.load_file(SharpUtility::GetFilePath(name + TREE_EXT).c_str());
#else
		pugi::xml_parse_result result = doc.load_file((m_WorkingDir + name + TREE_EXT).c_str());
#endif
		if (result.status)
		{
			ERROR_BEGIN << "Loading " << name << TREE_EXT ": " << result.description() << ERROR_END;
			return nullptr;
		}

		LOG_BEGIN << "Loading: " << name << TREE_EXT << LOG_END;

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;

		auto isEditor = rootData.attribute("IsEditor");
		if (!isEditor.empty())
		{
			ERROR_BEGIN << "This tree is for Editor Only: " << name << ERROR_END;
			return nullptr;
		}
		BehaviorTree* tree = new BehaviorTree(name);

		std::vector<TreeNode*> treeNodesCache;
		///> node uid starts from 1. push a null as 0
		treeNodesCache.push_back(nullptr);
		if (!_LoadOneNode(tree, rootData.first_child(), treeNodesCache, tree))
		{
			ERROR_BEGIN << "Load tree failed: " << name << ERROR_END;
			delete tree;
			return nullptr;
		}

		if (!tree->ProcessDataConnections(treeNodesCache, rootData.first_child().next_sibling("DataConnections")))
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

	bool TreeMgr::_LoadOneNode(TreeNode* node, const pugi::xml_node& data, std::vector<TreeNode*>& treeNodesCache, BehaviorTree* root)
	{
		UINT uid = (UINT)treeNodesCache.size();
		if (node == nullptr)
		{
			ERROR_BEGIN << "NULL node when load " << data.name() << ", uid " << (uid) << ERROR_END;
			return false;
		}
		node->SetRoot(root);
		node->SetUID(uid);
		treeNodesCache.push_back(node);
		if (!node->Load(data))
		{
			ERROR_BEGIN << "Failed when load " << data.name() << " " << node->GetClassName() << ", uid " << (uid) << ERROR_END;
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
				STRING name(className.value());
				TreeNode* childNode = TreeNode::CreateNodeByName(name);
				if (childNode == nullptr)
				{
					ERROR_BEGIN << "Cant create node " << name << " cause its not registered;" << ERROR_END;
					return false;
				}
				auto connectionName = it->attribute("Connection");
				if (!node->AddChild(childNode, connectionName.value()))
				{
					ERROR_BEGIN << "Failed when add child at " << connectionName.value() << ", uid " << (uid) << ERROR_END;
					delete childNode;
					return false;
				}
				root->AddTreeNodeCount(name);
				if (!_LoadOneNode(childNode, *it, treeNodesCache, root))
					return false;
			}
			else
			{
				if (!node->LoadChild(*it))
				{
					ERROR_BEGIN << "Failed when load child (not node) " << it->name() << ", uid " << (uid) << ERROR_END;
					return false;
				}
			}
		}
		return node->LoadFinish();
	}

	BehaviorTree * TreeMgr::GetTree(const STRING& name)
	{
		BehaviorTree *tree;
		TreeInfoType* info;
		if (!m_VersionMgr.GetData(name, tree, info))
		{
			auto v = info->GetLatestVersion();
			if (v && v->Invalid())
				return nullptr;

			tree = _LoadTree(name);
			info->SetLatest(tree);
			
			if (tree)
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
			auto v = info->GetLatestVersion();
			if (v && v->Invalid())
				return false;
			
			tree = _LoadTree(name);
			info->SetLatest(tree);

			if (!tree)
				return false;
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

#ifndef YSHARP
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
#endif
	void TreeMgr::Clear()
	{
		m_VersionMgr.Clear();
	}

}
