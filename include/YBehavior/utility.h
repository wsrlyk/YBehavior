#ifndef _YBEHAVIOR_UTILITY_H_
#define _YBEHAVIOR_UTILITY_H_

#include "YBehavior/types.h"
#include <vector>
#include <sstream>

namespace YBehavior
{
	class ISharedVariableCreateHelper;
	class ISharedVariableEx;
	class Utility
	{
	public:
		static const char SequenceSpliter = '=';
		static const char ListSpliter = '|';
		static const char SpaceSpliter = ' ';
		static void SplitString(const STRING& s, std::vector<STRING>& v, CHAR c, int count = 0);
		static Vector3 CreateVector3(const std::vector<STRING>& data);
		static void CreateVector3(const std::vector<STRING>& data, Vector3& vector3);

		template<typename T>
		static T ToType(const STRING& str)
		{
			T t;
			std::stringstream ss;
			ss << str;
			ss >> t;
			return t;
		}
	};
}

#endif