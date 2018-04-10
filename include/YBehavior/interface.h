#ifndef _YBEHAVIOR_INTERFACE_H_
#define _YBEHAVIOR_INTERFACE_H_

#include "YBehavior/types.h"

namespace YBehavior
{
	class IVariableOperationHelper;
	class SharedDataEx;
	class ISharedVariableEx
	{
	public:
		virtual ~ISharedVariableEx() {}
		inline void SetIndex(INT index) { m_Index = index; }
		virtual void SetVectorIndex(const STRING& vbType, const STRING& s) { }
		virtual void SetIndexFromString(const STRING& s) = 0;
		virtual const void* GetValue(SharedDataEx* pData) = 0;
		virtual void SetValue(SharedDataEx* pData, const void* src) = 0;
		virtual void SetValueFromString(const STRING& str) = 0;
		virtual INT GetTypeID() = 0;
		virtual IVariableOperationHelper* GetOperation() = 0;
	protected:
		INT m_Index;
	};

	class IDataArray
	{
	public:
		virtual ~IDataArray() {}
		virtual const void* Get(INT index) const = 0;
		virtual const STRING GetToString(INT index) const = 0;
		virtual bool Set(INT index, const void* src) = 0;
		virtual IDataArray* Clone() const = 0;
		virtual INT Length() const = 0;

	};

}

#endif