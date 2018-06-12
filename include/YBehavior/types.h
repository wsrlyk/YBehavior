#ifndef _YBEHAVIOR_TYPES_H_
#define _YBEHAVIOR_TYPES_H_

#include <string>
#include <vector>
#include "define.h"
#include <sstream>

namespace YBehavior
{
	struct YBEHAVIOR_API Vector3
	{
		float x;
		float y;
		float z;

		Vector3()
			: x(0)
			, y(0)
			, z(0)
		{

		}
		Vector3(float _x, float _y, float _z)
			: x(_x)
			, y(_y)
			, z(_z)
		{
		}
		Vector3(const Vector3& other)
			: x(other.x)
			, y(other.y)
			, z(other.z)
		{
		}

		friend std::stringstream & operator<<(std::stringstream &out, const Vector3 &obj)
		{
			out << obj.x << '=' << obj.y << '=' << obj.z;
			return out;
		}

		friend std::stringstream & operator >> (std::stringstream &in, Vector3 &obj)
		{
			char c;
			in >> obj.x >> c >> obj.y >> c >> obj.z;
			return in;
		}

		friend std::ostream & operator<<(std::ostream &out, const Vector3 &obj)
		{
			out << obj.x << '=' << obj.y << '=' << obj.z;
			return out;
		}

		friend std::istream & operator >> (std::istream &in, Vector3 &obj)
		{
			char c;
			in >> obj.x >> c >> obj.y >> c >> obj.z;
			return in;
		}

		bool operator == (const Vector3& other) const
		{
			return x == other.x && y == other.y && z == other.z;
		}

		bool operator != (const Vector3& other) const
		{
			return !((*this) == other);
		}

		Vector3 operator + (const Vector3& other) const
		{
			Vector3 res(x + other.x, y + other.y, z + other.z);
			return res;
		}

		Vector3 operator - (const Vector3& other) const
		{
			Vector3 res(x - other.x, y - other.y, z - other.z);
			return res;
		}

		Vector3 operator * (const Vector3& other) const
		{
			return *this;
		}
		Vector3 operator / (const Vector3& other) const
		{
			return *this;
		}

		bool operator < (const Vector3& other) const
		{
			return true;
		}
		bool operator > (const Vector3& other) const
		{
			return true;
		}
		bool operator <= (const Vector3& other) const
		{
			return true;
		}
		bool operator >= (const Vector3& other) const
		{
			return true;
		}


		const static Vector3 zero;
	};

	class Agent;
	struct YBEHAVIOR_API AgentWrapper
	{
		Agent* m_Data;

		AgentWrapper()
			: m_Data(nullptr)
		{

		}
		AgentWrapper(Agent* pAgent)
			: m_Data(pAgent)
		{

		}
		friend std::stringstream & operator<<(std::stringstream &out, const AgentWrapper &obj)
		{
			return out;
		}

		friend std::stringstream & operator >> (std::stringstream &in, AgentWrapper &obj)
		{
			return in;
		}
	};

	typedef std::string			STRING;
	typedef int					INT;
	typedef unsigned int		UINT;
	typedef unsigned long		UINT64;
	typedef unsigned char       BYTE;
	typedef unsigned short		BOOL;	///> WARNING: short has been used for bool. Cause bool in vector is specialized and has quite different behaviors with others.
	typedef float				FLOAT;
	typedef char				CHAR;

	typedef Agent*				AgentPtr;
	typedef STRING				String;
	typedef INT					Int;
	typedef UINT				Uint;
	typedef UINT64				Uint64;
	typedef BYTE				Byte;
	typedef BOOL				Bool;
	typedef FLOAT				Float;
	typedef std::vector<STRING>	VecString;
	typedef std::vector<INT>	VecInt;
	typedef std::vector<UINT>	VecUint;
	typedef std::vector<UINT64>	VecUint64;
	typedef std::vector<BYTE>	VecByte;
	typedef std::vector<BOOL>	VecBool;
	typedef std::vector<FLOAT>	VecFloat;
	typedef std::vector<AgentWrapper>	VecAgentWrapper;
	typedef std::vector<Vector3>	VecVector3;

#define YBEHAVIOR_BASICTYPE_NUMBER_ID(type, id)			\
	template<> inline int GetClassTypeNumberId<type>() \
	{\
		return id;\
	}\
	template<> inline int GetClassTypeNumberId<const type>() \
	{\
		return id; \
	}

	template<typename T>
	inline int GetClassTypeNumberId() {
		return -1;
	}

	YBEHAVIOR_BASICTYPE_NUMBER_ID(Bool, 1);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Int, 2);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Uint64, 3);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Float, 4);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(String, 5);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(AgentWrapper, 6);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Vector3, 7);

	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecBool, 101);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecInt, 102);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecUint64, 103);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecFloat, 104);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecString, 105);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecAgentWrapper, 106);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecVector3, 107);

	class GlobalDefinitions
	{
	public:
		static const char POINTER = 'P';
		static const char CONST = 'C';
	};
}

#endif