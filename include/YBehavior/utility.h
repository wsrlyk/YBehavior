#ifndef _YBEHAVIOR_UTILITY_H_
#define _YBEHAVIOR_UTILITY_H_

#include "types.h"
#include <vector>

namespace YBehavior
{
	class Utility
	{
	public:
		static const char SequenceSpliter = '=';
		static const char ListSpliter = '|';
		static void SplitString(const STRING& s, std::vector<STRING>& v, CHAR c);
		static Vector3 CreateVector3(const std::vector<STRING>& data);
		static void CreateVector3(const std::vector<STRING>& data, Vector3& vector3);
	};
}

#endif