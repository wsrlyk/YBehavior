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
namespace YBehavior
{
	void NodeFactory::SetActiveTree(const STRING& tree)
	{
		LOG_BEGIN << "SetActiveTree: " << tree.c_str() << LOG_END;

		mCurActiveTreeName = tree;

		if (tree.empty())
		{
			mpCurActiveNameIndexInfo = &mCommonNameIndexInfo;
#ifdef DEBUGGER
			mpCurActiveIndexNameMap = &mCommonIndexNameMap;
#endif
			return;
		}
		else
		{
			mpCurActiveNameIndexInfo = &mTempNameIndexInfo;
#ifdef DEBUGGER
			IndexNameMapType newMap;
			mIndexNameMap[tree] = newMap;
			mpCurActiveIndexNameMap = &mIndexNameMap[tree];
#endif

		}

		mpCurActiveNameIndexInfo->Reset();
		mpCurActiveNameIndexInfo->AssignIndex(mCommonNameIndexInfo);
	}

#ifdef DEBUGGER
	const STRING& NodeFactory::GetNameByIndex(const STRING& treeName, INT index, INT typeNumberId)
	{
		auto it = mIndexNameMap.find(treeName);
		if (it != mIndexNameMap.end())
		{
			IndexNameMapType& indexnamemap = it->second;
			auto it2 = indexnamemap.find(typeNumberId << 16 | index);
			if (it2 != indexnamemap.end())
				return it2->second;
			return Utility::StringEmpty;
		}

		return Utility::StringEmpty;
	}
#endif

	NodeFactory::NodeFactory()
	{
		mCommonNameIndexInfo.Reset();
		mTempNameIndexInfo.Reset();
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
		REGISTER_TYPE(factory, AlwaysFailed);
		REGISTER_TYPE(factory, Invertor);
		REGISTER_TYPE(factory, SetData);
		REGISTER_TYPE(factory, Random);

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
