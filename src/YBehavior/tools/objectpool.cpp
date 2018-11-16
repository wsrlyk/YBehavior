#include "YBehavior/tools/objectpool.h"

namespace YBehavior
{
	template<>
	void ObjectPool<EntityWrapper>::Recycle(EntityWrapper* t)
	{
		if (t == nullptr)
			return;
		t->Reset();
		s_Pool.push_back(t);
	}
}
