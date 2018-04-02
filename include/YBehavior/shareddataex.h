#ifndef _YBEHAVIOR_SHAREDDATAEX_H_
#define _YBEHAVIOR_SHAREDDATAEX_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"
#include "YBehavior/tools/meta.h"
#include "YBehavior/interface.h"

#define FOR_EACH_SINGLE_NORMAL_TYPE(func)	\
	func(Int);	\
	func(Uint64);	\
	func(Bool);	\
	func(Float);	\
	func(String);	\
	func(Vector3);	\
	func(AgentWrapper);	
#define FOR_EACH_VECTOR_NORMAL_TYPE(func)	\
	func(VecInt);\
	func(VecUint64);\
	func(VecBool);\
	func(VecFloat);\
	func(VecString);\
	func(VecVector3);\
	func(VecAgentWrapper);
#define FOR_EACH_TYPE(func)    \
    func(Int);    \
    func(Uint64);    \
    func(Bool);    \
    func(Float);    \
    func(String);    \
    func(AgentWrapper);    \
    func(Vector3);\
    func(VecInt);\
    func(VecUint64);\
    func(VecBool);\
    func(VecFloat);\
    func(VecString);\
    func(VecAgentWrapper);\
    func(VecVector3);

namespace YBehavior
{
	template<typename T>
	class DataArray : public IDataArray
	{
		std::vector<T> m_Datas;
	public:
		virtual IDataArray* Clone() const override
		{
			DataArray<T>* newArray = new DataArray<T>();
			newArray->m_Datas = m_Datas;
			return newArray;
		}

		bool Get(INT index, T& res)
		{
			if (index < 0 || index >= (INT)m_Datas.size())
				return false;
			res = m_Datas[index];
			return true;
		}

		virtual void* Get(INT index) override
		{
			if (index < 0 || index >= (INT)m_Datas.size())
				return nullptr;
			return &(m_Datas[index]);
		}

		bool Set(INT index, const T& src)
		{
			if (index < 0)
				return false;
			if ((INT)m_Datas.size() <= index)
			{
				m_Datas.resize(index);
				m_Datas.push_back(src);
			}
			else
			{
				m_Datas[index] = src;
			}
			return true;
		}

		bool Set(INT index, T&& src)
		{
			if (index < 0)
				return false;
			if ((INT)m_Datas.size() <= index)
			{
				m_Datas.resize(index);
				m_Datas.push_back(std::move(src));
			}
			else
			{
				m_Datas[index] = src;
			}
			return true;
		}

		virtual bool Set(INT index, const void* src) override
		{
			if (index < 0 || src == nullptr)
				return false;
			if ((INT)m_Datas.size() <= index)
			{
				m_Datas.resize(index);
				m_Datas.push_back(*((T*)src));
			}
			else
			{
				m_Datas[index] = *((T*)src);
			}
			return true;
		}
	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	class SharedDataEx
	{
	public:
		const static int INVALID_INDEX = -1;

		template<typename T>
		INT GetTypeIndex();
		

	protected:
		IDataArray* m_Datas[14];

	public:
		SharedDataEx();

		~SharedDataEx()
		{
			for (int i = 0; i < 14; ++i)
			{
				if (m_Datas[i] != nullptr)
					delete m_Datas[i];
			}
		}

		void Clone(const SharedDataEx& other)
		{
			for (int i = 0; i < 14; ++i)
			{
				if (other.m_Datas[i] == nullptr)
				{
					m_Datas[i] = nullptr;
					continue;
				}

				m_Datas[i] = other.m_Datas[i]->Clone();
			}
		}

		template<typename T>
		bool Get(INT index, T& res);

		template<typename T>
		const T* Get(INT index);

		void* Get(INT index, INT typeIndex)
		{
			IDataArray* iarray = m_Datas[typeIndex];
			return iarray->Get(index);
		}


		template<typename T>
		bool Set(INT index, const T& src);


		template<typename T>
		bool Set(INT index, const T* src);

		template<typename T>
		bool Set(INT index, T&& src);

		bool Set(INT index, INT typeIndex, void* src)
		{
			IDataArray* iarray = m_Datas[typeIndex];
			return iarray->Set(index, src);
		}

	};

	template<typename T>
	INT SharedDataEx::GetTypeIndex() {
		return -1;
	}

#define YBEHAVIOR_SHAREDDATA_STORE_INDEX(type, id)			\
		template<> inline INT SharedDataEx::GetTypeIndex<type>() \
		{\
			return id;\
		}


	YBEHAVIOR_SHAREDDATA_STORE_INDEX(Int, 0);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(Uint64, 1);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(Bool, 2);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(Float, 3);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(String, 4);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(AgentWrapper, 5);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(Vector3, 6);

	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecInt, 7);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecUint64, 8);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecBool, 9);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecFloat, 10);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecString, 11);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecAgentWrapper, 12);
	YBEHAVIOR_SHAREDDATA_STORE_INDEX(VecVector3, 13);

	template<typename T>
	bool SharedDataEx::Get(INT index, T& res)
	{
		IDataArray* iarray = m_Datas[GetTypeIndex<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Get(index, res);
	}
	template<typename T>
	const T* SharedDataEx::Get(INT index)
	{
		IDataArray* iarray = m_Datas[GetTypeIndex<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return (const T*)parray->Get(index);
	}

	template<typename T>
	bool SharedDataEx::Set(INT index, const T& src)
	{
		IDataArray* iarray = m_Datas[GetTypeIndex<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Set(index, std::move(src));
	}


	template<typename T>
	bool SharedDataEx::Set(INT index, const T* src)
	{
		IDataArray* iarray = m_Datas[GetTypeIndex<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Set(index, *src);
	}

	template<typename T>
	bool SharedDataEx::Set(INT index, T&& src)
	{
		IDataArray* iarray = m_Datas[GetTypeIndex<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Set(index, std::move(src));
	}

	///
}

#endif