#include "YBehavior/shareddataex.h"
#include "YBehavior/sharedvariablecreatehelper.h"

namespace YBehavior
{
	VariableCollection::VariableCollection()
	{
	}

	VariableCollection::VariableCollection(const VariableCollection& other)
	{
		for (int i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (other.m_Datas[i])
			{
				m_Datas[i] = other.m_Datas[i]->Clone();
			}
		}
	}

	VariableCollection::~VariableCollection()
	{
		for (int i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (m_Datas[i])
				delete m_Datas[i];
		}
	}

	void VariableCollection::CloneFrom(const VariableCollection& other)
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

	void VariableCollection::MergeFrom(const VariableCollection& other, bool useNewValue)
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (other.m_Datas[i])
			{
				if (m_Datas[i])
					m_Datas[i]->MergeFrom(other.m_Datas[i], useNewValue);
				else
					m_Datas[i] = other.m_Datas[i]->Clone();
			}
		}
	}

	void* VariableCollection::Get(KEY key, TYPEID typeID)
	{
		IDataArray* iarray = m_Datas[typeID];
		if (!iarray)
			return nullptr;
		return (void*)iarray->Get(key);
	}

	STRING VariableCollection::GetToString(KEY key, TYPEID typeID)
	{
		IDataArray* iarray = m_Datas[typeID];
		if (!iarray)
			return Utility::StringEmpty;
		return iarray->GetToString(key);
	}

	bool VariableCollection::Set(KEY key, TYPEID typeID, const void* src)
	{
		IDataArray* iarray = _ForceGetDataArray(typeID);
		return iarray->Set(key, src);
	}

	bool VariableCollection::SetDefault(KEY key, TYPEID typeID)
	{
		IDataArray* iarray = _ForceGetDataArray(typeID);
		return iarray->SetDefault(key);
	}

	bool VariableCollection::TrySet(KEY key, TYPEID typeKey, const void* src)
	{
		IDataArray* iarray = m_Datas[typeKey];
		if (!iarray)
			return false;
		return iarray->TrySet(key, src);
	}

	void VariableCollection::Clear()
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (m_Datas[i])
				m_Datas[i]->Clear();
		}
	}

	YBehavior::IDataArray* VariableCollection::_ForceGetDataArray(TYPEID typeID)
	{
		IDataArray* iarray = m_Datas[typeID];
		if (iarray == nullptr)
		{
			iarray = DataCreateHelperMgr::Get(typeID)->CreateDataArray();
			m_Datas[typeID] = iarray;
		}
		return iarray;
	}

}
