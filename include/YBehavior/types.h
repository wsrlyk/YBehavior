#ifndef _YBEHAVIOR_TYPES_H_
#define _YBEHAVIOR_TYPES_H_

#include <string>

namespace YBehavior
{
	typedef std::string			STRING;
	typedef int					INT;
	typedef unsigned int		UINT;
	typedef unsigned char       BYTE;
	typedef bool				BOOL;
	typedef float				FLOAT;
	typedef char				CHAR;

	struct Vector3
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
}

#endif