#ifndef _YBEHAVIOR_SHAREDVARIABLECREATEHELPER_H_
#define _YBEHAVIOR_SHAREDVARIABLECREATEHELPER_H_

#include "types.h"
#include "nodefactory.h"
#include "shareddataex.h"
#include <unordered_map>
#include "sharedvariableex.h"

namespace YBehavior
{
	class ISharedVariableCreateHelper
	{
	public:
		virtual ~ISharedVariableCreateHelper() {}
		virtual ISharedVariableEx* CreateVariable() const = 0;
		virtual void SetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str) const = 0;
		virtual bool TrySetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str, CHAR separator = '|') const = 0;
	};

	template<typename valueType>
	class SharedVariableCreateHelper: public ISharedVariableCreateHelper
	{
	public:
		ISharedVariableEx* CreateVariable() const override
		{
			return new SharedVariableEx<valueType>();
		}
		void SetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str) const override
		{
			KEY key = TreeKeyMgr::Instance()->CreateKeyByName<valueType>(name);
			pData->Set(key, Utility::ToType<valueType>(str));
		}
		bool TrySetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str, CHAR separator = '|') const override
		{
			KEY key = TreeKeyMgr::Instance()->GetKeyByName<valueType>(name);
			if (key == Utility::INVALID_KEY)
				return false;
			return pData->TrySet(key, Utility::ToType<valueType>(str));
		}
	};

	template<typename elementType>
	class SharedVariableCreateHelper<StdVector<elementType>> : public ISharedVariableCreateHelper
	{
	public:
		ISharedVariableEx* CreateVariable() const override
		{
			return new SharedVariableEx<StdVector<elementType>>();
		}
		void SetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str) const override
		{
			KEY key = TreeKeyMgr::Instance()->CreateKeyByName<StdVector<elementType>>(name);
			StdVector<STRING> splitRes;
			StdVector<elementType> res;
			Utility::SplitString(str, splitRes, '|');
			for (auto it = splitRes.begin(); it != splitRes.end(); ++it)
			{
				res.push_back(Utility::ToType<elementType>(*it));
			}
			pData->Set(key, std::move(res));
		}
		bool TrySetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str, CHAR separator = '|') const override
		{
			KEY key = TreeKeyMgr::Instance()->GetKeyByName<StdVector<elementType>>(name);
			if (key == Utility::INVALID_KEY)
				return false;
			StdVector<STRING> splitRes;
			StdVector<elementType> res;
			Utility::SplitString(str, splitRes, separator);
			for (auto it = splitRes.begin(); it != splitRes.end(); ++it)
			{
				res.push_back(Utility::ToType<elementType>(*it));
			}
			return pData->TrySet(key, std::move(res));
		}
	};

	class SharedVariableCreateHelperMgr
	{
	public:
		typedef std::unordered_map<STRING, ISharedVariableCreateHelper*> HelperMapType;
	protected:
		static HelperMapType* _Helpers;
	public:
		template<typename T>
		static void Register(const STRING& s)
		{
			(*_Helpers)[s] = new SharedVariableCreateHelper<T>();
		}

		static ISharedVariableCreateHelper* Get(const STRING& s)
		{
			HelperMapType::iterator it;
			if ((it = _Helpers->find(s)) != _Helpers->end())
			{
				return it->second;
			}
			return nullptr;
		}

		static const HelperMapType& GetAllHelpers() { return *_Helpers; }
		friend class Constructor;
		class Constructor {
		public:
			Constructor()
			{
				_Helpers = new HelperMapType();
#define REGISTER_HELPER(T, s)\
	SharedVariableCreateHelperMgr::Register<T>(s)

				REGISTER_HELPER(Int, "_I");
				REGISTER_HELPER(Uint64, "_U");
				REGISTER_HELPER(Bool, "_B");
				REGISTER_HELPER(Float, "_F");
				REGISTER_HELPER(String, "_S");
				REGISTER_HELPER(EntityWrapper, "_A");
				REGISTER_HELPER(Vector3, "_V");
				REGISTER_HELPER(VecInt, "II");
				REGISTER_HELPER(VecUint64, "UU");
				REGISTER_HELPER(VecBool, "BB");
				REGISTER_HELPER(VecFloat, "FF");
				REGISTER_HELPER(VecString, "SS");
				REGISTER_HELPER(VecEntityWrapper, "AA");
				REGISTER_HELPER(VecVector3, "VV");
			}
		};
		static Constructor cons;

	};
}

#endif