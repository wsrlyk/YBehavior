#pragma once

#include "YBehavior/variable.h"
#include "YBehavior/interface.h"
#include "YBehavior/treekeymgr.h"
#include "YBehavior/memory.h"
#include <algorithm>
#include "YBehavior/logger.h"

namespace YBehavior
{
	template<typename T>
	class Pin : public IPin
	{
	public:
		typedef typename IsVector<T>::ElementType ElementType;
		Pin()
		{
			Utility::SetDefault(m_Value);
			m_ArrayIndex = nullptr;
			m_Key = Utility::INVALID_KEY;
			m_IsLocal = false;
		}
		~Pin()
		{
			if (m_ArrayIndex != nullptr)
				delete m_ArrayIndex;
		}
	protected:
		T m_Value;
		Pin<INT>* m_ArrayIndex;

		inline void _SetValue(const void* pValue)
		{
			m_Value = *((const T*)pValue);
		}

		inline void _SetValue(const T& value)
		{
			m_Value = value;
		}

		inline void* _GetValuePtr()
		{
			return &m_Value;
		}

		inline T& _GetValue()
		{
			return m_Value;
		}

		void _SetValue(IMemory* pMemory, const ElementType* src)
		{
			if (src == nullptr)
				return;
			INT index = -1;
			m_ArrayIndex->GetValue(pMemory, index);
			if (index < 0)
			{
				ERROR_BEGIN << "Invalid Index of Array: " << index << " at " << this->GetLogName() << ERROR_END;
				return;
			}
			VariableCollection* pData;
			if (!IsLocal())
				pData = pMemory->GetMainData();
			else
				pData = pMemory->GetStackTop();
			if (!pData)
			{
				ERROR_BEGIN << "SharedData NULL at " << this->GetLogName() << ERROR_END;
			}

			const StdVector<ElementType>* pVector = (const StdVector<ElementType>*)pData->Get<StdVector<ElementType>>(m_Key);
			if (pVector)
			{
				if ((UINT)index < pVector->size())
					(*const_cast<StdVector<ElementType>*>(pVector))[index] = *src;
				else
					ERROR_BEGIN << "Index " << index << " out of range, Array Length " << pVector->size() << " at " << this->GetLogName() << ERROR_END;
			}
		}
		const ElementType* _GetElementPtr(IMemory* pMemory)
		{
			INT index = -1;
			m_ArrayIndex->GetValue(pMemory, index);
			if (index < 0)
			{
				ERROR_BEGIN << "Invalid Index of Array: " << index << " at " << this->GetLogName() << ERROR_END;
				return nullptr;
			}
			VariableCollection* pData;
			if (!IsLocal())
				pData = pMemory->GetMainData();
			else
				pData = pMemory->GetStackTop();
			if (!pData)
			{
				ERROR_BEGIN << "SharedData NULL at " << this->GetLogName() << ERROR_END;
				return nullptr;
			}
			const StdVector<ElementType>* pVector = (const StdVector<ElementType>*)pData->Get<StdVector<ElementType>>(m_Key);
			if (pVector && (UINT)index < pVector->size())
			{
				const ElementType* t = &(*pVector)[index];
				return t;
			}
			if (pVector)
			{
				ERROR_BEGIN << "Index " << index << " out of range, Array Length " << pVector->size() << " at " << this->GetLogName() << ERROR_END;
			}
			else
			{
				ERROR_BEGIN << "Invalid SharedData: " << m_Key << " at " << this->GetLogName() << ERROR_END;
			}
			return nullptr;
		}

		T* _GetValuePtr(IMemory* pMemory)
		{
			if (pMemory == nullptr || m_Key == Utility::INVALID_KEY)
				return &m_Value;
			///> It's an element of a vector
			if (!IsVector<T>::Result && m_ArrayIndex != nullptr)
			{
				return (T*)_GetElementPtr(pMemory);
			}

			VariableCollection* pData;
			if (!IsLocal())
				pData = pMemory->GetMainData();
			else
				pData = pMemory->GetStackTop();

			if (!pData)
			{
				ERROR_BEGIN << "SharedData NULL at " << this->GetLogName() << ERROR_END;
				return nullptr;
			}
			return (T*)pData->Get<T>(m_Key);
		}

