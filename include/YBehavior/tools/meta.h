#ifndef _YBEHAVIOR_META_H_
#define _YBEHAVIOR_META_H_

#include "YBehavior/types.h"

namespace YBehavior
{
	template<typename T>
	struct IsVector
	{
		const static bool Result = false;
		typedef T ElementType;
	};
	template<typename T>
	struct IsVector<std::vector<T>>
	{
		const static bool Result = true;
		typedef T ElementType;
	};

	template<typename T>
	struct CanFromString
	{
		const static bool Result = true;
	};
	template<>
	struct CanFromString<EntityWrapper>
	{
		const static bool Result = false;
	};

}

#endif