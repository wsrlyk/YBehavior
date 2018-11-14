#include "YBehavior/nodes/random.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetClassTypeNumberId<Int>(),
		GetClassTypeNumberId<Float>(),
		GetClassTypeNumberId<Bool>(),
	};

	bool Random::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = CreateVariable(m_Opl, "Target", data, ST_SINGLE, Utility::POINTER_CHAR);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return false;
		}
		///> Right1
		TYPEID dataType = CreateVariable(m_Opr1, "Bound1", data, ST_SINGLE);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return false;
		}
		///> Right2
		dataType = CreateVariable(m_Opr2, "Bound2", data, ST_SINGLE);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState Random::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opr1, true);
			LOG_SHARED_DATA(m_Opr2, true);
			LOG_SHARED_DATA(m_Opl, true);
		}

		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		pHelper->Random(pAgent->GetMemory(), m_Opl, m_Opr1, m_Opr2);

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opl, false);
		}

		return NS_SUCCESS;
	}

	bool RandomSelect::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDArray = CreateVariable(m_Input, "Input", data, ST_ARRAY);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeID = CreateVariable(m_Output, "Output", data, ST_SINGLE);
		if (!Utility::IsElement(typeID, typeIDArray))
		{
			ERROR_BEGIN << "RandomSelect types not match " << typeID << " and " << typeIDArray << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState RandomSelect::Update(AgentPtr pAgent)
	{
		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Input, true);

		INT size = m_Input->VectorSize(pAgent->GetMemory());
		if (size == 0)
		{
			return NS_FAILURE;
		}
		INT idx = Utility::Rand(0, size);

		m_Output->SetValue(pAgent->GetMemory(), m_Input->GetElement(pAgent->GetMemory(), idx));

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Output, false);
		return NS_SUCCESS;
	}
}
