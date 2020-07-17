#ifndef _YBEHAVIOR_MESSAGEPROCESSOR_H_
#define _YBEHAVIOR_MESSAGEPROCESSOR_H_

#ifdef YDEBUGGER
#include "YBehavior/types.h"
#include "YBehavior/singleton.h"

namespace YBehavior
{
	class MessageProcessor : public Singleton<MessageProcessor>
	{
	public:
		void ProcessOne(const STRING& s);
		void OnNetworkClosed();
	};
}

#endif
#endif