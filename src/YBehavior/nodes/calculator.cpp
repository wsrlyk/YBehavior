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

	template<typename T>
	void SetIndex(SharedVariable<T>& variable, const STRING& name)
	{
		variable.SetIndex(NodeFactory::Instance()->CreateIndexByName<T>(name));
	}

	typedef INT (*CreateIndexFunc)(const STRING& name);

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

	TypeAB ProcessOp(ISharedVariable*& op, pugi::xml_attribute& attrOptr, const pugi::xml_node& data)
	{
		std::vector<STRING> buffer;
		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Calculator Opl: " << data.name() << ERROR_END;
			return Types::NoneAB;
		}
		auto tempChar = attrOptr.value();
		Utility::SplitString(tempChar, buffer, Utility::SpaceSpliter);
		if (buffer.size() != 2 || buffer[0].length() != 3)
		{
			ERROR_BEGIN << "Format Error, Opl in" << data.name() << ": " << tempChar << ERROR_END;
			return Types::NoneAB;
		}

		switch (buffer[0][1])
		{
		case Types::FloatAB:
			{
				auto variable = new SharedFloat();
				op = variable;
				if (buffer[0][2] == 'S')
					SetIndex<Float>(*((SharedVariable<Float>*)op), buffer[1]);
				else
					variable->SetValueFromString(buffer[1]);
				return Types::FloatAB;
			}
			break;
		case Types::IntAB:
			{
				auto variable = new SharedInt();
				op = variable;
				if (buffer[0][2] == 'S')
					SetIndex<Int>(*((SharedVariable<Int>*)op), buffer[1]);
				else
					variable->SetValueFromString(buffer[1]);
				return Types::IntAB;
			}
			break;
		default:
			{
				ERROR_BEGIN << "This type cant be supported by Calculator " << data.name() << ": " << tempChar << ERROR_END;
				return Types::NoneAB;
			}
			break;
		}
	}

	void Calculator::OnLoaded(const pugi::xml_node& data)
	{
		///> ‘ÀÀ„∑˚
		auto attrOptr = data.attribute("operator");
		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Calculator Operator: " << data.name() << ERROR_END;
			return;
		}
		auto tempChar = attrOptr.value();
		if (strcmp(tempChar, "ADD"))
		{
			m_Operator = CO_ADD;
		}
		else if (strcmp(tempChar, "SUB"))
		{
			m_Operator = CO_SUB;
		}
		else if (strcmp(tempChar, "MUL"))
		{
			m_Operator = CO_MUL;
		}
		else if (strcmp(tempChar, "DIV"))
		{
			m_Operator = CO_DIV;
		}
		//////////////////////////////////////////////////////////////////////////
		///> µ»∫≈◊Û±ﬂ
		attrOptr = data.attribute("Opl");
		m_DataType = ProcessOp(m_Opl, attrOptr, data);
		///> µ»∫≈”“±ﬂ1
		attrOptr = data.attribute("Opr1");
		m_DataType = ProcessOp(m_Opr1, attrOptr, data);
		///> µ»∫≈”“±ﬂ2
		attrOptr = data.attribute("Opr2");
		m_DataType = ProcessOp(m_Opr2, attrOptr, data);
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