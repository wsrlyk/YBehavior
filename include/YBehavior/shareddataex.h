#ifndef _YBEHAVIOR_SHAREDDATAEX_H_
#define _YBEHAVIOR_SHAREDDATAEX_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"
#include "YBehavior/tools/meta.h"
#include "YBehavior/interface.h"
#include <unordered_map>
#include "tools/objectpool.h"

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
		void Set(const typename DataArrayMapDef<KEY, T>::type::const_iterator& begin, const typename DataArrayMapDef<KEY, T>::type::const_iterator& end)
		{
			m_It = begin;
			m_End = end;
		}

		bool IsEnd() override { return m_It == m_End; }
		IDataArrayIterator& operator ++() override { ++m_It; return *this; }
		const KEY Value() { return m_It->first; }

		void Recycle() override
		{
			ObjectPool<DataArrayIterator<T>>::Recycle(this);
		}
	};

	template<typename T>
	class DataArray : public IDataArray
	{
		typename DataArrayMapDef<KEY, T>::type m_Datas;
	public:
		void CloneFrom(const IDataArray* pOther) override
		{
			if (pOther == nullptr)
				return;

			const DataArray<T>* other = static_cast<const DataArray<T>*>(pOther);

			m_Datas = other->m_Datas;
		}

		Iterator Iter() const override
		{
			typename DataArrayMapDef<KEY, T>::type::const_iterator itBegin = m_Datas.begin();
			typename DataArrayMapDef<KEY, T>::type::const_iterator itEnd = m_Datas.end();
			DataArrayIterator<T>* innerIt = ObjectPool<DataArrayIterator<T>>::Get();
			innerIt->Set(itBegin, itEnd);
			Iterator it(innerIt);
			return std::move(it);
		}

		void MergeFrom(const IDataArray* other, bool bOverride) override
		{
			if (other == nullptr)
				return;

			const DataArray<T>* otherArray = (const DataArray<T>*) other;
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
			//	return Types::StringEmpty;
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

		bool Set(KEY key, const void* src) override
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
		bool TrySet(KEY key, const T& src)
		{
			if (m_Datas.find(key) == m_Datas.end())
				return false;
			m_Datas[key] = src;
			return true;
		}

		bool TrySet(KEY key, T&& src)
		{
			if (m_Datas.find(key) == m_Datas.end())
				return false;
			m_Datas[key] = src;
			return true;
		}

		bool TrySet(KEY key, const void* src) override
		{
			if (m_Datas.find(key) == m_Datas.end())
				return false;
			if (src != nullptr)
				m_Datas[key] = *((T*)src);
			return true;
		}


		void Clear() override
		{
			m_Datas.clear();
		}
	};

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

#define MAX_TYPE_KEY 14
#define DATAARRAY_ONETYPE_DEFINE(type) 		DataArray<type> m_Data##type;
	class SharedDataEx
	{
	public:

		template<typename T>
		KEY GetTypeKey();
		

	protected:
		IDataArray* m_Datas[MAX_TYPE_KEY];

		FOR_EACH_TYPE(DATAARRAY_ONETYPE_DEFINE)
	public:
		SharedDataEx();
		SharedDataEx(const SharedDataEx& other);

		~SharedDataEx();

		inline const IDataArray* GetDataArray(KEY typeKey) { return m_Datas[typeKey]; }

		void CloneFrom(const SharedDataEx& other);

		void MergeFrom(const SharedDataEx& other, bool bOverride);

		template<typename T>
		bool Get(KEY key, T& res);

		template<typename T>
		T* Get(KEY key);

		void* Get(KEY key, KEY typeKey)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return (void*)iarray->Get(key);
		}
		STRING GetToString(KEY key, KEY typeKey)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return iarray->GetToString(key);
		}

		template<typename T>
		bool Set(KEY key, const T* src);

		template<typename T>
		bool Set(KEY key, T&& src);

		bool Set(KEY key, KEY typeKey, const void* src)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return iarray->Set(key, src);
		}

		template<typename T>
		bool TrySet(KEY key, const T* src);

		template<typename T>
		bool TrySet(KEY key, T&& src);

		bool TrySet(KEY key, KEY typeKey, void* src)
		{
			IDataArray* iarray = m_Datas[typeKey];
			return iarray->TrySet(key, src);
		}

		void Clear();
	};


	YBEHAVIOR_SHAREDDATA_STORE_KEY(Int, 0);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Ulong, 1);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Bool, 2);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Float, 3);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(String, 4);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(EntityWrapper, 5);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(Vector3, 6);

	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecInt, 7);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecUlong, 8);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecBool, 9);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecFloat, 10);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecString, 11);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecEntityWrapper, 12);
	YBEHAVIOR_SHAREDDATA_STORE_KEY(VecVector3, 13);

	template<typename T>
	bool SharedDataEx::Get(KEY key, T& res)
	{
		KEY idx = GetTypeKey<T>();
		if (idx < 0)
			return false;
		DataArray<T>* parray = (DataArray<T>*)m_Datas[idx];
		return parray->Get(key, res);
	}
	template<typename T>
	T* SharedDataEx::Get(KEY key)
	{
		KEY idx = GetTypeKey<T>();
		if (idx < 0)
			return nullptr;
		DataArray<T>* parray = (DataArray<T>*)m_Datas[idx];
		return (T*)parray->Get(key);
	}

	template<typename T>
	bool SharedDataEx::Set(KEY key, const T* src)
	{
		if (src == nullptr)
			return false;
		KEY idx = GetTypeKey<T>();
		if (idx < 0)
			return false;
		DataArray<T>* parray = (DataArray<T>*)m_Datas[idx];
		return parray->Set(key, *src);
	}

	template<typename T>
	bool SharedDataEx::Set(KEY key, T&& src)
	{
		using t_type = typename std::remove_const<typename std::remove_reference<T>::type>::type;
		KEY idx = GetTypeKey<t_type>();
		if (idx < 0)
			return false;

		DataArray<t_type>* parray = (DataArray<t_type>*)m_Datas[idx];
		return parray->Set(key, std::forward<T>(src));
	}

	template<typename T>
	bool SharedDataEx::TrySet(KEY key, const T* src)
	{
		if (src == nullptr)
			return false;
		KEY idx = GetTypeKey<T>();
		if (idx < 0)
			return false;

		DataArray<T>* parray = (DataArray<T>*)m_Datas[idx];
		return parray->TrySet(key, *src);
	}

	template<typename T>
	bool SharedDataEx::TrySet(KEY key, T&& src)
	{
		using t_type = typename std::remove_const<typename std::remove_reference<T>::type>::type;
		KEY idx = GetTypeKey<t_type>();
		if (idx < 0)
			return false;

		DataArray<t_type>* parray = (DataArray<t_type>*)m_Datas[idx];
		return parray->TrySet(key, src);
	}

	///
}

#endif
