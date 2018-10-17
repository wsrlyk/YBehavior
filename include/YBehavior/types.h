#ifndef _YBEHAVIOR_TYPES_H_
#define _YBEHAVIOR_TYPES_H_

#include <string>
#include <vector>
#include "define.h"
#include <sstream>
#include <memory>
#include "YBehavior/tools/linkedlist.h"

namespace YBehavior
{
#define StdVector std::vector

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

	class Entity;
	class Agent;
	struct YBEHAVIOR_API EntityWrapper
	{
	protected:
		Entity* m_Data;
		std::shared_ptr<bool> m_IsValid;
		LinkedListNode<EntityWrapper>* m_Reference;

	protected:
		bool _CheckValidAndReset();
	public:
		EntityWrapper()
			: m_Data(nullptr)
			, m_IsValid(new bool(false))
			, m_Reference(nullptr)
		{
		}
		EntityWrapper(Entity* pEntity)
			: m_Data(pEntity)
			, m_IsValid(new bool(true))
			, m_Reference(nullptr)
		{
		}

		EntityWrapper(const EntityWrapper& other)
			: m_Data(other.m_Data)
			, m_IsValid(other.m_IsValid)
			, m_Reference(other.m_Reference)
		{
			//LOG_BEGIN << "Copy Construct" << LOG_END;
		}

		EntityWrapper& operator = (const EntityWrapper& other)
		{
			if (m_Reference != other.m_Reference)
			{
				_CheckValidAndReset();
			}
			m_Data = other.m_Data;
			m_IsValid = other.m_IsValid;
			m_Reference = other.m_Reference;
			return *this;
		}

		bool operator ==(const EntityWrapper& other) const
		{
			if (!IsValid() || !other.IsValid())
				return false;
			return Get() == other.Get();
		}

		bool operator !=(const EntityWrapper& other) const
		{
			if (!IsValid() || !other.IsValid())
				return true;
			return Get() != other.Get();
		}

		bool operator >(const EntityWrapper& other) const { return false; }
		bool operator <(const EntityWrapper& other) const { return false; }
		bool operator >=(const EntityWrapper& other) const { return false; }
		bool operator <=(const EntityWrapper& other) const { return false; }

		~EntityWrapper();

		void Reset();

		friend std::stringstream & operator<<(std::stringstream &out, const EntityWrapper &obj);

		friend std::stringstream & operator >> (std::stringstream &in, EntityWrapper &obj)
		{
			return in;
		}

		inline Entity* Get() const { return m_Data; }
		inline bool IsValid() const { return *m_IsValid; }
		inline void SetValid(bool b) { *m_IsValid = b; }
		inline void SetReference(LinkedListNode<EntityWrapper>* r) { m_Reference = r; }
	};

	typedef int					KEY;	///> Key to get value from the shareddata
	typedef int					SIZE_KEY;
	typedef int					TYPEID;


	typedef std::string			STRING;
	typedef int					INT;
	typedef unsigned int		UINT;
	typedef unsigned long long	UINT64;
	typedef unsigned long long	ULONG;
	typedef unsigned char       BYTE;
	typedef bool				BOOL_REAL;
	typedef unsigned short		BOOL;	///> WARNING: bool is defined by short. Cause bool in vector is specialized and has quite different behaviors with others.
	typedef float				FLOAT;
	typedef char				CHAR;

	typedef Agent*				AgentPtr;
	typedef STRING				String;
	typedef INT					Int;
	typedef UINT				Uint;
	typedef UINT64				Uint64;
	typedef UINT64				Ulong;
	typedef BYTE				Byte;
	typedef BOOL				Bool;
	typedef FLOAT				Float;
	typedef StdVector<STRING>	VecString;
	typedef StdVector<INT>	VecInt;
	typedef StdVector<UINT>	VecUint;
	typedef StdVector<UINT64>	VecUint64;
	typedef StdVector<ULONG>	VecUlong;
	typedef StdVector<BYTE>	VecByte;
	typedef StdVector<BOOL>	VecBool;
	typedef StdVector<FLOAT>	VecFloat;
	typedef StdVector<EntityWrapper>	VecEntityWrapper;
	typedef StdVector<Vector3>	VecVector3;

#define YBEHAVIOR_BASICTYPE_NUMBER_ID(type, id)			\
	template<> inline TYPEID GetClassTypeNumberId<type>() \
	{\
		return id;\
	}\
	template<> inline TYPEID GetClassTypeNumberId<const type>() \
	{\
		return id; \
	}

	template<typename T>
	inline TYPEID GetClassTypeNumberId() {
		return -1;
	}

	YBEHAVIOR_BASICTYPE_NUMBER_ID(Bool, 1);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Int, 2);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Uint64, 3);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Float, 4);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(String, 5);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(EntityWrapper, 6);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Vector3, 7);

	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecBool, 101);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecInt, 102);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecUint64, 103);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecFloat, 104);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecString, 105);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecEntityWrapper, 106);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecVector3, 107);
}

namespace YB = YBehavior;

#endif