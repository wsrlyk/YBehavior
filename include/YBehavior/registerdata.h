#ifndef _YBEHAVIOR_REGISTERDATA_H_
#define _YBEHAVIOR_REGISTERDATA_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
namespace YBehavior
{
#define FOR_EACH_REGISTER_TYPE(func)    \
		func(Int);    \
		func(Float);    \
		func(String);    \
		func(Bool);    \
		func(Ulong);   \
		func(EntityWrapper);   \
		func(Vector3);

	class YBEHAVIOR_API RegisterData
	{
		struct Datas
		{
#define DATA_VARIABLE_DEFINE(TYPE) const Vec##TYPE* pVec##TYPE;
			///> const VecInt* pVecInt;
			FOR_EACH_REGISTER_TYPE(DATA_VARIABLE_DEFINE);

			const String* pEvent;

			void Clear()
			{
#define DATA_VARIABLE_CLEAR(TYPE) pVec##TYPE = nullptr;
				///> pVecInt = nullptr;
				FOR_EACH_REGISTER_TYPE(DATA_VARIABLE_CLEAR);
				pEvent = nullptr;
			}
		};

#define RECEIVE_DEFINE(TYPE) Vec##TYPE m_Vec##TYPE;
		///> VecInt m_VecInt;
		FOR_EACH_REGISTER_TYPE(RECEIVE_DEFINE);

		String m_Event;

		Datas m_ReceiveData;
		Datas m_SendData;

		bool m_bDirty;
	public:
		RegisterData();
		void Clear();
		inline const Datas& GetReceiveData() { return m_ReceiveData; }
		inline Datas& GetSendData() { return m_SendData; }

		inline void SetEvent(const String& s)
		{			
			m_Event = s;
			m_bDirty = true;
		}
		inline void SetEvent(String&& s)
		{
			m_Event = s;
			m_bDirty = true;
		}
		inline bool IsDirty() { return m_bDirty; }
		
#define PUSH_FUNCTION(TYPE)\
		void Push(const TYPE& data)\
		{\
			m_Vec##TYPE.push_back(data);\
		}\
		void Assign(const StdVector<TYPE>& data)\
		{\
			m_Vec##TYPE.assign(data.begin(), data.end()); \
		}

		FOR_EACH_REGISTER_TYPE(PUSH_FUNCTION);

	};
}

#endif