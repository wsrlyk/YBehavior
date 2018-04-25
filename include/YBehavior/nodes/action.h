#ifndef _YBEHAVIOR_ACTION_H_
#define _YBEHAVIOR_ACTION_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class YBEHAVIOR_API Action : public LeafNode
	{
	public:
		Action();
		~Action();
	};
}

#endif
