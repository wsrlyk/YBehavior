#include "YBehavior/nodes/calculator.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/Agent.h"
#include "YBehavior/nodefactory.h"

namespace YBehavior
{
	Calculator::Calculator()
	{
	}


	Calculator::~Calculator()
	{
	}

	//template<typename T>
	//INT CreateIndexByName(const STRING& name)
	//{
	//	return NodeFactory::Instance()->CreateIndexByName<T>(name);
	//}

	//typedef INT (*CreateIndexFunc)(const STRING& name);

	template<typename T>
	NodeState DoOperation(SharedVariable<T>& opl, SharedVariable<T>& opr1, SharedVariable<T>& opr2, CalculatorOperator opType, AgentPtr agent, Calculator* pNode)
	{
		switch(opType)
		{
		case CO_ADD:
			opl.SetValue(agent->GetSharedData(), opr1.GetValue(agent->GetSharedData()) + opr2.GetValue(agent->GetSharedData()));
			break;
		case CO_SUB:
			opl.SetValue(agent->GetSharedData(), opr1.GetValue(agent->GetSharedData()) - opr2.GetValue(agent->GetSharedData()));
			break;
		case CO_MUL:
			opl.SetValue(agent->GetSharedData(), opr1.GetValue(agent->GetSharedData()) * opr2.GetValue(agent->GetSharedData()));
			break;
		case CO_DIV:
			{
				T t = opr2.GetValue(agent->GetSharedData());
				if (t == 0)
				{
					ERROR_BEGIN << "Divided by zero: " << pNode->GetNodeInfoForPrint() << ERROR_END;
					return NS_FAILED;
				}
				opl.SetValue(agent->GetSharedData(), opr1.GetValue(agent->GetSharedData()) / t );
			}
			break;
		}

		LOG_BEGIN << opl.GetValue(agent->GetSharedData()) << "<=" << opr1.GetValue(agent->GetSharedData()) << " " << opType << " " << opr2.GetValue(agent->GetSharedData()) << LOG_END;
		return NS_SUCCESS;
	}

	void Calculator::OnLoaded(const pugi::xml_node& data)
	{
		///> ‘ÀÀ„∑˚
		auto attrOptr = data.attribute("Operator");
		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Calculator Operator: " << data.name() << ERROR_END;
			return;
		}
		auto tempChar = attrOptr.value();
		if (strcmp(tempChar, "ADD") == 0)
		{
			m_Operator = CO_ADD;
		}
		else if (strcmp(tempChar, "SUB") == 0)
		{
			m_Operator = CO_SUB;
		}
		else if (strcmp(tempChar, "MUL") == 0)
		{
			m_Operator = CO_MUL;
		}
		else if (strcmp(tempChar, "DIV") == 0)
		{
			m_Operator = CO_DIV;
		}
		//////////////////////////////////////////////////////////////////////////
		///> µ»∫≈◊Û±ﬂ
		m_DataType = CreateVariable(m_Opl, "Opl", data, true);
		if (m_DataType != Types::IntAB && m_DataType != Types::FloatAB)
		{
			ERROR_BEGIN << "Invalid type for Opl in calculator: " << m_DataType << ERROR_END;
			return;
		}
		///> µ»∫≈”“±ﬂ1
		m_DataType = CreateVariable(m_Opr1, "Opr1", data, true);
		if (m_DataType != Types::IntAB && m_DataType != Types::FloatAB)
		{
			ERROR_BEGIN << "Invalid type for Opr1 in calculator: " << m_DataType << ERROR_END;
			return;
		}
		///> µ»∫≈”“±ﬂ2
		m_DataType = CreateVariable(m_Opr2, "Opr2", data, true);
		if (m_DataType != Types::IntAB && m_DataType != Types::FloatAB)
		{
			ERROR_BEGIN << "Invalid type for Opr2 in calculator: " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState Calculator::Update(AgentPtr pAgent)
	{
		switch(m_DataType)
		{
		case Types::IntAB:
			return DoOperation<Int>(*(SharedVariable<Int>*)m_Opl, *(SharedVariable<Int>*)m_Opr1, *(SharedVariable<Int>*)m_Opr2, m_Operator, pAgent, this);
			break;
		case Types::FloatAB:
			return DoOperation<Float>(*(SharedVariable<Float>*)m_Opl, *(SharedVariable<Float>*)m_Opr1, *(SharedVariable<Float>*)m_Opr2, m_Operator, pAgent, this);
			break;
		default:
			return NS_FAILED;
		}
	}

}