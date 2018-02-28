#ifndef _YBEHAVIOR_SHAREDDATA_H_
#define _YBEHAVIOR_SHAREDDATA_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"

namespace YBehavior
{
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////

	class YBEHAVIOR_API SharedData
	{
#define DECLARE_VARIABLES(T)			std::vector<T>* m_##T##s;	///> 定义变量
#define CONSTRUCT_VARIABLE(T)				m_##T##s = nullptr; 	///> 构造变量
#define DESTRUCT_VARIABLES(T)				if (m_##T##s != nullptr) delete m_##T##s; ///> 析构变量
#define DECLARE_DEFAULT_TYPES(T)		const static T s_Default##T;
#define DEFINE_DEFAULT_TYPES_V(T, V)	const T SharedData::s_Default##T(V);
///> 各种函数
#define DEFINE_TYPES_FUNC(T)\
	const T& Get##T(INT index)\
	{\
		if (m_##T##s == nullptr || index < 0 || index >= m_##T##s->size())\
			return s_Default##T;\
		return (*m_##T##s)[index];\
	}\
	\
	bool Set##T(INT index, const T& v)\
	{\
		if (index < 0)\
			return false;\
		if (m_##T##s == nullptr)\
			m_##T##s = new std::vector<T>(4);\
		if (index >= m_##T##s->size())\
		{\
			m_##T##s->reserve(m_##T##s->size() * 2);\
		}\
		(*m_##T##s)[index] = v;\
		return true;\
	}\
	\
	bool Set##T(INT index, T&& v)\
	{\
		if (index < 0)\
			return false;\
		if (m_##T##s == nullptr)\
			m_##T##s = new std::vector<T>(4);\
		if (index >= m_##T##s->size())\
		{\
			m_##T##s->reserve(m_##T##s->size() * 2);\
		}\
		(*m_##T##s)[index] = v;\
		return true;\
	}
#define FOR_EACH_TYPE(func)	\
	func(Int);	\
	func(Uint64);	\
	func(Bool);	\
	func(Float);	\
	func(String);	\
	func(AgentPtr);	\
	func(Vector3);\
	func(VecInt);\
	func(VecUint64);\
	func(VecBool);\
	func(VecFloat);\
	func(VecString);\
	func(VecAgentPtr);\
	func(VecVector3);
#define FOR_EACH_TYPE_WITH_VALUE(func)	\
	func(Int, 0);	\
	func(Uint64, 0);	\
	func(Bool, false);	\
	func(Float, 0.0f);	\
	func(String, "");	\
	func(AgentPtr, nullptr);	\
	func(Vector3, Vector3::zero);\
	func(VecInt, 1);\
	func(VecUint64, 1);\
	func(VecBool, 1);	\
	func(VecFloat, 1);	\
	func(VecString, 1);	\
	func(VecAgentPtr, 1);	\
	func(VecVector3, 1);	
#define FOR_EACH_SINGLE_NORMAL_TYPE(func)	\
	func(Int);	\
	func(Uint64);	\
	func(Bool);	\
	func(Float);	\
	func(String);	\
	func(Vector3);
#define FOR_EACH_VECTOR_NORMAL_TYPE(func)	\
	func(VecInt);\
	func(VecUint64);\
	func(VecBool);\
	func(VecFloat);\
	func(VecString);\
	func(VecVector3);
#define FOR_EACH_ABNORMAL_TYPE(func)	\
	func(AgentPtr);	\
	func(VecAgentPtr);

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
	protected:
		FOR_EACH_TYPE(DECLARE_VARIABLES);
	public:
		FOR_EACH_TYPE(DECLARE_DEFAULT_TYPES);
		const static int INVALID_INDEX = -1;

	public:
		SharedData()
		{
			FOR_EACH_TYPE(CONSTRUCT_VARIABLE);
		}
		~SharedData()
		{
			FOR_EACH_TYPE(DESTRUCT_VARIABLES);
		}

		FOR_EACH_TYPE(DEFINE_TYPES_FUNC);

		void Clone(const SharedData& other);
	};

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
	class YBEHAVIOR_API ISharedVariable
	{
	public:
		virtual ~ISharedVariable() {}
		inline void SetIndex(INT index) { m_Index = index; }
		virtual ISharedVariable operator+(ISharedVariable& other) { return *this;}
		virtual ISharedVariable& operator=(ISharedVariable& other) { return *this;}
		virtual ISharedVariable& operator=(ISharedVariable&& other) { return *this;}
		virtual void SetValueFromString(const STRING& str) {}

	protected:
		INT m_Index;
	};

	template<typename T>
	class YBEHAVIOR_API SharedVariable: public ISharedVariable
	{
	protected:
		T m_Value;
	public:
		virtual const T& GetValue(SharedData* data) = 0;
		virtual bool SetValue(SharedData* data, const T& v) = 0;
		virtual bool SetValue(SharedData* data, T&& v) = 0;
	};
	
#define DECLARE_SHARED_SINGLE_TYPES(T)\
	class YBEHAVIOR_API Shared##T : public SharedVariable<T>\
	{\
	public:\
		Shared##T();\
		const T& GetValue(SharedData* data);\
		bool SetValue(SharedData* data, const T& v);\
		bool SetValue(SharedData* data, T&& v);\
		void SetValueFromString(const STRING& str)\
		{\
			m_Value = std::move(Utility::ToType<T>(str));\
		}\
	};

#define DECLARE_SHARED_VECTOR_TYPES(T)\
	class YBEHAVIOR_API SharedVec##T : public SharedVariable<Vec##T>\
	{\
	public:\
		SharedVec##T();\
		const Vec##T& GetValue(SharedData* data);\
		bool SetValue(SharedData* data, const Vec##T& v);\
		bool SetValue(SharedData* data, Vec##T&& v);\
		void SetValueFromString(const STRING& str)\
		{\
			m_Value.clear();\
			std::vector<STRING> res;\
			Utility::SplitString(str, res, '|');\
			for (auto it = res.begin(); it != res.end(); ++it)\
			{\
				m_Value.push_back(std::move(Utility::ToType<T>(*it)));\
			}\
		}\
	};

#define DECLARE_SHARED_TYPES_NOT_FROM_STRING(T)\
	class YBEHAVIOR_API Shared##T : public SharedVariable<T>\
	{\
	public:\
	Shared##T();\
	const T& GetValue(SharedData* data);\
	bool SetValue(SharedData* data, const T& v);\
	bool SetValue(SharedData* data, T&& v);\
	};

	FOR_EACH_SINGLE_NORMAL_TYPE(DECLARE_SHARED_SINGLE_TYPES);
	FOR_EACH_SINGLE_NORMAL_TYPE(DECLARE_SHARED_VECTOR_TYPES);
	FOR_EACH_ABNORMAL_TYPE(DECLARE_SHARED_TYPES_NOT_FROM_STRING);
}

#endif