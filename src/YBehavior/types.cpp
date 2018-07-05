#include "YBehavior/types.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	std::stringstream & operator<<(std::stringstream &out, const AgentWrapper &obj)
	{
		if (obj.IsValid() && obj.Get() != nullptr)
			out << obj.Get()->ToString();
		else
			out << "InvalidAgent";
		return out;
	}

	bool AgentWrapper::_CheckValidAndReset()
	{
		if (IsValid() && m_Data != nullptr && m_IsValid.use_count() == 2)
		{
			m_IsValid.reset();
			m_Data->DeleteWrapper(m_Reference);
			return true;
		}
		else
		{
			m_IsValid.reset();
			return false;
		}
	}

	AgentWrapper::~AgentWrapper()
	{
		//LOG_BEGIN << "Destruct Wrapper" << LOG_END;
		_CheckValidAndReset();
	}

	void AgentWrapper::Reset()
	{
		_CheckValidAndReset();
		m_Data = nullptr;
		m_Reference = nullptr;
	}
}
