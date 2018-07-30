#include "YBehavior/types.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	std::stringstream & operator<<(std::stringstream &out, const EntityWrapper &obj)
	{
		if (obj.IsValid() && obj.Get() != nullptr)
			out << obj.Get()->ToString();
		else
			out << "InvalidAgent";
		return out;
	}

	bool EntityWrapper::_CheckValidAndReset()
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

	EntityWrapper::~EntityWrapper()
	{
		//LOG_BEGIN << "Destruct Wrapper" << LOG_END;
		_CheckValidAndReset();
	}

	void EntityWrapper::Reset()
	{
		_CheckValidAndReset();
		m_Data = nullptr;
		m_Reference = nullptr;
	}
}
