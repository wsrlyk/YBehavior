#ifndef _YBEHAVIOR_TYPES_H_
#define _YBEHAVIOR_TYPES_H_

#include <string>
#include <vector>
#include "define.h"

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

		const static Vector3 zero;
	};

	typedef std::string			STRING;
	typedef int					INT;
	typedef unsigned int		UINT;
	typedef unsigned long		UINT64;
	typedef unsigned char       BYTE;
	typedef bool				BOOL;
	typedef float				FLOAT;
	typedef char				CHAR;

	class Agent;
	typedef Agent* AgentPtr;
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
	typedef std::vector<AgentPtr>	VecAgentPtr;
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
	YBEHAVIOR_BASICTYPE_NUMBER_ID(AgentPtr, 6);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Vector3, 7);

	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecBool, 101);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecInt, 102);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecUint64, 103);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecFloat, 104);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecString, 105);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecAgentPtr, 106);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecVector3, 107);

	typedef CHAR TypeAB;
	class Types
	{
#define DEFINE_TYPEAB(t, c) const static TypeAB t##AB = c;
	public:
		DEFINE_TYPEAB(None, 0);
		DEFINE_TYPEAB(Int, 'I');
		DEFINE_TYPEAB(Float, 'F');
		DEFINE_TYPEAB(Bool, 'B');
	};
}

#endif