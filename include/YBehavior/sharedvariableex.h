#ifndef _YBEHAVIOR_SHAREDVARIABLEEX_H_
#define _YBEHAVIOR_SHAREDVARIABLEEX_H_

#include "YBehavior/shareddataex.h"
#include "YBehavior/variableoperation.h"
#include "YBehavior/interface.h"
#include "treekeymgr.h"

namespace YBehavior
{
	template<typename T>
	class SharedVariableEx : public ISharedVariableEx
	{
	public:
		typedef typename IsVector<T>::ElementType ElementType;
		SharedVariableEx()
		{
			m_VectorIndex = nullptr;
			m_Key = Utility::INVALID_KEY;
		}
		~SharedVariableEx()
		{
			if (m_VectorIndex != nullptr)
				delete m_VectorIndex;
		}
	protected:
		T m_Value;
		SharedVariableEx<INT>* m_VectorIndex;

		inline void _SetValue(const void* pValue)
		{
			m_Value = *((const T*)pValue);
		}
		inline void* _GetValue()
		{
			return &m_Value;
		}

		void _SetCastedValue(SharedDataEx* pData, const ElementType* src)
		{
			if (src == nullptr)
				return;
			INT index = *(m_VectorIndex->GetCastedValue(pData));
			if (index < 0)
				return;
			const StdVector<ElementType>* pVector = (const StdVector<ElementType>*)pData->Get<StdVector<ElementType>>(m_Key);
			if (pVector && (UINT)index < pVector->size())
				(*const_cast<StdVector<ElementType>*>(pVector))[index] = *src;
		}

		const ElementType* _GetCastedValue(SharedDataEx* pData)
		{
			INT index = *(m_VectorIndex->GetCastedValue(pData));
			if (index < 0)
			{
				ERROR_BEGIN << "Index of the vector storing the variable out of range: " << index << ERROR_END;
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
				ERROR_BEGIN << "VectorIndex out of range: " << index << ", Total " << pVector->size() << ERROR_END;
			}
			else
			{
				ERROR_BEGIN << "Invalid SharedData: " << m_Key << ERROR_END;
			}
			return nullptr;
		}
	public:
		TYPEID GetTypeID() { return GetClassTypeNumberId<T>(); }
		TYPEID GetReferenceSharedDataSelfID()
		{
			///> it's const, just return itself
			if (m_Key == Utility::INVALID_KEY)
				return GetClassTypeNumberId<T>();

			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				return GetClassTypeNumberId<StdVector<T>>();
			}

			return GetClassTypeNumberId<T>();
		}

		IVariableOperationHelper* GetOperation() { return VariableOperationHelper<T>::Get(); }
		ISharedVariableEx* GetVectorIndex() override
		{
			if (!IsVector<T>::Result)
				return m_VectorIndex;
			return nullptr;
		}

		bool IsConst() override
		{
			return m_Key == Utility::INVALID_KEY;
		}

		StdVector<ElementType>* _Convert2Vector(SharedDataEx* pData)
		{
			if (IsVector<T>::Result)
			{
				///> would have compile error if directly operate the m_Value when T is not a StdVector<XX>
				StdVector<ElementType>* mValue;
				if (pData == nullptr || m_Key == Utility::INVALID_KEY)
					mValue = (StdVector<ElementType>*)_GetValue();
				else
					mValue = (StdVector<ElementType>*)const_cast<void*>(GetValue(pData));
				
				return mValue;
			}
			else
				return nullptr;
		}
		INT VectorSize(SharedDataEx* pData) override
		{
			const StdVector<ElementType>* mValue = _Convert2Vector(pData);

			if (mValue != nullptr)
				return (INT)mValue->size();

			return 0;
		}

		void Clear(SharedDataEx* pData) override
		{
			StdVector<ElementType>* mValue = _Convert2Vector(pData);

			if (mValue != nullptr)
				mValue->clear();
		}