		StdVector<ElementType>* _Convert2VectorPtr(IMemory* pMemory)
		{
			if (IsVector<T>::Result)
			{
				///> would have compile error if directly operate the m_Value when T is not a StdVector<XX>
				StdVector<ElementType>* mValue;
				if (pMemory == nullptr || m_Key == Utility::INVALID_KEY)
					mValue = (StdVector<ElementType>*)_GetValuePtr();
				else
					mValue = (StdVector<ElementType>*)const_cast<void*>(GetValuePtr(pMemory));

				return mValue;
			}
			else
				return nullptr;
		}

	public:
		TYPEID TypeID() const override{ return GetTypeID<T>(); }
		TYPEID ElementTypeID() const override { return GetTypeID<ElementType>(); }
		TYPEID GetReferenceSharedDataSelfID() override
		{
			///> it's const, just return itself
			if (m_Key == Utility::INVALID_KEY)
				return GetTypeID<T>();

			if (!IsVector<T>::Result && m_ArrayIndex != nullptr)
			{
				return GetTypeID<StdVector<T>>();
			}

			return GetTypeID<T>();
		}
		bool IsThisArray() const override
		{
			return IsVector<T>::Result;
		}

		IPin* GetArrayIndex() override
		{
			if (!IsVector<T>::Result)
				return m_ArrayIndex;
			return nullptr;
		}

		bool IsConst() const override
		{
			return m_Key == Utility::INVALID_KEY;
		}

		StdVector<ElementType>* GetArrayPtr(IMemory* pMemory)
		{
			return _Convert2VectorPtr(pMemory);
		}

		INT ArraySize(IMemory* pMemory) override
		{
			const StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);

			if (mValue != nullptr)
				return (INT)mValue->size();

			return 0;
		}

		void Clear(IMemory* pMemory) override
		{
			StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);

