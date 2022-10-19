#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	const Vector3 Vector3::zero = {};

	std::stringstream & operator<<(std::stringstream &out, const EntityWrapper &obj)
	{
		if (obj.IsValid() && obj.Get() != nullptr)
			out << obj.Get()->ToString();
		else
			out << "InvalidEntity";
		return out;
	}

	EntityWrapper::~EntityWrapper()
	{
		Reset();
	}

	void EntityWrapper::Reset()
	{
		m_IsValid.reset();
		m_Data = nullptr;
	}
}
