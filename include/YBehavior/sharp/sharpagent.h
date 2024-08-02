#ifdef YSHARP
#ifndef _SHARPAGENT_H_
#define _SHARPAGENT_H_

#include "YBehavior/agent.h"

namespace YBehavior
{
	class SharpEntity : public Entity
	{
		UINT64 m_UID;
	public:
		SharpEntity(UINT64 uid);
		STRING ToString() const override;
		UINT64 GetUID() const { return m_UID;}
	};

	class SharpAgent : public Agent
	{
	public:
		SharpAgent(SharpEntity* entity);

		UINT64 GetDebugUID() const override { return ((SharpEntity*)m_Entity)->GetUID(); }

	};
}


#endif // _SHARPNODE_H_
#endif