#ifndef _YBEHAVIOR_SHAREDVARIABLEEX_H_
#define _YBEHAVIOR_SHAREDVARIABLEEX_H_

#include "YBehavior/shareddataex.h"
#include "YBehavior/variableoperation.h"
#include "YBehavior/interface.h"

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
			m_Index = SharedDataEx::INVALID_INDEX;
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
	public:
		INT GetTypeID() { return GetClassTypeNumberId<T>(); }
		INT GetReferenceSharedDataSelfID()
		{
			///> it's const, just return itself
			if (m_Index == SharedDataEx::INVALID_INDEX)
				return GetClassTypeNumberId<T>();

			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				return GetClassTypeNumberId<std::vector<T>>();
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
			return m_Index == SharedDataEx::INVALID_INDEX;
		}

		STRING GetValueToSTRING(SharedDataEx* pData) override
		{
			const T* v = GetCastedValue(pData);
			return Utility::ToString(*v);
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
					///> would have compile error if directly operate the m_Value when T is not a std::vector<XX>
					std::vector<ElementType>& mValue = *((std::vector<ElementType>*)_GetValue());
					mValue.clear();
					std::vector<STRING> res;
					Utility::SplitString(str, res, '|');
					for (auto it = res.begin(); it != res.end(); ++it)
					{
						mValue.push_back(std::move(Utility::ToType<ElementType>(*it)));
					}
				}
				else
				{
					ElementType res = Utility::ToType<ElementType>(str);
					_SetValue((const void*)(&res));
				}
			}
		}
		void SetIndexFromString(const STRING& s)
		{
			///> if T is a single type but has vector index, it means this variable is an element of a vector.
			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				SetIndex(NodeFactory::Instance()->CreateIndexByName<std::vector<T>>(s));
			}
			else
			{
				SetIndex(NodeFactory::Instance()->CreateIndexByName<T>(s));
			}
		}

		const T* GetCastedValue(SharedDataEx* pData)
		{
			if (pData == nullptr || m_Index == SharedDataEx::INVALID_INDEX)
				return &m_Value;
			///> It's an element of a vector
			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				INT index = *(m_VectorIndex->GetCastedValue(pData));
				if (index < 0)
				{
					ERROR_BEGIN << "Index of the vector storing the variable out of range: " << index << ERROR_END;
					return nullptr;
				}
				const std::vector<T>* pVector = (const std::vector<T>*)pData->Get<std::vector<T>>(m_Index);
				if (pVector && index < pVector->size())
				{
					const T& t = (*pVector)[index];
					return &t;
				}
				return nullptr;
			}

			return (const T*)pData->Get<T>(m_Index);
		}

		void SetCastedValue(SharedDataEx* pData, const T* src)
		{
			if (pData == nullptr || m_Index == SharedDataEx::INVALID_INDEX)
			{
				m_Value = *((T*)src);
				return;
			}
			///> It's an element of a vector
			if (!IsVector<T>::Result && m_VectorIndex != nullptr)
			{
				INT index = *(m_VectorIndex->GetCastedValue(pData));
				if (index < 0)
					return;
				const std::vector<T>* pVector = (const std::vector<T>*)pData->Get<std::vector<T>>(m_Index);
				if (pVector && index < pVector->size())
					(*const_cast<std::vector<T>*>(pVector))[index] = *src;
			}
			else
			{
				pData->Set<T>(m_Index, src);
			}
		}

		///> This function must be called BEFORE SetIndexFromString
		void SetVectorIndex(const STRING& vbType, const STRING& s)
		{
			if (vbType == "S")
			{
				m_VectorIndex = new SharedVariableEx<INT>();
				m_VectorIndex->SetIndexFromString(s);
			}
			else if (vbType == "C")
			{
				m_VectorIndex = new SharedVariableEx<INT>();
				m_VectorIndex->SetValueFromString(s);
			}
		}

	};


}

#endif