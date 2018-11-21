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
#include "YBehavior/nodes/piecewisefunction.h"
#include "YBehavior/nodes/dice.h"
#include "YBehavior/nodes/subtree.h"
#include "YBehavior/nodes/wait.h"
#include "YBehavior/nodes/array.h"
namespace YBehavior
{
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
		REGISTER_TYPE(factory, RandomSelect);
		REGISTER_TYPE(factory, ReadRegister);
		REGISTER_TYPE(factory, WriteRegister);
		REGISTER_TYPE(factory, SwitchCase);
		REGISTER_TYPE(factory, For);
		REGISTER_TYPE(factory, ForEach);
		REGISTER_TYPE(factory, Loop);
		REGISTER_TYPE(factory, PiecewiseFunction);
		REGISTER_TYPE(factory, Dice);
		REGISTER_TYPE(factory, SubTree);
		REGISTER_TYPE(factory, Wait);
		REGISTER_TYPE(factory, ClearArray);
		REGISTER_TYPE(factory, GetArrayLength);
		REGISTER_TYPE(factory, ArrayPushElement);
		REGISTER_TYPE(factory, MessUp);

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
