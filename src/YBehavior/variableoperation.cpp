#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	template<>
	void ValueHandler::Calculate<EntityWrapper>( void* pLeft, const void* pRight0, const void* pRight1, OperationType op)
	{

	}
	template<>
	void ValueHandler::Calculate<Bool>(void* pLeft, const void* pRight0, const void* pRight1, OperationType op)
	{

	}

#ifdef YB_ABOUT_EQUAL_FLOAT
	template<>
	bool ValueHandler::Compare<Float>(const void* pLeft, const void* pRight, OperationType op)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in Compare" << ERROR_END;
			return false;
		}
		if (pRight == nullptr)
		{
			ERROR_BEGIN << "pRight is null in Compare" << ERROR_END;
			return false;
		}

		const Float& left = *((const Float*)pLeft);
		const Float& right = *((const Float*)pRight);


		switch (op)
		{
		case YBehavior::OT_EQUAL:
			if (right == 0.0f)
				return left == right;
			return std::abs(left - right) < 0.1f;
		case YBehavior::OT_NOT_EQUAL:
			if (right == 0.0f)
				return left != right;
			return std::abs(left - right) >= 0.1f;
		case YBehavior::OT_GREATER:
			return left > right;
		case YBehavior::OT_LESS:
			return left < right;
		case YBehavior::OT_LESS_EQUAL:
			return left <= right;
		case YBehavior::OT_GREATER_EQUAL:
			return left >= right;
		default:
			return false;
		}
	}
#endif

	template<>
	void ValueHandler::Random<Int>(void* pLeft, const void* pRight0, const void* pRight1)
	{
		_DoRandom<Int>(pLeft, pRight0, pRight1);
	}

	template<>
	void ValueHandler::Random<Float>(void* pLeft, const void* pRight0, const void* pRight1)
	{
		_DoRandom<Float>(pLeft, pRight0, pRight1);
	}

	template<>
	void ValueHandler::Random<Bool>(void* pLeft, const void* pRight0, const void* pRight1)
	{
		_DoRandom<Bool>(pLeft, pRight0, pRight1);
	}


	template<>
	void ValueHandler::_Sub<String>(const String& left, const String& right, String& output)
	{
	}

	template<>
	void ValueHandler::_Mul<String>(const String& left, const String& right, String& output)
	{
	}

	template<>
	void ValueHandler::_Div<String>(const String& left, const String& right, String& output)
	{
	}

	TempObject::~TempObject()
	{
		if (pData && pHelper)
			pHelper->RecycleData(pData);
	}

}
