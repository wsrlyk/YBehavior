#ifndef _YBEHAVIOR_NODEFACTORY_H_
#define _YBEHAVIOR_NODEFACTORY_H_

#include "YBehavior/factory.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"

namespace YBehavior
{
	class YBEHAVIOR_API NodeFactory: public Factory<BehaviorNode>
	{
	protected:
		static NodeFactory* s_NodeFactory;
	public:
		static NodeFactory* Instance();
	};
}

#endif