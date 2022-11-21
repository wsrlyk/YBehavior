#ifndef _YBEHAVIOR_SHAREDDATAEX_H_
#define _YBEHAVIOR_SHAREDDATAEX_H_

#include "YBehavior/types/types.h"
#include "YBehavior/utility.h"
#include "YBehavior/tools/meta.h"
#include "YBehavior/interface.h"
#include "tools/objectpool.h"
#include "YBehavior/types/smallmap.h"
namespace YBehavior
{
	template<typename K, typename T>
	struct DataArrayMapDef
	{
		using type = small_map<K, T>;
		using const_iterator = typename type::const_iterator;
		using iterator = typename type::iterator;
	};

	template<typename T>
	class DataArrayIterator : public IDataArrayIterator
	{
		typename DataArrayMapDef<KEY, T>::const_iterator m_It;
		typename DataArrayMapDef<KEY, T>::const_iterator m_End;
	public:
		void Set(const typename DataArrayMapDef<KEY, T>::const_iterator& begin, const typename DataArrayMapDef<KEY, T>::const_iterator& end)
		{
			m_It = begin;
			m_End = end;
		}

		bool IsEnd() override { return m_It == m_End; }
		IDataArrayIterator& operator ++() override { ++m_It; return *this; }
		const KEY Value() override { return m_It->first; }

		void Recycle() override
		{
			ObjectPoolStatic<DataArrayIterator<T>>::Recycle(this);
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

		IDataArray* Clone() override
		{
			DataArray<T>* other = new DataArray<T>(*this);
			return other;
		}

		Iterator Iter() const override
		{
			auto itBegin = m_Datas.begin();
			auto itEnd = m_Datas.end();
			DataArrayIterator<T>* innerIt = ObjectPoolStatic<DataArrayIterator<T>>::Get();
			innerIt->Set(itBegin, itEnd);
			Iterator it(innerIt);
			return it;
		}

		void MergeFrom(const IDataArray* other, bool useNewValue) override
		{
			if (other == nullptr)
				return;

			const DataArray<T>* otherArray = (const DataArray<T>*) other;
			for (auto it = otherArray->m_Datas.begin(); it != otherArray->m_Datas.end(); ++it)
			{
				if (useNewValue)
					this->m_Datas[it->first] = it->second;
				else
				{
					m_Datas.insert({ it->first, it->second });
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

		const void* Get(KEY key) const override
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

		const STRING GetToString(KEY key) const override
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

		bool SetDefault(KEY key) override
		{
			return Utility::SetDefault<T>(m_Datas[key]);
		}

		bool TrySet(KEY key, const T& src)
		{
			auto it = m_Datas.find(key);
			if (it == m_Datas.end())
				return false;
			it->second = src;
			return true;
		}

		bool TrySet(KEY key, T&& src)
		{
			auto it = m_Datas.find(key);
			if (it == m_Datas.end())
				return false;
			it->second = std::move(src);
			return true;
		}

		bool TrySet(KEY key, const void* src) override
		{
			if (src == nullptr)
				return false;

			auto it = m_Datas.find(key);
			if (it == m_Datas.end())
				return false;
			
			it->second = *((T*)src);
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
	class SharedDataEx
	{
	protected:
		IDataArray* m_Datas[MAX_TYPE_KEY]{ nullptr };

	public:
		SharedDataEx();
		SharedDataEx(const SharedDataEx& other);

		~SharedDataEx();

		inline const IDataArray* GetDataArray(TYPEID typeID) { return m_Datas[typeID]; }

		void CloneFrom(const SharedDataEx& other);

		void MergeFrom(const SharedDataEx& other, bool useNewValue);

		template<typename T>
		bool Get(KEY key, T& res);

		template<typename T>
		T* Get(KEY key);

		void* Get(KEY key, TYPEID typeID);

		STRING GetToString(KEY key, TYPEID typeID);

		template<typename T>
		bool Set(KEY key, const T* src);

		template<typename T>
		bool Set(KEY key, T&& src);

		bool Set(KEY key, TYPEID typeID, const void* src);
		bool SetDefault(KEY key, TYPEID typeID);

		template<typename T>
		bool TrySet(KEY key, const T* src);

		template<typename T>
		bool TrySet(KEY key, T&& src);

		bool TrySet(KEY key, TYPEID typeKey, const void* src);

		void Clear();

		IDataArray* _ForceGetDataArray(TYPEID typeID);
	};



	template<typename T>
	bool SharedDataEx::Get(KEY key, T& res)
	{
		TYPEID typeID = GetTypeID<T>();
		if (typeID < 0)
			return false;
		DataArray<T>* parray = (DataArray<T>*)m_Datas[typeID];
		if (!parray)
			return false;
		return parray->Get(key, res);
	}
	template<typename T>
	T* SharedDataEx::Get(KEY key)
	{
		TYPEID typeID = GetTypeID<T>();
		if (typeID < 0)
			return nullptr;
		DataArray<T>* parray = (DataArray<T>*)m_Datas[typeID];
		if (!parray)
			return nullptr;
		return (T*)parray->Get(key);
	}

	template<typename T>
	bool SharedDataEx::Set(KEY key, const T* src)
	{
		if (src == nullptr)
			return false;
		TYPEID typeID = GetTypeID<T>();
		if (typeID < 0)
			return false;
		auto iarray = _ForceGetDataArray(typeID);
		return ((DataArray<T>*)iarray)->Set(key, *src);
	}

	template<typename T>
	bool SharedDataEx::Set(KEY key, T&& src)
	{
		using t_type = typename std::remove_const<typename std::remove_reference<T>::type>::type;
		TYPEID typeID = GetTypeID<t_type>();
		if (typeID < 0)
			return false;
		auto iarray = _ForceGetDataArray(typeID);
		return ((DataArray<t_type>*)iarray)->Set(key, std::forward<T>(src));
	}

	template<typename T>
	bool SharedDataEx::TrySet(KEY key, const T* src)
	{
		if (src == nullptr)
			return false;
		TYPEID typeID = GetTypeID<T>();
		if (typeID < 0)
			return false;

		DataArray<T>* parray = (DataArray<T>*)m_Datas[typeID];
		if (!parray)
			return false;
		return parray->TrySet(key, *src);
	}

	template<typename T>
	bool SharedDataEx::TrySet(KEY key, T&& src)
	{
		using t_type = typename std::remove_const<typename std::remove_reference<T>::type>::type;
		TYPEID typeID = GetTypeID<t_type>();
		if (typeID < 0)
			return false;

		DataArray<t_type>* parray = (DataArray<t_type>*)m_Datas[typeID];
		if (!parray)
			return false;
		return parray->TrySet(key, std::forward<T>(src));
	}

	///
}

#endif
