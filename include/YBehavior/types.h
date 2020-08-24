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

	struct YBEHAVIOR_API Vector3
	{
		float x;
		float y;
		float z;

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
			Vector3 res = { x + other.x, y + other.y, z + other.z };
			return res;
		}

		Vector3 operator - (const Vector3& other) const
		{
			Vector3 res = { x - other.x, y - other.y, z - other.z };
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
	typedef const char*			CSTRING;
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


	YBEHAVIOR_BASICTYPE_NUMBER_ID(Bool, 1);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Int, 2);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Ulong, 3);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Float, 4);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(String, 5);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(EntityWrapper, 6);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(Vector3, 7);

	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecBool, 101);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecInt, 102);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecUlong, 103);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecFloat, 104);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecString, 105);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecEntityWrapper, 106);
	YBEHAVIOR_BASICTYPE_NUMBER_ID(VecVector3, 107);

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

	YBEHAVIOR_BASICTYPE_STORE_KEY(Int, 0);
	YBEHAVIOR_BASICTYPE_STORE_KEY(Ulong, 1);
	YBEHAVIOR_BASICTYPE_STORE_KEY(Bool, 2);
	YBEHAVIOR_BASICTYPE_STORE_KEY(Float, 3);
	YBEHAVIOR_BASICTYPE_STORE_KEY(String, 4);
	YBEHAVIOR_BASICTYPE_STORE_KEY(EntityWrapper, 5);
	YBEHAVIOR_BASICTYPE_STORE_KEY(Vector3, 6);

	YBEHAVIOR_BASICTYPE_STORE_KEY(VecInt, 7);
	YBEHAVIOR_BASICTYPE_STORE_KEY(VecUlong, 8);
	YBEHAVIOR_BASICTYPE_STORE_KEY(VecBool, 9);
	YBEHAVIOR_BASICTYPE_STORE_KEY(VecFloat, 10);
	YBEHAVIOR_BASICTYPE_STORE_KEY(VecString, 11);
	YBEHAVIOR_BASICTYPE_STORE_KEY(VecEntityWrapper, 12);
	YBEHAVIOR_BASICTYPE_STORE_KEY(VecVector3, 13);

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