#include "YBehavior/nodefactory.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodes/calculator.h"
#include "YBehavior/nodes/sequence.h"
#include "YBehavior/nodes/comparer.h"
#include "YBehavior/nodes/ifthenelse.h"
#include "YBehavior/nodes/decorator.h"
#include "YBehavior/nodes/selector.h"
#include "YBehavior/nodes/setdata.h"
#include "YBehavior/nodes/random.h"
#include "YBehavior/nodes/register.h"
#include "YBehavior/nodes/switchcase.h"
#include "YBehavior/nodes/loop.h"
namespace YBehavior
{
	void NodeFactory::SetActiveTree(NameKeyMgr* nameKeyMgr, bool bReset)
	{
		//LOG_BEGIN << "SetActiveTree: " << tree.c_str() << LOG_END;

		if (nameKeyMgr == nullptr)
		{
			mpCurActiveNameKeyInfo = &mCommonNameKeyInfo;
			return;
		}
		else
		{
			mpCurActiveNameKeyInfo = nameKeyMgr;
		}

		if (bReset)
		{
			mpCurActiveNameKeyInfo->Reset();
			mpCurActiveNameKeyInfo->AssignKey(mCommonNameKeyInfo);
		}
	}

	KEY NodeFactory::GetKeyByName(const STRING& name, TYPEID typeID)
	{
		auto info = mCommonNameKeyInfo.Get(typeID);
		KEY key = info.Get(name);
		if (key != INVALID_KEY)
			return key;
		if (mpCurActiveNameKeyInfo == NULL)
			return INVALID_KEY;
		info = mpCurActiveNameKeyInfo->Get(typeID);
		return info.Get(name);
	}

	const STRING& NodeFactory::GetNameByKey(KEY key, TYPEID typeID)
	{
		auto info = mCommonNameKeyInfo.Get(typeID);
		const STRING& name = info.Get(key);
		if (name != Utility::StringEmpty)
			return name;
		if (mpCurActiveNameKeyInfo == NULL)
			return Utility::StringEmpty;
		info = mpCurActiveNameKeyInfo->Get(typeID);
		return info.Get(key);
	}

	NodeFactory::NodeFactory()
	{
		mCommonNameKeyInfo.Reset();
	}

	NodeFactory* CreateNodeFactory()
	{
		NodeFactory* factory = new NodeFactory();
		REGISTER_TYPE(factory, Sequence);
		REGISTER_TYPE(factory, Selector);
		REGISTER_TYPE(factory, RandomSequence);
		REGISTER_TYPE(factory, RandomSelector);
		REGISTER_TYPE(factory, Calculator);
		REGISTER_TYPE(factory, Comparer);
		REGISTER_TYPE(factory, IfThenElse);
		REGISTER_TYPE(factory, AlwaysSuccess);
		REGISTER_TYPE(factory, AlwaysFailure);
		REGISTER_TYPE(factory, Invertor);
		REGISTER_TYPE(factory, SetData);
		REGISTER_TYPE(factory, Random);
		REGISTER_TYPE(factory, ReadRegister);
		REGISTER_TYPE(factory, WriteRegister);
		REGISTER_TYPE(factory, SwitchCase);
		REGISTER_TYPE(factory, For);
		REGISTER_TYPE(factory, ForEach);

		return factory;
	}

	NodeFactory* NodeFactory::Instance()
	{
		if (s_NodeFactory == nullptr)
			s_NodeFactory = CreateNodeFactory();
		return s_NodeFactory;
	}

	NodeFactory* NodeFactory::s_NodeFactory = nullptr;
}
