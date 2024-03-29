#ifndef _YBEHAVIOR_NODEFACTORY_H_
#define _YBEHAVIOR_NODEFACTORY_H_

#include "YBehavior/factory.h"
#include "YBehavior/behaviortree.h"

#ifdef YSHARP
#include "YBehavior/sharp/sharpnode.h"
#endif

namespace YBehavior
{
#ifdef YSHARP
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

	class NodeFactory: public Factory<TreeNode>
	{
	protected:
		static NodeFactory* s_NodeFactory;

#ifdef YSHARP
		std::unordered_map<STRING, SharpCallbacks> m_SharpCallbacks;
#endif
	public:
#ifdef YSHARP
		TreeNode* Get(const STRING& name) override;
		void SetSharpCallback(const STRING& name, OnSharpNodeLoadedDelegate onload, OnSharpNodeUpdateDelegate onupdate);
#endif // SHARP

		static NodeFactory* Instance();
	};
}

#endif