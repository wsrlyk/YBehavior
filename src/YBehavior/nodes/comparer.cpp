#include "YBehavior/nodes/comparer.h"
#include "YBehavior/agent.h"
#include "YBehavior/variablecreation.h"
#include <set>
namespace YBehavior
{
	static std::set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Bool>(),
		GetTypeID<String>(),
		GetTypeID<Vector3>(),
		GetTypeID<Uint64>(),
		GetTypeID<EntityWrapper>()
	};

	static Bimap<CompareType, STRING> OperatorMap = {
		{ CompareType::EQUAL, "==" },
		{ CompareType::NOT_EQUAL, "!=" },
		{ CompareType::GREATER, ">" },
		{ CompareType::LESS, "<" },
		{ CompareType::LESS_EQUAL, "<=" },
		{ CompareType::GREATER_EQUAL, ">=" }
	};

	bool Comparer::OnLoaded(const pugi::xml_node& data)
	{
		///> Operator
		if (!VariableCreation::GetValue(this, "Operator", data, OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		auto dataTypeL = VariableCreation::CreateVariable(this, m_Opl, "Opl", data, true);
		if (s_ValidTypes.find(dataTypeL) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Opl in Comparer: " << dataTypeL << ERROR_END;
			return false;
		}
		///> Right
		auto dataTypeR = VariableCreation::CreateVariable(this, m_Opr, "Opr", data);
		if (dataTypeL != dataTypeR)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Opl & Opr" << ERROR_END;
			return false;
		}

		m_pHelper = VariableCompareMgr::Instance()->Get(dataTypeL);
		if (!m_pHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "This type is not supported by ComparerNode." << ERROR_END;
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

		return m_pHelper->Compare(pAgent->GetMemory(), m_Opl, m_Opr, m_Operator) ? NS_SUCCESS : NS_FAILURE;
	}

}