			if (mValue != nullptr)
				mValue->clear();
		}

		STRING GetValueToSTRING(IMemory* pMemory) override
		{
			const T* v = GetValue(pMemory);
			if (v != nullptr)
				return Utility::ToStringWithLength(*v);
			return Utility::StringEmpty;
		}
		const void* GetElementPtr(IMemory* pMemory, INT index) override
		{
			const StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);

			if (mValue != nullptr)
			{
				if ((INT)mValue->size() <= index)
				{
					ERROR_BEGIN << "Index " << index << " out of range, Array Length " << mValue->size() << " at " << this->GetLogName() << ERROR_END;
					return nullptr;
				}
				else
				{
					return &(*mValue)[index];
				}
			}
			else
			{
				return nullptr;
			}
		}
		void SetElement(IMemory* pMemory, const void* v, INT index) override
		{
			StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);

			if (mValue != nullptr && v != nullptr)
			{
				if ((INT)mValue->size() <= index)
				{
					ERROR_BEGIN << "Index " << index << " out of range, Array Length " << mValue->size() << " at " << this->GetLogName() << ERROR_END;
				}
				else
				{
					(*mValue)[index] = *((const ElementType*)v);
				}
			}
		}
		void PushBackElement(IMemory* pMemory, const void* v) override
		{
			StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);

			if (mValue != nullptr && v != nullptr)
			{
				mValue->push_back(*((const ElementType*)v));
			}
		}

		bool RemoveElement(IMemory* pMemory, const void* v, bool isAll) override
		{
			StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);

			if (mValue != nullptr && v != nullptr && !mValue->empty())
			{
				if (isAll)
				{
					auto end = std::remove(mValue->begin(), mValue->end(), *((const ElementType*)v));
					if (end != mValue->end())
					{
						mValue->erase(end, mValue->end());
						return true;
					}
					return false;
				}
				else
				{
					auto it = std::find(mValue->begin(), mValue->end(), *((const ElementType*)v));
					if (it != mValue->end())
					{
						mValue->erase(it);
						return true;
					}
					return false;
				}
			}
			return false;
		}

		bool HasElement(IMemory* pMemory, const void* v, INT& firstIndex) override
		{
			StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);
			firstIndex = -1;
			if (mValue != nullptr && v != nullptr && !mValue->empty())
			{
				auto it = std::find(mValue->begin(), mValue->end(), *((const ElementType*)v));
				auto idx = it - mValue->begin();
				firstIndex = (INT)idx;
				return it != mValue->end();
			}
			return false;
		}
		INT CountElement(IMemory* pMemory, const void* v, INT& firstIndex) override
		{
			StdVector<ElementType>* mValue = _Convert2VectorPtr(pMemory);
			firstIndex = -1;
			if (mValue != nullptr && v != nullptr && !mValue->empty())
			{
				INT count = 0;
				auto it = mValue->begin();
				const auto& vv = *((const ElementType*)v);
				do
				{
					it = std::find(it, mValue->end(), vv);
					if (it == mValue->end())
						break;
					++count;
					if (firstIndex == -1)
					{
						auto idx = it - mValue->begin();
						firstIndex = (INT)idx;
					}

					++it;
				} while (it != mValue->end());
				return count;
			}
			return 0;
		}

		const void* GetValuePtr(IMemory* pMemory) override
		{
			return GetValue(pMemory);
		}
		void SetValue(IMemory* pMemory, const void* src) override
		{
			SetValue(pMemory, (const T*)src);
		}

		void SetValueFromString(const STRING& str) override
		{
			if (CanFromString<ElementType>::Result)
			{
				if (IsVector<T>::Result)
				{
					///> would have compile error if directly operate the m_Value when T is not a StdVector<XX>
					StdVector<ElementType>& mValue = *((StdVector<ElementType>*)_GetValuePtr());
					mValue.clear();
					StdVector<STRING> res;
					Utility::SplitString(str, res, '|');
					for (auto it = res.begin(); it != res.end(); ++it)
					{
						mValue.push_back(Utility::ToType<ElementType>(*it));
					}
				}
				else
				{
					ElementType res = Utility::ToType<ElementType>(str);
					_SetValue((const void*)&res);
				}
			}
		}
		void SetKeyFromString(const STRING& s) override
		{
			KEY key = Utility::INVALID_KEY;
			/////> if T is a single type but has vector index, it means this variable is an element of a vector.
			//if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			//{
			//	key = (TreeKeyMgr::Instance()->GetKeyByName<StdVector<T>>(s));
			//}
			//else
			//{
			//	key = (TreeKeyMgr::Instance()->GetKeyByName<T>(s));
			//}
			key = (TreeKeyMgr::Instance()->GetKeyByName(s));

			if (key == Utility::INVALID_KEY)
			{
				ERROR_BEGIN << "Cant Get Key for \"" << s << "\" with typeid = " << GetTypeID<T>() << " at " << this->GetLogName() << ERROR_END;
			}
			m_ReferenceName = s;
			SetKey(key);
		}

		const T* GetValue(IMemory* pMemory)
		{
			return (const T*)_GetValuePtr(pMemory);
		}

		void GetValue(IMemory* pMemory, T& t)
		{
			const T* v = GetValue(pMemory);
			if (v != nullptr)
				t = *v;
		}

		void SetValue(IMemory* pMemory, const T* src)
		{
			if (pMemory == nullptr || m_Key == Utility::INVALID_KEY)
			{
				///> Currently, we treat the non-pointer as CONST, then this function wont change their values.
				//m_Value = *((T*)src);
				//ERROR_BEGIN << "Try to modify a CONST variable value: " << this->GetName() << ERROR_END;
				return;
			}
			///> It's an element of a vector
			if (!IsVector<T>::Result && m_ArrayIndex != nullptr)
			{
				_SetValue(pMemory, (const ElementType*)src);
			}
			else
			{
				VariableCollection* pData;
				if (IsLocal())
				{
					pData = pMemory->GetStackTop();
				}
				else
				{
					pData = pMemory->GetMainData();
				}
				if (pData)
					pData->TrySet<T>(m_Key, src);
				else
					ERROR_BEGIN << "SharedData NULL at " << this->GetLogName() << ERROR_END;
			}
		}

		void SetValue(IMemory* pData, const T&& src)
		{
			T t(src);
			SetValue(pData, &t);
		}

		void SetValue(IMemory* pData, const T& src)
		{
			SetValue(pData, &src);
		}

		///> This function must be called BEFORE SetIndexFromString
		void SetArrayIndex(const STRING& vbType, const STRING& s) override
		{
			if (vbType.length() < 1)
				return;
			const char t = Utility::ToUpper(vbType[0]);
			if (t == Utility::POINTER_CHAR)
			{
				m_ArrayIndex = new Pin<INT>();
				m_ArrayIndex->SetKeyFromString(s);
				m_ArrayIndex->SetIsLocal(Utility::IsLower(vbType[0]));
			}
			else if (t == Utility::CONST_CHAR)
			{
				m_ArrayIndex = new Pin<INT>();
				m_ArrayIndex->SetValueFromString(s);
			}
		}

	};
}
