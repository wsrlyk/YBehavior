#include "YBehavior/variablecreation.h"
#include "YBehavior/sharedvariablecreatehelper.h"

namespace YBehavior
{

	YBehavior::TYPEID VariableCreation::CreateVariable(TreeNode* pTreeNode, ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst /*= false*/, const STRING& defaultCreateStr /*= Utility::StringEmpty*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
		{
			if (!noConst && defaultCreateStr.length() > 0)
			{
				const ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(defaultCreateStr);
				if (helper != nullptr)
				{
					op = helper->CreateVariable();
					pTreeNode->AddVariable(op);

//#ifdef YDEBUGGER
					op->SetName(attriName, pTreeNode->GetUID(), pTreeNode->GetClassName(), pTreeNode->GetTreeName());
//#endif

					return op->TypeID();
				}
				else
				{
					ERROR_BEGIN_NODE(pTreeNode) << "DefaultCreateStr " << defaultCreateStr << " ERROR for attribute" << attriName << " in " << data.name() << ERROR_END;
					return -1;
				}
			}
			ERROR_BEGIN_NODE(pTreeNode) << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return -1;
		}
		return _CreateVariable(pTreeNode, op, attrOptr, data, noConst);
	}

	YBehavior::TYPEID VariableCreation::CreateVariableIfExist(TreeNode* pTreeNode, ISharedVariableEx*& op, const STRING& attriName, const pugi::xml_node& data, bool noConst /*= false*/)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());
		op = nullptr;
		if (attrOptr.empty())
			return -1;
		return _CreateVariable(pTreeNode, op, attrOptr, data, noConst);
	}

	YBehavior::TYPEID VariableCreation::_CreateVariable(TreeNode* pTreeNode, ISharedVariableEx*& op, const pugi::xml_attribute& attrOptr, const pugi::xml_node& data, bool noConst)
	{
		StdVector<STRING> buffer;
		if (!ParseVariable(pTreeNode, attrOptr, data, buffer, ST_NONE, noConst))
			return -1;

		const ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
		if (helper != nullptr)
		{
			op = helper->CreateVariable();
			pTreeNode->AddVariable(op);

//#ifdef YDEBUGGER
			///> There may be some errors in code below, so we must set some names first
			///> to make the log readable
			op->SetName(attrOptr.name(), pTreeNode->GetUID(), pTreeNode->GetClassName(), pTreeNode->GetTreeName());
//#endif
			///> Vector Index
			if (buffer.size() >= 5 && buffer[2] == "VI")
			{
				op->SetVectorIndex(buffer[3], buffer[4]);
			}

			if (Utility::ToUpper(buffer[0][2]) == Utility::POINTER_CHAR)
			{
				op->SetKeyFromString(buffer[1]);
				op->SetIsLocal(Utility::IsLower(buffer[0][2]));
			}
			else
				op->SetValueFromString(buffer[1]);

//#ifdef YDEBUGGER
			///> Set the final names
			op->SetName(attrOptr.name(), pTreeNode->GetUID(), pTreeNode->GetClassName(), pTreeNode->GetTreeName());
//#endif

			return op->TypeID();
		}
		else
		{
			ERROR_BEGIN_NODE(pTreeNode) << "Get VariableCreateHelper Failed: " << buffer[0].substr(0, 2) << ERROR_END;
			return -1;
		}
	}

	bool VariableCreation::ParseVariable(TreeNode* pTreeNode, const pugi::xml_attribute& attri, const pugi::xml_node& data, StdVector<STRING>& buffer, SingleType single, bool noConst /*= false*/)
	{
		auto tempChar = attri.value();
		///> split all spaces
		Utility::SplitString(tempChar, buffer, Utility::SpaceSpliter);
		if (buffer.size() == 0 || buffer[0].length() < 3)
		{
			ERROR_BEGIN_NODE(pTreeNode) << "Format Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
			return false;
		}

		if (single != ST_NONE)
		{
			if (!((single == ST_SINGLE) ^ (buffer[0][0] == buffer[0][1])))
			{
				ERROR_BEGIN_NODE(pTreeNode) << "Single or Array Error, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
				return false;
			}
		}

		if (noConst)
		{
			//if (Utility::ToLower(buffer[0][2]) != Utility::ToLower(variableType))
			if (Utility::ToUpper(buffer[0][2]) != Utility::POINTER_CHAR)
			{
				ERROR_BEGIN_NODE(pTreeNode) << "Cant be a const variable, " << attri.name() << " in " << data.name() << ": " << tempChar << ERROR_END;
				return false;
			}
		}

		if (buffer.size() == 1)
			buffer.push_back("");

		return true;

	}

	YBehavior::STRING VariableCreation::GetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
		{
			ERROR_BEGIN_NODE(pTreeNode) << "Cant Find Attribute " << attriName << " in " << data.name() << ERROR_END;
			return "";
		}
		StdVector<STRING> buffer;
		if (!ParseVariable(pTreeNode, attrOptr, data, buffer, ST_SINGLE, false))
			return "";

		return buffer[1];
	}

	bool VariableCreation::TryGetValue(TreeNode* pTreeNode, const STRING & attriName, const pugi::xml_node & data, STRING& output)
	{
		const pugi::xml_attribute& attrOptr = data.attribute(attriName.c_str());

		if (attrOptr.empty())
			return false;
		StdVector<STRING> buffer;
		if (!ParseVariable(pTreeNode, attrOptr, data, buffer, ST_SINGLE, false))
			return false;

		output = buffer[1];
		return true;
	}

}
