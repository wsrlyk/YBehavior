#ifndef _YBEHAVIOR_VARIABLECREATION_H_
#define _YBEHAVIOR_VARIABLECREATION_H_

#include "YBehavior/types.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/treenode.h"
#include "YBehavior/tools/bimap.h"

namespace YBehavior
{
	class ISharedVariableEx;
	class TreeNode;
	class VariableCreation
	{
	public:
		///> If no config, a default CONST variable will be created
		static TYPEID CreateVariable(TreeNode* pTreeNode, ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false, const STRING& defaultCreateStr = Utility::StringEmpty);
		///> If no config, a default CONST variable will be created
		template <typename T>
		static TYPEID CreateVariable(TreeNode* pTreeNode, SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false);
		///> If no config, variable will NOT be created
		static TYPEID CreateVariableIfExist(TreeNode* pTreeNode, ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false);
		///> If no config, variable will NOT be created
		template <typename T>
		static TYPEID CreateVariableIfExist(TreeNode* pTreeNode, SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false);

		
		static STRING GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data);
		static bool TryGetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, STRING& output);
		template<typename T>
		static bool GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T defaultValue, T& outValue);
		template<typename T>
		static bool GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T& outValue);
		static bool ParseVariable(TreeNode* pTreeNode, const pugi::xml_attribute& attri, const pugi::xml_node& data, StdVector<STRING>& buffer, SingleType single, bool noConst = false);

	private:
		static TYPEID _CreateVariable(TreeNode* pTreeNode, ISharedVariableEx*& op, const pugi::xml_attribute& attrOptr, const pugi::xml_node& data, bool noConst);
	};

	template<typename T>
	bool VariableCreation::GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T& outValue)
	{
		STRING s(GetValue(pTreeNode, attriName, data));
		if (strMap.TryGetKey(s, outValue))
			return true;

		ERROR_BEGIN_NODE(pTreeNode) << attriName << " Error: " << s << ERROR_END;
		return false;
	}

	template<typename T>
	bool VariableCreation::GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING, EnumClassHash>& strMap, T defaultValue, T& outValue)
	{
		STRING s;
		if (!TryGetValue(pTreeNode, attriName, data, s))
		{
			outValue = defaultValue;
			return true;
		}

		if (strMap.TryGetKey(s, outValue))
			return true;

		ERROR_BEGIN_NODE(pTreeNode) << attriName << " Error: " << s << ERROR_END;
		return false;
	}

	template <typename T>
	TYPEID VariableCreation::CreateVariableIfExist(TreeNode* pTreeNode, SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst /*= false*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
			return -1;

		ISharedVariableEx* pTemp = nullptr;
		TYPEID typeID = _CreateVariable(pTreeNode, pTemp, attrOptr, data, noConst);
		if (typeID == GetTypeID<T>())
		{
			op = (SharedVariableEx<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN_NODE(pTreeNode) << "Invalid type for " << attriName << " with type " << typeID << ERROR_END;
		}
		return typeID;
	}

	template <typename T>
	TYPEID VariableCreation::CreateVariable(TreeNode* pTreeNode, SharedVariableEx<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst /*= false*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
		{
			if (!noConst)
			{
				op = new SharedVariableEx<T>();
				pTreeNode->AddVariable(op);
//#ifdef YDEBUGGER
				op->SetName(attriName, pTreeNode->GetUID(), pTreeNode->GetClassName(), pTreeNode->GetTreeName());
//#endif
				return GetTypeID<T>();
			}

			ERROR_BEGIN_NODE(pTreeNode) << "Cant create default variable for " << attriName << " with typeid = " << GetTypeID<T>() << ERROR_END;
			return -1;
		}

		ISharedVariableEx* pTemp = nullptr;
		TYPEID typeID = _CreateVariable(pTreeNode, pTemp, attrOptr, data, noConst);
		if (typeID == GetTypeID<T>())
		{
			op = (SharedVariableEx<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN_NODE(pTreeNode) << "Invalid type for " << attriName << " with type " << typeID << ERROR_END;
		}
		return typeID;

	}

}

#endif
