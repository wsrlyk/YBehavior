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
		inline void SetKey(KEY key) { m_Key = key; }
		inline KEY GetKey() { return m_Key; }
		virtual bool IsConst() = 0;
#ifdef DEBUGGER
		inline void SetName(const STRING& name) { m_Name = name; }
		inline const STRING& GetName() { return m_Name; }
#endif
		virtual void SetVectorIndex(const STRING& vbType, const STRING& s) { }
		virtual void SetKeyFromString(const STRING& s) = 0;
		virtual const void* GetValue(SharedDataEx* pData) = 0;
		virtual const void* GetElement(SharedDataEx* pData, INT index) = 0;
		virtual void SetValue(SharedDataEx* pData, const void* src) = 0;
		virtual void SetValueFromString(const STRING& str) = 0;
		virtual TYPEID GetTypeID() = 0;
		///> if this variable is an INT, and it refers to an element of an INT[], this method will return the type of INT[] instead of INT;   Used in log..
		virtual TYPEID GetReferenceSharedDataSelfID() = 0;

		virtual IVariableOperationHelper* GetOperation() = 0;
		virtual ISharedVariableEx* GetVectorIndex() = 0;
		virtual STRING GetValueToSTRING(SharedDataEx* pData) = 0;
		virtual INT VectorSize(SharedDataEx* pData) = 0;
	protected:
		KEY m_Key;
#ifdef DEBUGGER
		STRING m_Name;
#endif
	};

	class IDataArrayIterator
	{
	public:
		virtual bool IsEnd() { return true; }
		virtual IDataArrayIterator& operator ++() { return *this; }
		virtual const KEY Value() { return 0; }
	};

	class IDataArray
	{
	public:
		class Iterator : public IDataArrayIterator
		{
			IDataArrayIterator* innerIter = nullptr;
		public:
			Iterator(IDataArrayIterator* iter)
				: innerIter(iter)
			{

			}
			Iterator(Iterator&& other)
			{
				this->innerIter = other.innerIter;
				other.innerIter = nullptr;
			}
			~Iterator()
			{
				if (innerIter != nullptr)
					delete innerIter;
			}
			bool IsEnd() override { return innerIter->IsEnd(); }
			IDataArrayIterator& operator ++() override { ++(*innerIter); return *this; }
			const KEY Value() override { return innerIter->Value(); }
		private:
			Iterator(const Iterator& other)
			{

			}

			Iterator& operator=(const Iterator& other)
			{
				return *this;
			}
		};

		virtual ~IDataArray() {}
		virtual const void* Get(KEY key) const = 0;
		virtual const STRING GetToString(KEY key) const = 0;
		virtual bool Set(KEY key, const void* src) = 0;
		virtual IDataArray* Clone() const = 0;
		virtual void Merge(IDataArray* other, bool bOverride) = 0;
		virtual SIZE_KEY Length() const = 0;
		virtual TYPEID GetTypeID() const = 0;
		virtual Iterator Iter() const = 0;
	};

}

#endif