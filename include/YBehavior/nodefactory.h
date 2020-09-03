#ifndef _YBEHAVIOR_NODEFACTORY_H_
#define _YBEHAVIOR_NODEFACTORY_H_

#include "YBehavior/factory.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"

#ifdef SHARP
#include "YBehavior/sharp/sharpnode.h"
#endif

namespace YBehavior
{
#ifdef SHARP
	struct SharpCallbacks
	{
		SharpCallbacks()
		{
			onload = nullptr;
			onupdate = nullptr;
		}
		OnSharpNodeLoadedDelegate onload;
		OnSharpNodeUpdateDelegate onupdate;
	};
#endif

	class NodeFactory: public Factory<BehaviorNode>
	{
	protected:
		static NodeFactory* s_NodeFactory;

#ifdef SHARP
		std::unordered_map<STRING, SharpCallbacks> m_SharpCallbacks;
#endif
	public:
#ifdef SHARP
		BehaviorNode* Get(const STRING& name) override;
		void SetSharpCallback(const STRING& name, OnSharpNodeLoadedDelegate onload, OnSharpNodeUpdateDelegate onupdate);
#endif // SHARP

		static NodeFactory* Instance();
	};
}

#endif