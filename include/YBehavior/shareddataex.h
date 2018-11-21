#ifndef _YBEHAVIOR_SHAREDDATAEX_H_
#define _YBEHAVIOR_SHAREDDATAEX_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"
#include "YBehavior/tools/meta.h"
#include "YBehavior/interface.h"
#include <unordered_map>

namespace YBehavior
{
	template<typename K, typename T>
	struct DataArrayMapDef
	{
		typedef std::unordered_map<K, T> type;
	};

	template<typename T>
	class DataArrayIterator : public IDataArrayIterator
	{
		typename DataArrayMapDef<KEY, T>::type::const_iterator m_It;
		typename DataArrayMapDef<KEY, T>::type::const_iterator m_End;
	public:
		DataArrayIterator(typename DataArrayMapDef<KEY, T>::type::const_iterator begin, typename DataArrayMapDef<KEY, T>::type::const_iterator end)
			: m_It(begin)
			, m_End(end)
		{
		}

		bool IsEnd() override { return m_It == m_End; }
		IDataArrayIterator& operator ++() override { ++m_It; return *this; }
		const KEY Value() { return m_It->first; }
	};

	template<typename T>
	class DataArray : public IDataArray
	{
		typename DataArrayMapDef<KEY, T>::type m_Datas;
	public:
		IDataArray * Clone() const override
		{
			DataArray<T>* newArray = new DataArray<T>();
			newArray->m_Datas = m_Datas;
			return newArray;
		}

		Iterator Iter() const override
		{
			typename DataArrayMapDef<KEY, T>::type::const_iterator itBegin = m_Datas.begin();
			typename DataArrayMapDef<KEY, T>::type::const_iterator itEnd = m_Datas.end();
			DataArrayIterator<T>* innerIt = new DataArrayIterator<T>(itBegin, itEnd);
			Iterator it(innerIt);
			return std::move(it);
		}

		void Merge(IDataArray* other, bool bOverride) override
		{
			DataArray<T>* otherArray = (DataArray<T>*) other;
			for (auto it = otherArray->m_Datas.begin(); it != otherArray->m_Datas.end(); ++it)
			{
				if (!bOverride)
					this->m_Datas[it->first] = it->second;
				else
				{
					KEY key = it->first;
					T val = it->second;
					m_Datas.insert(std::make_pair<KEY, T>(std::move(key), std::move(val)));
				}
			}
			typename DataArrayMapDef<KEY, T>::type::iterator it2; otherArray->m_Datas.end();
		}

		TYPEID TypeID() const override
		{
			return GetTypeID<T>();
		}

		SIZE_KEY Length() const override { return (SIZE_KEY)m_Datas.size(); }

		bool Get(KEY key, T& res) const
		{
			//if (key < 0 || key >= (KEY)m_Datas.size())
			//	return false;
			//res = m_Datas[key];
			//return true;
			auto it = m_Datas.find(key);
			if (it == m_Datas.end())
				return false;
			res = it->second;
			return true;
		}

		virtual const void* Get(KEY key) const override
		{
			//if (key < 0 || key >= (KEY)m_Datas.size())
			//	return nullptr;
			//const T* data = &m_Datas[key];
			//return (data);
			auto it = m_Datas.find(key);
			if (it == m_Datas.end())
				return nullptr;
			return (const void*)&(it->second);
		}

		virtual const STRING GetToString(KEY key) const override
		{
			//if (key < 0 || key >= (KEY)m_Datas.size())
			//	return Utility::StringEmpty;
			//const T& data = m_Datas[key];
			//return Utility::ToString(data);
			auto it = m_Datas.find(key);
			if (it == m_Datas.end())
				return Utility::StringEmpty;
			const T& data = it->second;
			return Utility::ToString(data);
		}

		bool Set(KEY key, const T& src)
		{
			//if (key < 0)
			//	return false;
			//if ((KEY)m_Datas.size() <= key)
			//{
			//	m_Datas.resize(key);
			//	m_Datas.push_back(src);
			//}
			//else
			//{
			//	m_Datas[key] = src;
			//}
			//return true;
			m_Datas[key] = src;
			return true;
		}

