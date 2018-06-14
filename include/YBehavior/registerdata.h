#ifndef _YBEHAVIOR_REGISTERDATA_H_
#define _YBEHAVIOR_REGISTERDATA_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
namespace YBehavior
{
	class YBEHAVIOR_API RegisterData
	{
		VecInt m_VecInt;
		VecFloat m_VecFloat;
		VecString m_VecString;
		VecUlong m_VecUlong;
		VecBool m_VecBool;

		String m_Event;

		bool m_bDirty;
	public:
		RegisterData();
		void Clear();
		inline VecInt* GetInt() { return &m_VecInt; }
		inline VecFloat* GetFloat() { return &m_VecFloat; }
		inline VecString* GetString() { return &m_VecString; }
		inline VecUlong* GetUlong() { return &m_VecUlong; }
		inline VecBool* GetBool() { return &m_VecBool; }
		inline String* GetEvent() { return &m_Event; }
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
	};
}

#endif