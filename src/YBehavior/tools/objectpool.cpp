#include "YBehavior/tools/objectpool.h"

namespace YBehavior
{
	template<>
	void ObjectPool<EntityWrapper>::Return(EntityWrapper* t)
	{
		if (t == nullptr)
			return;
		t->Reset();
		m_Pool.push_back(t);
	}
}
