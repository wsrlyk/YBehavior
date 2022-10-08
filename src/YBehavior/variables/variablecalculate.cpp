#include "YBehavior/variables/variablecalculate.h"

namespace YBehavior
{
	template<>
	void ValueCalculate::_DoCalculate<STRING>(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/)
	{
		if (op != CalculateType::ADD)
		{
			ERROR_BEGIN << "STRING only support ADD." << ERROR_END;
			return;
		}

		const STRING& right0 = *((const STRING*)pRight0);
		const STRING& right1 = *((const STRING*)pRight1);

		STRING& left = *((STRING*)pLeft);
		left = right0 + right1;
	}

	template<>
	void ValueCalculate::_DoCalculate<Vector3, Vector3, float>(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/)
	{
		if (op != CalculateType::MUL && op != CalculateType::DIV)
		{
			ERROR_BEGIN << "Vector3&float only support MUL/DIV." << ERROR_END;
			return;
		}

		const Vector3& right0 = *((const Vector3*)pRight0);
		const float& right1 = *((const float*)pRight1);

		Vector3& left = *((Vector3*)pLeft);
		left = op == CalculateType::MUL ? right0 * right1 : right0 / right1;
	}

	VariableCalculateMgr::VariableCalculateMgr()
	{
#define REGISTER_CALCULATE_1(T)\
	{\
		auto key = std::make_tuple(GetTypeID<T>(), GetTypeID<T>(), GetTypeID<T>());\
		m_Calculates[key] = new VariableCalculateHelper<T>();\
	}
#define REGISTER_CALCULATE_3(TL, TR0, TR1)\
	{\
		auto key = std::make_tuple(GetTypeID<TL>(), GetTypeID<TR0>(), GetTypeID<TR1>());\
		m_Calculates[key] = new VariableCalculateHelper<TL, TR0, TR1>();\
	}

		REGISTER_CALCULATE_1(INT);
		REGISTER_CALCULATE_1(FLOAT);
		REGISTER_CALCULATE_1(STRING);
		REGISTER_CALCULATE_1(Vector3);

		REGISTER_CALCULATE_3(Vector3, Vector3, FLOAT);

	}
	VariableCalculateMgr::~VariableCalculateMgr()
	{
		for (auto it = m_Calculates.begin(); it != m_Calculates.end(); ++it)
		{
			delete it->second;
		}
		m_Calculates.clear();
	}
	const IVariableCalculateHelper* VariableCalculateMgr::Get(TYPEID tl, TYPEID tr0, TYPEID tr1) const
	{
		auto it = m_Calculates.find(std::make_tuple(tl, tr0, tr1));
		if (it != m_Calculates.end())
			return it->second;
		return nullptr;
	}
}
