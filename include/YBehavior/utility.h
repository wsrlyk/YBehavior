#ifndef _YBEHAVIOR_UTILITY_H_
#define _YBEHAVIOR_UTILITY_H_

#include "YBehavior/types.h"
#include <vector>
#include <sstream>

namespace YBehavior
{
	class Utility
	{
	public:
		static const char SequenceSpliter = '=';
		static const char ListSpliter = '|';
		static const char SpaceSpliter = ' ';
		static const STRING StringEmpty;

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

		template<typename T>
		static STRING ToString(const T& t);
		template<typename T>
		static STRING ToString(const std::vector<T>& t);

	};

	template<typename T>
	static STRING Utility::ToString(const T& t)
	{
		STRING str;
		std::stringstream ss;
		ss << t;
		ss >> str;
		return str;
	}

	template<typename T>
	static STRING Utility::ToString(const std::vector<T>& t)
	{
		STRING str;
		std::stringstream ss;
		for (int i = 0; i < t.size(); ++i)
		{
			if (i != 0)
				ss << '|';
			ss << t[i];
		}
		ss >> str;
		return str;
	}

}

#endif