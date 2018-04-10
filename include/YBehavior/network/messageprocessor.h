#ifndef _YBEHAVIOR_MESSAGEPROCESSOR_H_
#define _YBEHAVIOR_MESSAGEPROCESSOR_H_

#include "YBehavior/types.h"
#include "YBehavior/singleton.h"

namespace YBehavior
{
	class MessageProcessor : public Singleton<MessageProcessor>
	{
	public:
		void ProcessOne(const STRING& s);
	};
}

#endif