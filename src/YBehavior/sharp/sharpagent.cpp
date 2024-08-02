#ifdef YSHARP

#include "YBehavior/sharp/sharpagent.h"
#include "YBehavior/utility.h"

namespace YBehavior
{

	SharpEntity::SharpEntity(UINT64 uid)
	: Entity()
	, m_UID(uid)
	{

	}

	STRING SharpEntity::ToString() const
	{
		return Utility::ToString(m_UID % 1000);
	}

	SharpAgent::SharpAgent(SharpEntity* entity)
	: Agent(entity)
	{

	}

}

#endif