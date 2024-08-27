#ifdef YSHARP

#include "YBehavior/sharp/sharpagent.h"
#include "YBehavior/utility.h"

namespace YBehavior
{

	SharpEntity::SharpEntity(UINT64 uid, int index)
	: Entity()
	, m_UID(uid)
	, m_Index(index)
	{

	}

	STRING SharpEntity::ToString() const
	{
		return Utility::ToString(m_UID % 1000);
	}

	SharpAgent::SharpAgent(SharpEntity* entity, int index)
	: Agent(entity)
	, m_Index(index)
	{

	}

}

#endif