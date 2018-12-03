#ifndef _YBEHAVIOR_INTERFACE_H_
#define _YBEHAVIOR_INTERFACE_H_

#include "YBehavior/types.h"

namespace YBehavior
{
	class IVariableOperationHelper;
	class SharedDataEx;
	class IMemory
	{
	public:
		virtual SharedDataEx* GetMainData() = 0;
		virtual SharedDataEx* GetStackTop() = 0;
	};
	class ISharedVariableEx
	{
	public:
		virtual ~ISharedVariableEx() {}
		inline void SetKey(KEY key) { m_Key = key; }
		inline KEY GetKey() { return m_Key; }
		inline void SetIsLocal(bool local) { m_IsLocal = local; }
		inline bool IsLocal() { return m_IsLocal; }
		virtual bool IsConst() = 0;
#ifdef DEBUGGER
		inline void SetName(const STRING& name) { m_Name = name; }
		inline const STRING& GetName() { return m_Name; }
#endif
		virtual void SetVectorIndex(const STRING& vbType, const STRING& s) { }
		virtual void SetKeyFromString(const STRING& s) = 0;
		virtual const void* GetValue(IMemory* pMemory) = 0;
		virtual void SetValue(IMemory* pMemory, const void* src) = 0;
		virtual void SetValueFromString(const STRING& str) = 0;
		virtual TYPEID TypeID() const = 0;
		///> if this variable is an INT, and it refers to an element of an INT[], this method will return the type of INT[] instead of INT;   Used in log..
		virtual TYPEID GetReferenceSharedDataSelfID() = 0;

		virtual IVariableOperationHelper* GetOperation() const = 0;
		virtual IVariableOperationHelper* GetElementOperation() const = 0;
		virtual ISharedVariableEx* GetVectorIndex() = 0;
		virtual STRING GetValueToSTRING(IMemory* pMemory) = 0;

		virtual bool IsThisVector() const = 0;
		///> Belows are functions for vector
		virtual INT VectorSize(IMemory* pMemory) = 0;
		virtual void Clear(IMemory* pMemory) = 0;
		virtual const void* GetElement(IMemory* pMemory, INT index) = 0;
		virtual void SetElement(IMemory* pMemory, const void* v, INT index) = 0;
		virtual void PushBackElement(IMemory* pMemory, const void* v) = 0;
	protected:
		KEY m_Key;
		bool m_IsLocal;
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
		virtual ~IDataArrayIterator(){}
		virtual void Recycle() {}
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
				{
					innerIter->Recycle();
					innerIter = nullptr;
				}
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
		virtual void CloneFrom(const IDataArray*) = 0;
		virtual void MergeFrom(const IDataArray* other, bool bOverride) = 0;
		virtual SIZE_KEY Length() const = 0;
		virtual TYPEID TypeID() const = 0;
		virtual Iterator Iter() const = 0;
	};

}

#endif