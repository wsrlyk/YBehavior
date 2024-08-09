#include "YBehavior/utility.h"
#include <vector>
#include "YBehavior/types/types.h"
#include "YBehavior/variable.h"
#include <time.h>
#include <stdio.h>

namespace YBehavior
{

	const INT Utility::IntEmpty(0);
	const FLOAT Utility::FloatEmpty(0.0f);
	const BOOL Utility::BoolEmpty(Utility::FALSE_VALUE);
	const ULONG Utility::UlongEmpty(0);
	const STRING Utility::StringEmpty("");
	const EntityWrapper Utility::EntityWrapperEmpty;
	const Vector3 Utility::Vector3Empty;
	const VecInt Utility::VecIntEmpty;
	const VecFloat Utility::VecFloatEmpty;
	const VecBool Utility::VecBoolEmpty;
	const VecString Utility::VecStringEmpty;
	const VecUlong Utility::VecUlongEmpty;
	const VecVector3 Utility::VecVector3Empty;
	const VecEntityWrapper Utility::VecEntityWrapperEmpty;

	const char Utility::POINTER_CHAR = 'P';
	const char Utility::CONST_CHAR = 'C';
	const BOOL Utility::TRUE_VALUE = 1;
	const BOOL Utility::FALSE_VALUE = 0;
	const KEY Utility::INVALID_KEY = -1;
	const YBehavior::TYPEID Utility::INVALID_TYPE = -1;
	const YBehavior::TYPEID Utility::TYPE_NUM = 7;

	std::random_device Utility::rd;
	std::default_random_engine Utility::mt(rd());

	void Utility::SplitString(const STRING& s, StdVector<STRING>& output, CHAR c, bool RemoveEmptyEntries, int count)
	{
		int counter = 0;
		output.clear();
		STRING::size_type pos1, pos2;
		pos2 = s.find(c);
		pos1 = 0;
		while(STRING::npos != pos2)
		{
			if (!RemoveEmptyEntries || pos2 > pos1)
			{
				output.push_back(s.substr(pos1, pos2 - pos1));
				++counter;
			}

			pos1 = pos2 + 1;

			if (count > 0 && counter >= count)
				break;

			pos2 = s.find(c, pos1);
		}
		if(pos1 != s.length())
			output.push_back(s.substr(pos1));
	}


	void Utility::CreateVector3(const StdVector<STRING>& data, Vector3& vector3)
	{
		if (data.size() < 3)
		{
			if (data.size() > 0)
				vector3.x = static_cast<float>(strtod(data[0].c_str(), 0));
			else
				vector3.x = 0;
			if (data.size() > 1)
				vector3.y = static_cast<float>(strtod(data[1].c_str(), 0));
			else
				vector3.y = 0;
			if (data.size() > 2)
				vector3.z = static_cast<float>(strtod(data[2].c_str(), 0));
			else
				vector3.z = 0;
			return;
		}
		vector3.x = static_cast<float>(strtod(data[0].c_str(), 0));
		vector3.y = static_cast<float>(strtod(data[1].c_str(), 0));
		vector3.z = static_cast<float>(strtod(data[2].c_str(), 0));
	}

	YBehavior::Vector3 Utility::CreateVector3(const StdVector<STRING>& data)
	{
		Vector3 vec;
		CreateVector3(data, vec);
		return vec;
	}

	bool Utility::IsElement(TYPEID eleType, TYPEID vectorType)
	{
		return (vectorType - TYPE_NUM) == eleType;
	}

	char Utility::ToLower(char c)
	{
		if ('A' <= c && c <= 'Z')
			return c - 'A' + 'a';
		return c;
	}

	char Utility::ToUpper(char c)
	{
		if ('a' <= c && c <= 'z')
			return c + 'A' - 'a';
		return c;
	}

	bool Utility::IsLower(char c)
	{
		return 'a' <= c && c <= 'z';
	}

	template<>
	BOOL Utility::ToType(const STRING& str)
	{
		return str == "T" ? 1 : 0;
		//bool t;
		//std::stringstream ss;
		//ss << str;
		//ss >> std::boolalpha >> t;
		//return t ? 1 : 0;
	}

