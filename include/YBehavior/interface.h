#ifndef _YBEHAVIOR_INTERFACE_H_
#define _YBEHAVIOR_INTERFACE_H_

#include "YBehavior/types/types.h"

namespace YBehavior
{
	class IDataOperationHelper;
	class VariableCollection;
	class IMemory
	{
	public:
		virtual VariableCollection* GetMainData() = 0;
		virtual VariableCollection* GetStackTop() = 0;
		virtual ~IMemory() {}
	};
	class IPin
	{
	public:
		virtual ~IPin() {}
		inline void SetKey(KEY key) { m_Key = key; }
		inline KEY GetKey() { return m_Key; }
		inline void SetIsLocal(bool local) { m_IsLocal = local; }
		inline bool IsLocal() { return m_IsLocal; }
		virtual bool IsConst() = 0;
		inline UINT GetIndex() const { return m_Idx; }
		inline void SetIndex(UINT idx) { m_Idx = idx; }

		void SetName(const STRING& name, UINT nodeUID, const STRING& nodeName, const STRING& treeName)
		{
			m_Name = name;
			std::stringstream ss;
			ss << treeName << "." << nodeUID << "." << nodeName << "." << name;
			if (m_ReferenceName.size() > 0)
			{
				ss << " (" << m_ReferenceName;
				if (IsLocal())
				{
					ss << "'";
				}
				ss << ")";
			}
			m_LogName = ss.str();
		}
		inline const STRING& GetName() { return m_Name; }
		inline const STRING& GetLogName() { return m_LogName; }

		virtual void SetArrayIndex(const STRING& vbType, const STRING& s) { }
		virtual void SetKeyFromString(const STRING& s) = 0;
		virtual const void* GetValuePtr(IMemory* pMemory) = 0;
		virtual void SetValue(IMemory* pMemory, const void* src) = 0;
		virtual void SetValueFromString(const STRING& str) = 0;
		virtual TYPEID TypeID() const = 0;
		virtual TYPEID ElementTypeID() const = 0;
		///> if this variable is an INT, and it refers to an element of an INT[], this method will return the type of INT[] instead of INT;   Used in log..
		virtual TYPEID GetReferenceSharedDataSelfID() = 0;

		virtual IPin* GetArrayIndex() = 0;
		virtual STRING GetValueToSTRING(IMemory* pMemory) = 0;

		virtual bool IsThisArray() const = 0;
		///> Belows are functions for vector
		virtual INT ArraySize(IMemory* pMemory) = 0;
		virtual void Clear(IMemory* pMemory) = 0;
		virtual const void* GetElementPtr(IMemory* pMemory, INT index) = 0;
		virtual void SetElement(IMemory* pMemory, const void* v, INT index) = 0;
		virtual void PushBackElement(IMemory* pMemory, const void* v) = 0;
		virtual bool RemoveElement(IMemory* pMemory, const void* v, bool isAll) = 0;
		virtual bool HasElement(IMemory* pMemory, const void* v, INT& firstIndex) = 0;
		virtual INT CountElement(IMemory* pMemory, const void* v, INT& firstIndex) = 0;
	protected:
		KEY m_Key;
		bool m_IsLocal;
		STRING m_Name;
		STRING m_LogName;
		STRING m_ReferenceName;
		/// <summary>
		/// Index in the container
		/// </summary>
		UINT m_Idx;
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
		virtual bool SetDefault(KEY key) = 0;
		virtual bool TrySet(KEY key, const void* src) = 0;
		virtual void CloneFrom(const IDataArray*) = 0;
		virtual IDataArray* Clone() = 0;
		virtual void MergeFrom(const IDataArray* other, bool useNewValue) = 0;
		virtual SIZE_KEY Length() const = 0;
		virtual TYPEID TypeID() const = 0;
		virtual Iterator Iter() const = 0;
		virtual void Clear() = 0;
	};

}

#endif