#ifndef _YBEHAVIOR_TYPES_H_
#define _YBEHAVIOR_TYPES_H_

#include <string>
#include <vector>
#include "define.h"
#include <sstream>
#include <memory>
#include <unordered_map>

namespace YBehavior
{
#define StdVector std::vector

	struct Vector3
	{
		float x{};
		float y{};
		float z{};

		friend std::stringstream & operator<<(std::stringstream &outstream, const Vector3 &obj)
		{
			outstream << obj.x << ',' << obj.y << ',' << obj.z;
			return outstream;
		}

		friend std::stringstream & operator >> (std::stringstream &instream, Vector3 &obj)
		{
			char c;
			instream >> obj.x >> c >> obj.y >> c >> obj.z;
			return instream;
		}

		friend std::ostream & operator<<(std::ostream &outstream, const Vector3 &obj)
		{
			outstream << obj.x << ',' << obj.y << ',' << obj.z;
			return outstream;
		}

		friend std::istream & operator >> (std::istream &instream, Vector3 &obj)
		{
			char c;
			instream >> obj.x >> c >> obj.y >> c >> obj.z;
			return instream;
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
			Vector3 res;
			res.x = x + other.x;
			res.y = y + other.y;
			res.z = z + other.z;
			return res;
		}

		Vector3 operator - (const Vector3& other) const
		{
			Vector3 res;
			res.x = x - other.x;
			res.y = y - other.y;
			res.z = z - other.z;
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
	struct EntityWrapper
	{
	protected:
		Entity* m_Data;
		std::shared_ptr<bool> m_IsValid;

	public:
		EntityWrapper()
			: m_Data(nullptr)
			, m_IsValid(new bool(false))
		{
		}
		EntityWrapper(Entity* pEntity)
			: m_Data(pEntity)
			, m_IsValid(new bool(pEntity != nullptr))
		{
		}

		EntityWrapper(const EntityWrapper& other)
			: m_Data(other.m_Data)
			, m_IsValid(other.m_IsValid)
		{
			//LOG_BEGIN << "Copy Construct" << LOG_END;
		}

		EntityWrapper(EntityWrapper&& other)
			: m_Data(other.m_Data)
			, m_IsValid(other.m_IsValid)
		{
			//LOG_BEGIN << "Move Construct" << LOG_END;
			other.m_Data = nullptr;
			other.m_IsValid.reset();
		}

		EntityWrapper& operator = (const EntityWrapper& other)
		{
			m_Data = other.m_Data;
			m_IsValid = other.m_IsValid;
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
	};

	typedef int					KEY;	///> Key to get value from the shareddata
	typedef int					SIZE_KEY;
	typedef int					TYPEID;


	typedef std::string			STRING;
	typedef const char*			CSTRING_CONST;
	typedef char*				CSTRING;
	typedef int					INT;
	typedef unsigned int		UINT;
	typedef unsigned long long	UINT64;
	typedef unsigned long long	ULONG;
	typedef unsigned char       BYTE;
	typedef bool				BOOL_REAL;
	typedef unsigned short		BOOL;	///> WARNING: bool is defined by short. Cause bool in vector is specialized and has quite different behaviors with others.
	typedef unsigned short		USHORT;
	typedef float				FLOAT;
	typedef char				CHAR;

	typedef Agent*				AgentPtr;
	typedef Entity*				EntityPtr;
	typedef STRING				String;
	typedef INT					Int;
	typedef UINT				Uint;
	typedef UINT64				Uint64;
	typedef UINT64				Ulong;
	typedef BYTE				Byte;
	typedef BOOL				Bool;
	typedef USHORT				Ushort;
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
	template<> inline TYPEID GetTypeID<type>() \
	{\
		return id;\
	}\
	template<> inline TYPEID GetTypeID<const type>() \
	{\
		return id; \
	}

	template<typename T>
	inline TYPEID GetTypeID() {
		return -1;
	}

	//////////////////////////////////////////////////////////////////////////
	template<typename T>
	inline KEY GetTypeKey() {
		return -1;
	}

#define YBEHAVIOR_BASICTYPE_STORE_KEY(type, id)			\
	template<> inline KEY GetTypeKey<type>() \
	{\
		return id;\
	}\
	template<> inline TYPEID GetTypeKey<const type>() \
	{\
		return id; \
	}

	///> Make these two numbers the same
#define YBEHAVIOR_BASICTYPE_NUMBER(type, num)\
	YBEHAVIOR_BASICTYPE_NUMBER_ID(type, num);\
	YBEHAVIOR_BASICTYPE_STORE_KEY(type, num);

	YBEHAVIOR_BASICTYPE_NUMBER(Int, 0);
	YBEHAVIOR_BASICTYPE_NUMBER(Ulong, 1);
	YBEHAVIOR_BASICTYPE_NUMBER(Bool, 2);
	YBEHAVIOR_BASICTYPE_NUMBER(Float, 3);
	YBEHAVIOR_BASICTYPE_NUMBER(String, 4);
	YBEHAVIOR_BASICTYPE_NUMBER(EntityWrapper, 5);
	YBEHAVIOR_BASICTYPE_NUMBER(Vector3, 6);

	YBEHAVIOR_BASICTYPE_NUMBER(VecInt, 7);
	YBEHAVIOR_BASICTYPE_NUMBER(VecUlong, 8);
	YBEHAVIOR_BASICTYPE_NUMBER(VecBool, 9);
	YBEHAVIOR_BASICTYPE_NUMBER(VecFloat, 10);
	YBEHAVIOR_BASICTYPE_NUMBER(VecString, 11);
	YBEHAVIOR_BASICTYPE_NUMBER(VecEntityWrapper, 12);
	YBEHAVIOR_BASICTYPE_NUMBER(VecVector3, 13);

	typedef const void* NodePtr;
	struct TreeMap
	{
		typedef std::tuple<NodePtr, STRING> NodeDesc;

		struct key_hash
		{
			std::size_t operator()(const NodeDesc& k) const
			{
				return std::hash<NodePtr>{}(std::get<0>(k)) ^ std::hash<STRING>{}(std::get<1>(k));
			}
		};

		struct key_equal
		{
			bool operator()(const NodeDesc& v0, const NodeDesc& v1) const
			{
				return (
					std::get<0>(v0) == std::get<0>(v1) &&
					std::get<1>(v0) == std::get<1>(v1)
					);
			}
		};

		std::unordered_map<NodePtr, STRING> Node2Trees;
		std::unordered_map<NodeDesc, STRING, key_hash, key_equal> Name2Trees;
	};
}

namespace YB = YBehavior;

#endif