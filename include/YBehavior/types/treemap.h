#ifndef _YBEHAVIOR_TREEMAP_H_
#define _YBEHAVIOR_TREEMAP_H_

#include "YBehavior/types/types.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	struct TreeMap
	{
		typedef std::tuple<NodePtr, STRING> NodeDesc;

		struct key_hash
		{
			std::size_t operator()(const NodeDesc& k) const
			{
				return std::hash<NodePtr>{}(std::get<0>(k)) ^ std::hash<STRING>{}(std::get<1>(k));
			}
		};

		struct key_equal
		{
			bool operator()(const NodeDesc& v0, const NodeDesc& v1) const
			{
				return (
					std::get<0>(v0) == std::get<0>(v1) &&
					std::get<1>(v0) == std::get<1>(v1)
					);
			}
		};

		small_map<NodePtr, STRING> Node2Trees;
		small_map<NodeDesc, STRING> Name2Trees;
	};
}

#endif
