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
	struct SharpNodeData
	{
		int indexInSharp{-1};
		bool hasContext{false};
	};
#endif

	class NodeFactory: public Factory<TreeNode>
	{
	protected:
		static NodeFactory* s_NodeFactory;

#ifdef YSHARP
		std::unordered_map<STRING, SharpNodeData> m_SharpNodeDatas;
#endif
	public:
#ifdef YSHARP
		TreeNode* Get(const STRING& name) override;
		void RegisterSharpNode(const STRING& name, int index, bool hasContext);
		void ClearSharpNodes();
#endif // SHARP

		static NodeFactory* Instance();
	};
}

#endif