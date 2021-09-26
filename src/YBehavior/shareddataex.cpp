#include "YBehavior/shareddataex.h"
#include "YBehavior/sharedvariablecreatehelper.h"

namespace YBehavior
{
	SharedDataEx::SharedDataEx()
	{
	}

	SharedDataEx::SharedDataEx(const SharedDataEx& other)
	{
		for (int i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (other.m_Datas[i])
			{
				m_Datas[i] = other.m_Datas[i]->Clone();
			}
		}
	}

	SharedDataEx::~SharedDataEx()
	{
		for (int i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (m_Datas[i])
				delete m_Datas[i];
		}
	}

	void SharedDataEx::CloneFrom(const SharedDataEx& other)
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (other.m_Datas[i])
			{
				if (m_Datas[i])
					m_Datas[i]->CloneFrom(other.m_Datas[i]);
				else
					m_Datas[i] = other.m_Datas[i]->Clone();
			}
			else
			{
				if (m_Datas[i])
				{
					delete m_Datas[i];
					m_Datas[i] = nullptr;
				}
			}
		}
	}

	void SharedDataEx::MergeFrom(const SharedDataEx& other, bool bOverride)
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (other.m_Datas[i])
			{
				if (m_Datas[i])
					m_Datas[i]->MergeFrom(other.m_Datas[i], bOverride);
				else
					m_Datas[i] = other.m_Datas[i]->Clone();
			}
		}
	}

	void* SharedDataEx::Get(KEY key, TYPEID typeID)
	{
		IDataArray* iarray = m_Datas[typeID];
		if (!iarray)
			return nullptr;
		return (void*)iarray->Get(key);
	}

	STRING SharedDataEx::GetToString(KEY key, TYPEID typeID)
	{
		IDataArray* iarray = m_Datas[typeID];
		if (!iarray)
			return Utility::StringEmpty;
		return iarray->GetToString(key);
	}

	bool SharedDataEx::Set(KEY key, TYPEID typeID, const void* src)
	{
		IDataArray* iarray = _ForceGetDataArray(typeID);
		return iarray->Set(key, src);
	}

	bool SharedDataEx::SetDefault(KEY key, TYPEID typeID)
	{
		IDataArray* iarray = _ForceGetDataArray(typeID);
		return iarray->SetDefault(key);
	}

	bool SharedDataEx::TrySet(KEY key, TYPEID typeKey, const void* src)
	{
		IDataArray* iarray = m_Datas[typeKey];
		if (!iarray)
			return false;
		return iarray->TrySet(key, src);
	}

	void SharedDataEx::Clear()
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (m_Datas[i])
				m_Datas[i]->Clear();
		}
	}

	YBehavior::IDataArray* SharedDataEx::_ForceGetDataArray(TYPEID typeID)
	{
		IDataArray* iarray = m_Datas[typeID];
		if (iarray == nullptr)
		{
			iarray = SharedVariableCreateHelperMgr::Get(typeID)->CreateDataArray();
			m_Datas[typeID] = iarray;
		}
		return iarray;
	}

}
