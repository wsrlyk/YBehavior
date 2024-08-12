#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/treenode.h"
#include "YBehavior/tools/bimap.h"

namespace YBehavior
{
	class IPin;
	class TreeNode;
	class PinCreation
	{
	public:
		///> If no config, a default CONST pin will be created
		static TYPEID CreatePin(TreeNode* pTreeNode, IPin*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false, const STRING& defaultCreateStr = Utility::StringEmpty);
		///> If no config, a default CONST pin will be created
		template <typename T>
		static TYPEID CreatePin(TreeNode* pTreeNode, Pin<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false);
		///> If no config, pin will NOT be created
		static TYPEID CreatePinIfExist(TreeNode* pTreeNode, IPin*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false);
		///> If no config, pin will NOT be created
		template <typename T>
		static TYPEID CreatePinIfExist(TreeNode* pTreeNode, Pin<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst = false);

		
		static STRING GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data);
		static bool TryGetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, STRING& output);
		template<typename T>
		static bool GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING>& strMap, T defaultValue, T& outValue);
		template<typename T>
		static bool GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING>& strMap, T& outValue);
		static bool ParsePin(TreeNode* pTreeNode, const pugi::xml_attribute& attri, const pugi::xml_node& data, StdVector<STRING>& buffer, SingleType single, bool noConst = false);

	private:
		static TYPEID _CreatePin(TreeNode* pTreeNode, IPin*& op, const pugi::xml_attribute& attrOptr, const pugi::xml_node& data, bool noConst);
	};

	template<typename T>
	bool PinCreation::GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING>& strMap, T& outValue)
	{
		STRING s(GetValue(pTreeNode, attriName, data));
		if (strMap.TryGetKey(s, outValue))
			return true;

		ERROR_BEGIN_NODE(pTreeNode) << attriName << " Error: " << s << ERROR_END;
		return false;
	}

	template<typename T>
	bool PinCreation::GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, const Bimap<T, STRING>& strMap, T defaultValue, T& outValue)
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
	TYPEID PinCreation::CreatePinIfExist(TreeNode* pTreeNode, Pin<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst /*= false*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
			return -1;

		IPin* pTemp = nullptr;
		TYPEID typeID = _CreatePin(pTreeNode, pTemp, attrOptr, data, noConst);
		if (typeID == GetTypeID<T>())
		{
			op = (Pin<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN_NODE(pTreeNode) << "Invalid type for " << attriName << " with type " << typeID << ERROR_END;
		}
		return typeID;
	}

	template <typename T>
	TYPEID PinCreation::CreatePin(TreeNode* pTreeNode, Pin<T>*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst /*= false*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
		{
			if (!noConst)
			{
				op = new Pin<T>();
				pTreeNode->AddPin(op);
//#ifdef YDEBUGGER
				op->SetName(attriName, pTreeNode->GetUID(), pTreeNode->GetClassName(), pTreeNode->GetTreeName());
//#endif
				return GetTypeID<T>();
			}

			ERROR_BEGIN_NODE(pTreeNode) << "Cant create default pin for " << attriName << " with typeid = " << GetTypeID<T>() << ERROR_END;
			return -1;
		}

		IPin* pTemp = nullptr;
		TYPEID typeID = _CreatePin(pTreeNode, pTemp, attrOptr, data, noConst);
		if (typeID == GetTypeID<T>())
		{
			op = (Pin<T>*)pTemp;
		}
		else
		{
			op = nullptr;
			ERROR_BEGIN_NODE(pTreeNode) << "Invalid type for " << attriName << " with type " << typeID << ERROR_END;
		}
		return typeID;

	}

}
