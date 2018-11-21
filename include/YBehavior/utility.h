#ifndef _YBEHAVIOR_UTILITY_H_
#define _YBEHAVIOR_UTILITY_H_

#include "YBehavior/types.h"
#include <vector>
#include <sstream>
#include <random>
namespace YBehavior
{
#define FOR_EACH_SINGLE_NORMAL_TYPE(func)	\
	func(Int);	\
	func(Ulong);	\
	func(Bool);	\
	func(Float);	\
	func(String);	\
	func(Vector3);	\
	func(EntityWrapper);	
#define FOR_EACH_VECTOR_NORMAL_TYPE(func)	\
	func(VecInt);\
	func(VecUlong);\
	func(VecBool);\
	func(VecFloat);\
	func(VecString);\
	func(VecVector3);\
	func(VecEntityWrapper);
#define FOR_EACH_TYPE(func)    \
    func(Int);    \
    func(Ulong);    \
    func(Bool);    \
    func(Float);    \
    func(String);    \
    func(EntityWrapper);    \
    func(Vector3);\
    func(VecInt);\
    func(VecUlong);\
    func(VecBool);\
    func(VecFloat);\
    func(VecString);\
    func(VecEntityWrapper);\
    func(VecVector3);

	class Utility
	{
	public:
		static const char SequenceSpliter = '=';
		static const char ListSpliter = '|';
		static const char SpaceSpliter = ' ';
#define EMPTY_TYPES(T) static const T T##Empty;
		FOR_EACH_TYPE(EMPTY_TYPES)

		static const char POINTER_CHAR;
		static const char CONST_CHAR;
		static const BOOL TRUE_VALUE;
		static const BOOL FALSE_VALUE;
		static const KEY INVALID_KEY;
		static const TYPEID INVALID_TYPE;

		static void SplitString(const STRING& s, StdVector<STRING>& v, CHAR c, int count = 0);
		static Vector3 CreateVector3(const StdVector<STRING>& data);
		static void CreateVector3(const StdVector<STRING>& data, Vector3& vector3);

		static bool IsElement(TYPEID eleType, TYPEID vectorType);
		static char ToLower(char c);
		static char ToUpper(char c);
		static bool IsLower(char c);
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
		//template<>
		//static STRING Utility::ToString(const BOOL& t);
		template<typename T>
		static STRING ToString(const StdVector<T>& t);
		template<typename T>
		static STRING ToStringWithLength(const T& t);
		template<typename T>
		static STRING ToStringWithLength(const StdVector<T>& t);
		//template<>
		//static STRING ToString(const StdVector<BOOL>& t);

		template<typename T>
		static T Rand(const T& smallNum, const T& largeNum);

		static UINT Hash(const STRING& str);

		template<typename T>
		static const T& Default() { return 0; }
	private:
		static std::random_device rd;
		static std::default_random_engine mt;
	};

	template<typename T>
	STRING Utility::ToString(const T& t)
	{
		STRING str;
		std::stringstream ss;
		ss << t;
		ss >> str;
		return str;
	}

	template<>
	BOOL Utility::ToType(const STRING& str);

	template<>
	STRING Utility::ToString(const BOOL& t);

	template<typename T>
	STRING Utility::ToString(const StdVector<T>& t)
	{
		STRING str;
		std::stringstream ss;
		for (unsigned i = 0; i < t.size(); ++i)
		{
			if (i != 0)
				ss << '|';
			ss << t[i];
		}
		ss >> str;
		return str;
	}


	template<>
	STRING Utility::ToString(const StdVector<BOOL>& t);

	template<typename T>
	STRING Utility::ToStringWithLength(const StdVector<T>& t)
	{
		STRING str;
		std::stringstream ss;
		ss << "{";
		for (unsigned i = 0; i < t.size(); ++i)
		{
			if (i != 0)
				ss << '|';
			ss << t[i];
		}
		ss << "}(size=" << t.size() << ")";
		ss >> str;
		return str;
	}

	template<typename T>
	STRING Utility::ToStringWithLength(const T& t)
	{
		return ToString(t);
	}

	template<typename T>
	T Utility::Rand(const T& smallNum, const T& largeNum)
	{
		std::uniform_int_distribution<T> dist(smallNum, largeNum - 1);
		return dist(mt);
	}

	template<>
	Float Utility::Rand<Float>(const Float& smallNum, const Float& largeNum);

	template<>
	Bool Utility::Rand<Bool>(const Bool& smallNum, const Bool& largeNum);


#define DEFAULT_DECLARE(type)\
	template<>\
	static const type& Utility::Default();

	FOR_EACH_TYPE(DEFAULT_DECLARE);
}

#endif
