#ifdef YSHARP
#ifndef _SHARPAGENT_H_
#define _SHARPAGENT_H_

#include "YBehavior/agent.h"
#include <unordered_set>
#include "YBehavior/singleton.h"
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

	class SharpUnitMgr : public Singleton<SharpUnitMgr>
	{
		std::unordered_set<SharpAgent*> m_agents;
		std::unordered_set<SharpEntity*> m_entities;

		template<class T, class ... Args>
		T* Create(std::unordered_set<T*>& container, Args &&...args)
		{
			auto o = new T(std::forward<Args>(args)...);
			container.emplace(o);
			return o;
		}

		template<class T>
		void Destroy(T* o, std::unordered_set<T*>& container)
		{
			auto it = container.find(o);
			if (it != container.end())
			{
				delete o;
				container.erase(it);
			}
		}
		template<class T>
		void Clear(std::unordered_set<T*>& container)
		{
			for (auto it : container)
			{
				delete it;
			}
			container.clear();
		}
	public:
		SharpAgent* CreateAgent(SharpEntity* entity)
		{
			return Create(m_agents, entity);
		}
		SharpEntity* CreateEntity(ULONG uid)
		{
			return Create(m_entities, uid);
		}
		void Destroy(SharpAgent* o)
		{
			Destroy(o, m_agents);
		}
		void Destroy(SharpEntity* o)
		{
			Destroy(o, m_entities);
		}

		void Clear()
		{
			Clear(m_agents);
			Clear(m_entities);
		}
	};

}


#endif // _SHARPNODE_H_
#endif