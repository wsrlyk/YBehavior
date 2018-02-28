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
	};
}

#endif