		bool Set(KEY key, T&& src)
		{
			//if (key < 0)
			//	return false;
			//if ((KEY)m_Datas.size() <= key)
			//{
			//	m_Datas.resize(key);
			//	m_Datas.push_back(src);
			//}
			//else
			//{
			//	m_Datas[key] = src;
			//}
			//return true;
			m_Datas[key] = src;
			return true;
		}

		virtual bool Set(KEY key, const void* src) override
		{
			//if (key < 0 || src == nullptr)
			//	return false;
			//if ((KEY)m_Datas.size() <= key)
			//{
			//	m_Datas.resize(key);
			//	m_Datas.push_back(*((T*)src));
			//}
			//else
			//{
			//	m_Datas[key] = *((T*)src);
			//}
			//return true;
			if (src != nullptr)
				m_Datas[key] = *((T*)src);
			return true;
		}
	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

#define MAX_TYPE_KEY 14

	class SharedDataEx
	{
	public:

		template<typename T>
		KEY GetTypeKey();
		

	protected:
		IDataArray* m_Datas[MAX_TYPE_KEY];

	public:
		SharedDataEx();
		SharedDataEx(SharedDataEx&& other);

		~SharedDataEx();

		inline const IDataArray* GetDataArray(KEY typeKey) { return m_Datas[typeKey]; }

		void Clone(const SharedDataEx& other);

		void Merge(const SharedDataEx& other, bool bOverride);

		template<typename T>
		bool Get(KEY key, T& res);

		template<typename T>
		const T* Get(KEY key);

		const void* Get(KEY key, KEY typeKey)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return (const void*)iarray->Get(key);
		}
		STRING GetToString(KEY key, KEY typeKey)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return iarray->GetToString(key);
		}

		template<typename T>
		bool Set(KEY key, const T& src);


		template<typename T>
		bool Set(KEY key, const T* src);

		template<typename T>
		bool Set(KEY key, T&& src);

		bool Set(KEY key, KEY typeKey, void* src)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return iarray->Set(key, src);
		}
	};

	template<typename T>
	KEY SharedDataEx::GetTypeKey() {
		return -1;
	}

#define YBEHAVIOR_SHAREDDATA_STORE_KEY(type, id)			\
		template<> inline KEY SharedDataEx::GetTypeKey<type>() \
		{\
			return id;\
		}


	YBEHAVIOR_SHAREDDATA_STORE_KEY(Int, 0);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Uint64, 1);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Bool, 2);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Float, 3);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(String, 4);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(EntityWrapper, 5);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Vector3, 6);

	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecInt, 7);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecUint64, 8);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecBool, 9);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecFloat, 10);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecString, 11);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecEntityWrapper, 12);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecVector3, 13);

	template<typename T>
	bool SharedDataEx::Get(KEY key, T& res)
	{
		IDataArray* iarray = m_Datas[GetTypeKey<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Get(key, res);
	}
	template<typename T>
	const T* SharedDataEx::Get(KEY key)
	{
		IDataArray* iarray = m_Datas[GetTypeKey<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return (const T*)parray->Get(key);
	}

	template<typename T>
	bool SharedDataEx::Set(KEY key, const T& src)
	{
		IDataArray* iarray = m_Datas[GetTypeKey<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Set(key, src);
	}


	template<typename T>
	bool SharedDataEx::Set(KEY key, const T* src)
	{
		IDataArray* iarray = m_Datas[GetTypeKey<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Set(key, *src);
	}

	template<typename T>
	bool SharedDataEx::Set(KEY key, T&& src)
	{
		IDataArray* iarray = m_Datas[GetTypeKey<T>()];
		DataArray<T>* parray = (DataArray<T>*)iarray;
		return parray->Set(key, src);
	}

	///
}

#endif