	template<>
	STRING Utility::ToString(const BOOL& t)
	{
		return t > 0 ? "T" : "F";
		//STRING str;
		//std::stringstream ss;
		//ss << std::boolalpha << (t > 0);
		//ss >> str;
		//return str;
	}

	template<>
	STRING Utility::ToString(const StdVector<BOOL>& t)
	{
		STRING str;
		std::stringstream ss;
		for (unsigned i = 0; i < t.size(); ++i)
		{
			if (i != 0)
				ss << '|';
			ss << ToString(t[i]);
		}
		ss >> str;
		return str;
	}

	UINT Utility::Hash(const STRING& str)
	{
		UINT len = (UINT)str.length();
		UINT hash = 0;
		for (UINT i = 0; i < len; ++i)
		{
			hash = (hash << 5) + hash + (UINT)str[i];
		}
		return hash;
	}

	YBehavior::STRING Utility::GetNameFromPath(const STRING& path)
	{
		STRING res;
		auto it = path.find_last_of('/');
		if (it != STRING::npos)
			res = path.substr(it + 1);
		else
			res = path;
		it = res.find_last_of('\\');
		if (it != STRING::npos)
			res = res.substr(it + 1);
		return res;
	}


	const YBehavior::STRING Utility::TIME_FORMAT_DAY("%Y-%m-%d");
	const YBehavior::STRING Utility::TIME_FORMAT_SECOND("%Y-%m-%d_%H:%M:%S");
	STRING Utility::GetTime(const STRING& format)
	{
		time_t tNow = time(NULL);
		struct tm t;
#ifdef YB_MSVC
		localtime_s(&t, &tNow);
#else
		localtime_r(&tNow, &t);
#endif
		char buffer[40] = { 0 };
		
		strftime(buffer, sizeof(buffer), format.c_str(), &t);
		return std::string(buffer);
	}

	UINT Utility::GetMicroDuration(const Utility::TimePointType& start, const Utility::TimePointType& end)
	{
		auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
		return (UINT)duration.count();
		//return (UINT)(duration.count() * std::chrono::microseconds::period::num / std::chrono::microseconds::period::den);
	}


	template<>
	Float Utility::Rand<Float>(const Float& small, const Float& large)
	{
		std::uniform_real_distribution<Float> dist(small, large);
		return dist(mt);
	}

	template<>
	Bool Utility::Rand<Bool>(const Bool& small, const Bool& large)
	{
		std::uniform_int_distribution<Int> dist(small, large);
		return dist(mt);
	}

#define DEFAULT_DEFINE(type)\
	template<>\
	bool Utility::SetDefault(type& t)\
	{\
		t = Utility::type##Empty;\
		return true;\
	}

	FOR_EACH_SINGLE_NORMAL_TYPE(DEFAULT_DEFINE)

#define TYPES_DEFINE_CREATE_STR_FUNC(type, str)\
	template<>\
	const STRING& Utility::GetCreateStr<type>() \
	{\
		static const STRING type##CreateStr(str);\
		return type##CreateStr;\
	}
	
	TYPES_DEFINE_CREATE_STR_FUNC(Int, "_I");
	TYPES_DEFINE_CREATE_STR_FUNC(Ulong, "_U");
	TYPES_DEFINE_CREATE_STR_FUNC(Bool, "_B");
	TYPES_DEFINE_CREATE_STR_FUNC(Float, "_F");
	TYPES_DEFINE_CREATE_STR_FUNC(String, "_S");
	TYPES_DEFINE_CREATE_STR_FUNC(EntityWrapper, "_A");
	TYPES_DEFINE_CREATE_STR_FUNC(Vector3, "_V");
	TYPES_DEFINE_CREATE_STR_FUNC(VecInt, "II");
	TYPES_DEFINE_CREATE_STR_FUNC(VecUlong, "UU");
	TYPES_DEFINE_CREATE_STR_FUNC(VecBool, "BB");
	TYPES_DEFINE_CREATE_STR_FUNC(VecFloat, "FF");
	TYPES_DEFINE_CREATE_STR_FUNC(VecString, "SS");
	TYPES_DEFINE_CREATE_STR_FUNC(VecEntityWrapper, "AA");
	TYPES_DEFINE_CREATE_STR_FUNC(VecVector3, "VV");

}
