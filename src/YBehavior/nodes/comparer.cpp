#include "YBehavior/nodes/comparer.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/tools/common.h"
#include "YBehavior/variablecreation.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Bool>(),
		GetTypeID<String>(),
		GetTypeID<Vector3>(),
		GetTypeID<Uint64>(),
		GetTypeID<EntityWrapper>()
	};

	bool Comparer::OnLoaded(const pugi::xml_node& data)
	{
		///> Operator
		if (!VariableCreation::GetValue(this, "Operator", data, IVariableOperationHelper::s_OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = VariableCreation::CreateVariable(this, m_Opl, "Opl", data, true);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID dataType = VariableCreation::CreateVariable(this, m_Opr, "Opr", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Opl & Opr" << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState Comparer::Update(AgentPtr pAgent)
	{
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Opl, true);
			YB_LOG_VARIABLE(m_Opr, true);
		}

		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		return pHelper->Compare(pAgent->GetMemory(), m_Opl, m_Opr, m_Operator) ? NS_SUCCESS : NS_FAILURE;
	}

}
