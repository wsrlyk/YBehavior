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
namespace YBehavior
{
	void NodeFactory::SetActiveTree(const STRING& tree)
	{
		LOG_BEGIN << "SetActiveTree: " << tree.c_str() << LOG_END;

		mCurActiveTreeName = tree;

		if (tree.empty())
		{
			mpCurActiveNameKeyInfo = &mCommonNameKeyInfo;
#ifdef DEBUGGER
			mpCurActiveKeyNameMap = &mCommonKeyNameMap;
#endif
			return;
		}
		else
		{
			mpCurActiveNameKeyInfo = &mTempNameKeyInfo;
#ifdef DEBUGGER
			KeyNameMapType newMap;
			mKeyNameMap[tree] = newMap;
			mpCurActiveKeyNameMap = &mKeyNameMap[tree];
#endif

		}

		mpCurActiveNameKeyInfo->Reset();
		mpCurActiveNameKeyInfo->AssignKey(mCommonNameKeyInfo);
	}

#ifdef DEBUGGER
	const STRING& NodeFactory::GetNameByKey(const STRING& treeName, KEY key, TYPEID typeNumberId)
	{
		auto it = mKeyNameMap.find(treeName);
		if (it != mKeyNameMap.end())
		{
			KeyNameMapType& keynamemap = it->second;
			auto it2 = keynamemap.find(typeNumberId << 16 | key);
			if (it2 != keynamemap.end())
				return it2->second;
			return Utility::StringEmpty;
		}

		return Utility::StringEmpty;
	}
#endif

	NodeFactory::NodeFactory()
	{
		mCommonNameKeyInfo.Reset();
		mTempNameKeyInfo.Reset();
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