		STRING GetValueToSTRING(SharedDataEx* pData) override
		{
			const T* v = GetCastedValue(pData);
			if (v != nullptr)
				return Utility::ToStringWithLength(*v);
			return Utility::StringEmpty;
		}
		const void* GetElement(SharedDataEx* pData, INT index) override
		{
			const StdVector<ElementType>* mValue = _Convert2Vector(pData);

			if (mValue != nullptr)
			{
				if ((INT)mValue->size() <= index)
				{
					ERROR_BEGIN << "Index " << index << " out of range of Vector with size " << mValue->size() << ERROR_END;
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
		void SetElement(SharedDataEx* pData, const void* v, INT index) override
		{
			StdVector<ElementType>* mValue = _Convert2Vector(pData);

			if (mValue != nullptr && v != nullptr)
			{
				if ((INT)mValue->size() <= index)
				{
					ERROR_BEGIN << "Index " << index << " out of range of Vector with size " << mValue->size() << ERROR_END;
				}
				else
				{
					(*mValue)[index] = *((const ElementType*)v);
				}
			}
		}
		void PushBackElement(SharedDataEx* pData, const void* v) override
		{
			StdVector<ElementType>* mValue = _Convert2Vector(pData);

			if (mValue != nullptr && v != nullptr)
			{
				mValue->push_back(*((const ElementType*)v));
			}
		}

		const void* GetValue(SharedDataEx* pData)
		{
			return GetCastedValue(pData);
		}
		void SetValue(SharedDataEx* pData, const void* src)
		{
			SetCastedValue(pData, (const T*)src);
		}

		void SetValueFromString(const STRING& str)
		{
			if (CanFromString<ElementType>::Result)
			{
				if (IsVector<T>::Result)
				{
					///> would have compile error if directly operate the m_Value when T is not a StdVector<XX>
					StdVector<ElementType>& mValue = *((StdVector<ElementType>*)_GetValue());
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
					_SetValue((const void*)(&res));
				}
			}
		}
		void SetKeyFromString(const STRING& s)
		{
			///> if T is a single type but has vector index, it means this variable is an element of a vector.
			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				SetKey(TreeKeyMgr::Instance()->GetKeyByName<StdVector<T>>(s));
			}
			else
			{
				SetKey(TreeKeyMgr::Instance()->GetKeyByName<T>(s));
			}
		}

		const T* GetCastedValue(SharedDataEx* pData)
		{
			if (pData == nullptr || m_Key == Utility::INVALID_KEY)
				return &m_Value;
			///> It's an element of a vector
			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				return (const T*)_GetCastedValue(pData);
			}

			return (const T*)pData->Get<T>(m_Key);
		}
		void GetCastedValue(SharedDataEx* pData, T& t)
		{
			const T* v = GetCastedValue(pData);
			if (v != nullptr)
				t = *v;
		}

		void SetCastedValue(SharedDataEx* pData, const T* src)
		{
			if (pData == nullptr || m_Key == Utility::INVALID_KEY)
			{
				///> Currently, we treat the non-pointer as CONST, then this function wont change their values.
				//m_Value = *((T*)src);
				//ERROR_BEGIN << "Try to modify a CONST variable value: " << this->GetName() << ERROR_END;
				return;
			}
			///> It's an element of a vector
			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				_SetCastedValue(pData, (const ElementType*)src);
			}
			else
			{
				pData->Set<T>(m_Key, src);
			}
		}

		void SetCastedValue(SharedDataEx* pData, const T&& src)
		{
			T t(src);
			SetCastedValue(pData, &t);
		}

		void SetCastedValue(SharedDataEx* pData, const T& src)
		{
			SetCastedValue(pData, &t);
		}

		///> This function must be called BEFORE SetIndexFromString
		void SetVectorIndex(const STRING& vbType, const STRING& s)
		{
			if (vbType.length() < 1)
				return;
			if (vbType[0] == Utility::POINTER_CHAR)
			{
				m_VectorIndex = new SharedVariableEx<INT>();
				m_VectorIndex->SetKeyFromString(s);
			}
			else if (vbType[0] == Utility::CONST_CHAR)
			{
				m_VectorIndex = new SharedVariableEx<INT>();
				m_VectorIndex->SetValueFromString(s);
			}
		}

	};
}

#endif