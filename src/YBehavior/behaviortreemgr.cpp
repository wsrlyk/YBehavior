#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodefactory.h"
#include <string.h>
#include "YBehavior/tools/common.h"

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

		BehaviorTree* tree = new BehaviorTree(name);
		TreeKeyMgr::Instance()->SetActiveTree(tree->GetNameKeyMgr(), true);

		UINT uid = 0;
		if (!_LoadOneNode(tree, rootData.first_child(), uid))
		{
			ERROR_BEGIN << "Load xml failed: " << name << ERROR_END;
			return nullptr;
		}

#ifdef DEBUGGER
		xml_string_writer writer;
		rootData.print(writer, PUGIXML_TEXT("\t"), pugi::format_indent | pugi::format_raw);
		writer.result.erase(std::remove_if(writer.result.begin(), writer.result.end(), ::isspace), writer.result.end());
		tree->SetHash(Utility::Hash(writer.result));
#endif
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
		node->LoadFinish();
		return true;
	}

	BehaviorTree* TreeMgr::GetTree(const STRING& name)
	{
		TreeInfo* info;
		auto it = m_Trees.find(name);
		if (it != m_Trees.end())
		{
			BehaviorTree* tree = it->second->GetLatestTree();
			if (tree)
			{
				it->second->ChangeReferenceCount(true);
				return tree;
			}
			else
				info = it->second;
		}
		else
		{
			info = new TreeInfo();
			m_Trees[name] = info;
		}

		
		BehaviorTree* tree = _LoadOneTree(name);
		info->SetLatestTree(tree);
		info->ChangeReferenceCount(true);
		return tree;
	}

	void TreeMgr::ReloadTree(const STRING& name)
	{
		auto it = m_Trees.find(name);
		if (it != m_Trees.end())
		{
			it->second->IncreaseLatestVesion();
		}
	}

	void TreeMgr::ReloadAll()
	{
		for (auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
		{
			it->second->IncreaseLatestVesion();
		}
	}

	void TreeMgr::ReturnTree(BehaviorTree* tree)
	{
		if (tree == nullptr)
			return;

		auto it = m_Trees.find(tree->GetTreeName());
		if (it != m_Trees.end())
		{
			it->second->ChangeReferenceCount(false, tree->GetVersion());
		}
	}

	TreeMgr::~TreeMgr()
	{
		for(auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
			delete it->second;
		m_Trees.clear();
	}

	TreeMgr* TreeMgr::s_Instance;

	void TreeMgr::Print()
	{
		std::cout << "Print all trees" << std::endl;
		for (auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
		{
			std::cout << it->first << std::endl;
			it->second->Print();
		}
		std::cout << "Print all trees end." << std::endl;
	}

	void TreeInfo::Print()
	{
		for (auto it = m_TreeVersions.begin(); it != m_TreeVersions.end(); ++it)
		{
			std::cout << "version " << it->first << ", count " << it->second->referenceCount << std::endl;
		}
	}

	TreeInfo::TreeInfo()
		: m_LatestVersion(nullptr)
	{
		CreateVersion();
	}

	TreeInfo::~TreeInfo()
	{
		for (auto it = m_TreeVersions.begin(); it != m_TreeVersions.end(); ++it)
		{
			delete it->second->tree;
			delete it->second;
		}
		m_TreeVersions.clear();
	}

	TreeVersion* TreeInfo::CreateVersion()
	{
		TreeVersion* pVersion = new TreeVersion();
		if (m_LatestVersion == nullptr)
		{
			pVersion->version = 0;
		}
		else
		{
			pVersion->version = m_LatestVersion->version + 1;

			///> Check if the current latest version has no reference. Remove it if true
			if (m_LatestVersion->referenceCount <= 0)
			{
				int v = m_LatestVersion->version;
				delete m_LatestVersion->tree;
				delete m_LatestVersion;
				m_TreeVersions.erase(v);
			}
		}
		m_LatestVersion = pVersion;
		m_TreeVersions[pVersion->version] = pVersion;

		return pVersion;
	}

	void TreeInfo::IncreaseLatestVesion()
	{
		if (m_LatestVersion == nullptr || m_LatestVersion->tree == nullptr)
			return;

		CreateVersion();
	}

	void TreeInfo::SetLatestTree(BehaviorTree* tree)
	{
		if (m_LatestVersion == nullptr)
			CreateVersion();
		m_LatestVersion->tree = tree;
		tree->SetVersion(m_LatestVersion->version);
	}

	void TreeInfo::ChangeReferenceCount(bool bInc, int versionNum /*= -1*/)
	{
		TreeVersion* version = nullptr;
		if (versionNum < 0)
			version = m_LatestVersion;
		else
		{
			auto it = m_TreeVersions.find(versionNum);
			if (it != m_TreeVersions.end())
				version = it->second;
		}

		if (version == nullptr)
			return;

		if (bInc)
			++(version->referenceCount);
		else
		{
			if (--(version->referenceCount) <= 0 && m_LatestVersion != version)
			{
				///> Old version has no reference, remove it
				delete version->tree;
				delete version;
				m_TreeVersions.erase(versionNum);
			}
		}
	}

}
