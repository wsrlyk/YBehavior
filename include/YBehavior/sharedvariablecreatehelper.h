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
		virtual ISharedVariableEx* CreateVariable() = 0;
		virtual void SetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str) = 0;
	};

	template<typename valueType>
	class SharedVariableCreateHelper: public ISharedVariableCreateHelper
	{
	public:
		virtual ISharedVariableEx* CreateVariable() override
		{
			return new SharedVariableEx<valueType>();
		}
		virtual void SetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str) override
		{
			INT index = NodeFactory::Instance()->CreateIndexByName<valueType>(name);
			pData->Set(index, Utility::ToType<valueType>(str));
		}
	};

	template<typename elementType>
	class SharedVariableCreateHelper<std::vector<elementType>> : public ISharedVariableCreateHelper
	{
	public:
		virtual ISharedVariableEx* CreateVariable() override
		{
			return new SharedVariableEx<elementType>();
		}
		virtual void SetSharedData(SharedDataEx* pData, const STRING& name, const STRING& str) override
		{
			INT index = NodeFactory::Instance()->CreateIndexByName<std::vector<elementType>>(name);
			std::vector<STRING> splitRes;
			std::vector<elementType> res;
			Utility::SplitString(str, splitRes, '|');
			for (auto it = splitRes.begin(); it != splitRes.end(); ++it)
			{
				res.push_back(Utility::ToType<elementType>(*it));
			}
			pData->Set(index, std::move(res));
		}
	};

	class SharedVariableCreateHelperMgr
	{
		typedef std::unordered_map<STRING, ISharedVariableCreateHelper*> HelperMapType;
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
				REGISTER_HELPER(AgentWrapper, "_A");
				REGISTER_HELPER(Vector3, "_V");
				REGISTER_HELPER(VecInt, "II");
				REGISTER_HELPER(VecUint64, "UU");
				REGISTER_HELPER(VecBool, "BB");
				REGISTER_HELPER(VecFloat, "FF");
				REGISTER_HELPER(VecString, "SS");
				REGISTER_HELPER(VecAgentWrapper, "AA");
				REGISTER_HELPER(VecVector3, "VV");
			}
		};
		static Constructor cons;

	};

	SharedVariableCreateHelperMgr::Constructor SharedVariableCreateHelperMgr::cons;
	SharedVariableCreateHelperMgr::HelperMapType* SharedVariableCreateHelperMgr::_Helpers;
}

#endif