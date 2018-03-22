#include "YBehavior/nodes/comparer.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/Agent.h"
#include "YBehavior/nodefactory.h"

namespace YBehavior
{
	Bimap<ComparerOperator, STRING> Comparer::s_OperatorMap = { { CPO_EQUAL, "==" },{ CPO_LARGER, ">" },{ CPO_SMALLER, "<" },{ CPO_INEQUAL, "!=" },{ CPO_NOTLARGER, "<=" },{ CPO_NOTSMALLER, ">=" } };
	std::unordered_set<TypeAB> Comparer::s_ValidTypes = { Types::IntAB, Types::FloatAB, Types::BoolAB, Types::Uint64AB, Types::StringAB, Types::Vector3AB};

	template<typename T>
	NodeState DoOperation(SharedVariable<T>& opl, SharedVariable<T>& opr, ComparerOperator opType, AgentPtr agent, Comparer* pNode)
	{
		STRING strOp;
		Comparer::s_OperatorMap.TryGetValue(opType, strOp);
		LOG_BEGIN << opl.GetValue(agent->GetSharedData()) << " " << strOp << " " << opr.GetValue(agent->GetSharedData()) << " ?" <<LOG_END;

		bool bRes;
		switch(opType)
		{
		case CPO_EQUAL:
			bRes = (opl.GetValue(agent->GetSharedData()) == opr.GetValue(agent->GetSharedData()));
			break;
		case CPO_INEQUAL:
			bRes = (opl.GetValue(agent->GetSharedData()) != opr.GetValue(agent->GetSharedData()));
			break;
		case CPO_LARGER:
			bRes = (opl.GetValue(agent->GetSharedData()) > opr.GetValue(agent->GetSharedData()));
			break;
		case CPO_NOTSMALLER:
			bRes = (opl.GetValue(agent->GetSharedData()) >= opr.GetValue(agent->GetSharedData()));
			break;
		case CPO_NOTLARGER:
			bRes = (opl.GetValue(agent->GetSharedData()) <= opr.GetValue(agent->GetSharedData()));
			break;
		case CPO_SMALLER:
			bRes = (opl.GetValue(agent->GetSharedData()) < opr.GetValue(agent->GetSharedData()));
			break;
		}
		return bRes ? NS_SUCCESS : NS_FAILED;
	}

	void Comparer::OnLoaded(const pugi::xml_node& data)
	{
		///> ÔËËã·û
		auto attrOptr = data.attribute("Operator");
		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Comparer Operator: " << data.name() << ERROR_END;
			return;
		}
		STRING tempChar = GetValue("Operator", data);
		if (!s_OperatorMap.TryGetKey(tempChar, m_Operator))
		{
			ERROR_BEGIN << "Operator Error: " << tempChar << ERROR_END;
			return;
		}

		//////////////////////////////////////////////////////////////////////////
		///> µÈºÅ×ó±ß
		m_DataType = CreateVariable(m_Opl, "Opl", data, true);
		if (m_DataType != Types::IntAB && m_DataType != Types::FloatAB)
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return;
		}
		///> µÈºÅÓÒ±ß1
		m_DataType = CreateVariable(m_Opr, "Opr", data, true);
		if (m_DataType != Types::IntAB && m_DataType != Types::FloatAB)
		{
			ERROR_BEGIN << "Invalid type for Opr in Comparer: " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState Comparer::Update(AgentPtr pAgent)
	{
		switch(m_DataType)
		{
		case Types::IntAB:
			return DoOperation<Int>(*(SharedVariable<Int>*)m_Opl, *(SharedVariable<Int>*)m_Opr, m_Operator, pAgent, this);
			break;
		case Types::FloatAB:
			return DoOperation<Float>(*(SharedVariable<Float>*)m_Opl, *(SharedVariable<Float>*)m_Opr, m_Operator, pAgent, this);
			break;
		case Types::Uint64AB:
			return DoOperation<Uint64>(*(SharedVariable<Uint64>*)m_Opl, *(SharedVariable<Uint64>*)m_Opr, m_Operator, pAgent, this);
			break;
		case Types::StringAB:
			return DoOperation<String>(*(SharedVariable<String>*)m_Opl, *(SharedVariable<String>*)m_Opr, m_Operator, pAgent, this);
			break;
		case Types::Vector3AB:
			return DoOperation<Vector3>(*(SharedVariable<Vector3>*)m_Opl, *(SharedVariable<Vector3>*)m_Opr, m_Operator, pAgent, this);
			break;
		case Types::BoolAB:
			return DoOperation<Bool>(*(SharedVariable<Bool>*)m_Opl, *(SharedVariable<Bool>*)m_Opr, m_Operator, pAgent, this);
			break;
		default:
			return NS_FAILED;
		}
	}

}