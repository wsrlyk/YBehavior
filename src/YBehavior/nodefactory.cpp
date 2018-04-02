#include "YBehavior/nodefactory.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodes/calculator.h"
#include "YBehavior/nodes/sequence.h"
#include "YBehavior/nodes/comparer.h"
#include "YBehavior/nodes/ifthenelse.h"
#include "YBehavior/nodes/decorator.h"
namespace YBehavior
{
	void NodeFactory::SetActiveTree(const STRING& tree)
	{
		LOG_BEGIN << "SetActiveTree: " << tree.c_str() << LOG_END;

		mCurActiveTreeName = tree;

		if (tree.empty())
		{
			mpCurActiveNameIndexInfo = &mCommonNameIndexInfo;
			return;
		}
		else
		{
			mpCurActiveNameIndexInfo = &mTempNameIndexInfo;
		}

		mpCurActiveNameIndexInfo->Reset();
		mpCurActiveNameIndexInfo->AssignIndex(mCommonNameIndexInfo);
	}

	NodeFactory::NodeFactory()
	{
		mCommonNameIndexInfo.Reset();
		mTempNameIndexInfo.Reset();
	}

	NodeFactory* CreateNodeFactory()
	{
		NodeFactory* factory = new NodeFactory();
		REGISTER_TYPE(factory, Sequence);
		REGISTER_TYPE(factory, Calculator);
		REGISTER_TYPE(factory, Comparer);
		REGISTER_TYPE(factory, IfThenElse);
		REGISTER_TYPE(factory, AlwaysSuccess);
		REGISTER_TYPE(factory, AlwaysFailed);

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
