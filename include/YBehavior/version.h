#ifndef _YBEHAVIOR_VERSION_H_
#define _YBEHAVIOR_VERSION_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"
#include "YBehavior/types/smallmap.h"
#include <iostream>
#include <map>
#include "YBehavior/fsm/behavior.h"

namespace YBehavior
{
	template<typename T>
	struct Version
	{
		int versionID{ -1 };
		T* data{ nullptr };
		int referenceCount{ 0 };

		bool Invalid() const { return !data && referenceCount < 0; }
		void TrySetInvalid() { if (!data) referenceCount = -1; }
		void ResetValid() { if (Invalid()) referenceCount = 0; }
	};

	template<typename T>
	class Info
	{
	public:
		typedef Version<T> VersionType;
		~Info();

		void TryRemoveVersion(VersionType* version);
		VersionType* CreateVersion();
		T* GetLatest() { return m_LatestVersion ? m_LatestVersion->data : nullptr; }
		inline VersionType* GetLatestVersion() { return m_LatestVersion; }
		void IncreaseLatestVesion();

		void SetLatest(T* data);
		void ChangeReferenceCount(bool bInc, VersionType* version = nullptr);
		void Print();

		inline small_map<int, VersionType*>& GetVersions() { return m_Versions; }
	private:
		VersionType * m_LatestVersion{ nullptr };
		VersionType* m_PreviousVersion{ nullptr };
		small_map<int, VersionType*> m_Versions;
	};

	template<typename T>
	void Info<T>::Print()
	{
		for (auto it = m_Versions.begin(); it != m_Versions.end(); ++it)
		{
			std::cout << "version " << it->first << ", count " << it->second->referenceCount << std::endl;
		}
	}

	template<typename T>
	void Info<T>::ChangeReferenceCount(bool bInc, typename Info<T>::VersionType* version /*= nullptr*/)
	{
		if (version == nullptr)
			version = m_LatestVersion;
		else
		{
			auto it = m_Versions.find(version->versionID);
			if (it != m_Versions.end() && it->second != version)
				version = nullptr;
		}

		if (version == nullptr)
			return;

		if (bInc)
		{
			++(version->referenceCount);
		}
		else
		{
			--(version->referenceCount);

			if (m_LatestVersion != version)
			{
				///> Old version has no reference, remove it
				TryRemoveVersion(version);
			}
		}

	}

	template<typename T>
	void Info<T>::SetLatest(T* data)
	{
		if (m_LatestVersion == nullptr)
			CreateVersion();
		m_LatestVersion->data = data;
		if (data)
			data->SetVersion(m_LatestVersion);
		else
			m_LatestVersion->TrySetInvalid();
	}

	template<typename T>
	void Info<T>::IncreaseLatestVesion()
	{
		if (m_LatestVersion == nullptr)
			return;
		
		if (m_LatestVersion->Invalid())
		{
			m_LatestVersion->ResetValid();
			return;
		}

		if (m_LatestVersion->data == nullptr)
			return;

		CreateVersion();
	}

	template<typename T>
	typename Info<T>::VersionType* Info<T>::CreateVersion()
	{
		VersionType* pVersion = new VersionType();
		if (m_LatestVersion == nullptr)
		{
			pVersion->versionID = 0;
		}
		else
		{
			pVersion->versionID = m_LatestVersion->versionID + 1;

			///> Check if the current latest version has no reference. Remove it if true
			{
				TryRemoveVersion(m_LatestVersion);
			}
		}
		m_PreviousVersion = m_LatestVersion;
		m_LatestVersion = pVersion;
		m_Versions[pVersion->versionID] = pVersion;

		return pVersion;

	}

	template<typename T>
	Info<T>::~Info()
	{
		for (auto it = m_Versions.begin(); it != m_Versions.end(); ++it)
		{
			delete it->second->data;
		}
		m_Versions.clear();
	}


	template<typename T>
	void Info<T>::TryRemoveVersion(typename Info<T>::VersionType* version)
	{
		if (version == nullptr || version->referenceCount > 0)
			return;

		m_Versions.erase(version->versionID);

		if (version->data)
		{
			delete version->data;
			version->data = nullptr;
		}

		if (m_PreviousVersion == version)
			m_PreviousVersion = nullptr;
		if (m_LatestVersion == version)
			m_LatestVersion = nullptr;
		delete version;
	}

	//////////////////////////////////////////////////////////////////////////
	template<typename DataType, typename KeyType>
	class VersionMgr
	{
	public:
		typedef Info<DataType> InfoType;
		typedef std::map<KeyType, InfoType*> InfoListType;
		VersionMgr() {}
		~VersionMgr();
		bool GetData(const KeyType& key, DataType* &outputData, InfoType* &outputInfo);
		///> Mark this tree dirty to reload it when GetTree
		void Reload(const KeyType& key);
		void ReloadAll();
		void Return(DataType* data);
		inline InfoListType& GetInfos() { return m_Infos; }
		void Clear();
	private:
		//static TreeMgr* s_Instance;

		InfoListType m_Infos;
	};

	template<typename DataType, typename KeyType>
	void VersionMgr<DataType, KeyType>::Return(DataType* data)
	{
		if (data == nullptr)
			return;

		auto it = m_Infos.find(data->GetKey());
		if (it != m_Infos.end())
		{
			it->second->ChangeReferenceCount(false, (typename InfoType::VersionType*)data->GetVersion());
		}
	}

	template<typename DataType, typename KeyType>
	void VersionMgr<DataType, KeyType>::ReloadAll()
	{
		for (auto& it : m_Infos)
		{
			it.second->IncreaseLatestVesion();
		}
	}

	template<typename DataType, typename KeyType>
	void VersionMgr<DataType, KeyType>::Reload(const KeyType& key)
	{
		auto it = m_Infos.find(key);
		if (it != m_Infos.end())
		{
			it->second->IncreaseLatestVesion();
		}
	}

	template<typename DataType, typename KeyType>
	bool VersionMgr<DataType, KeyType>::GetData(const KeyType& key, DataType* &outputData, InfoType* &outputInfo)
	{
		outputInfo = nullptr;
		auto it = m_Infos.find(key);
		if (it != m_Infos.end())
		{
			outputInfo = it->second;
			outputData = outputInfo->GetLatest();
			if (outputData)
			{
				outputInfo->ChangeReferenceCount(true);
				return true;
			}
		}

		if (outputInfo == nullptr)
		{
			outputInfo = new InfoType();

			m_Infos[key] = outputInfo;
		}

		return false;
	}

	template<typename DataType, typename KeyType>
	void VersionMgr<DataType, KeyType>::Clear()
	{
		for (auto it = m_Infos.begin(); it != m_Infos.end(); ++it)
		{
			delete it->second;
		}
		m_Infos.clear();
	}

	template<typename DataType, typename KeyType>
	VersionMgr<DataType, KeyType>::~VersionMgr()
	{
		Clear();
	}

}

#endif