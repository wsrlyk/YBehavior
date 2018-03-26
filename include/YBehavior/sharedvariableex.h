#ifndef _YBEHAVIOR_SHAREDVARIABLEEX_H_
#define _YBEHAVIOR_SHAREDVARIABLEEX_H_

#include "YBehavior/shareddataex.h"
#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	class IVariableOperationHelper;
	class ISharedVariableEx
	{
	public:
		virtual ~ISharedVariableEx() {}
		inline void SetIndex(INT index) { m_Index = index; }
		virtual void SetIndexFromString(const STRING& s) = 0;
		virtual const void* GetValue(SharedDataEx* pData) = 0;
		virtual void SetValue(SharedDataEx* pData, const void* src) = 0;
		virtual void SetValueFromString(const STRING& str) = 0;
		virtual INT GetTypeID() = 0;
		virtual IVariableOperationHelper* GetOperation() = 0;
	protected:
		INT m_Index;

	};
	template<typename T>
	class SharedVariableEx : public ISharedVariableEx
	{
	public:
		typedef typename IsVector<T>::ElementType ElementType;
	protected:
		T m_Value;

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
		IVariableOperationHelper* GetOperation() { return VariableOperationHelper<T>::Get(); }

		const void* GetValue(SharedDataEx* pData)
		{
			if (pData == nullptr || m_Index == SharedDataEx::INVALID_INDEX)
				return (const void*)&m_Value;
			return pData->Get<T>(m_Index);
		}
		void SetValue(SharedDataEx* pData, const void* src)
		{
			if (pData == nullptr || m_Index == SharedDataEx::INVALID_INDEX)
				m_Value = *((T*)src);
			else
				pData->Set<T>(m_Index, (const T*)src);
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
			SetIndex(NodeFactory::Instance()->CreateIndexByName<T>(s));
		}

		const T* GetCastedValue(SharedDataEx* pData)
		{
			if (data == nullptr || m_Index == SharedDataEx::INVALID_INDEX)
				return &m_Value;
			return (const T*)pData->Get<T>(m_Index);
		}

		void SetCastedValue(SharedDataEx* pData, const T* src)
		{
			if (data == nullptr || m_Index == SharedDataEx::INVALID_INDEX)
				m_Value = *((T*)src);
			else
				pData->Set<T>(m_Index, src);
		}
	};


}

#endif