#ifndef _YBEHAVIOR_SHAREDVARIABLECREATEHELPER_H_
#define _YBEHAVIOR_SHAREDVARIABLECREATEHELPER_H_

#include "types.h"
#include "nodefactory.h"
#include "shareddata.h"

namespace YBehavior
{
	class ISharedVariableCreateHelper
	{
		TypeAB m_Type;
	public:
		virtual ISharedVariable* CreateVariable() = 0;
		virtual void SetIndex(ISharedVariable* variable, const STRING& s) = 0;
		virtual void SetSharedData(SharedData* pData, const STRING& name, const STRING& str) = 0;
		TypeAB GetType() { return m_Type;}
		void SetType(TypeAB type) { m_Type = type;}
	};

	template<typename valueType, typename variableType>
	class SharedVariableCreateHelper: public ISharedVariableCreateHelper
	{
	public:
		virtual ISharedVariable* CreateVariable() override
		{
			return new variableType();
		}
		virtual void SetIndex(ISharedVariable* variable, const STRING& s) override
		{
			variable->SetIndex(NodeFactory::Instance()->CreateIndexByName<valueType>(s));
		}
		virtual void SetSharedData(SharedData* pData, const STRING& name, const STRING& str) override
		{
			INT index = NodeFactory::Instance()->CreateIndexByName<valueType>(name);
			pData->Set(index, Utility::ToType<valueType>(str));

		}
	};

#define VectorCreateHelper(T)\
	template<>\
	class SharedVariableCreateHelper<T, SharedVec##T> : public ISharedVariableCreateHelper\
	{\
	public:\
		virtual ISharedVariable* CreateVariable() override\
		{\
			return new SharedVec##T();\
		}\
		virtual void SetIndex(ISharedVariable* variable, const STRING& s) override\
		{\
			variable->SetIndex(NodeFactory::Instance()->CreateIndexByName<T>(s));\
		}\
		virtual void SetSharedData(SharedData* pData, const STRING& name, const STRING& str) override\
		{\
			INT index = NodeFactory::Instance()->CreateIndexByName<T>(name);\
			std::vector<STRING> splitRes;\
			Vec##T res;\
			Utility::SplitString(str, splitRes, '|');\
			for (auto it = splitRes.begin(); it != splitRes.end(); ++it)\
			{\
				res.push_back(std::move(Utility::ToType<T>(*it)));\
			}\
			\
			pData->Set(index, std::move(res));\
		}\
	};

FOR_EACH_SINGLE_NORMAL_TYPE(VectorCreateHelper);

}

#endif