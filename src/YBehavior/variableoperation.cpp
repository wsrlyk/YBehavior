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
