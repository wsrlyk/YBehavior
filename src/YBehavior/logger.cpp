#include "YBehavior/logger.h"
#include <iostream>

namespace YBehavior
{

	LogMgr::LogMgr()
	{
		m_LogProcessor = nullptr;
		m_ErrorProcessor = nullptr;
	}

	void LogMgr::SetProcessor(LogProcessDelegate* pLog, ErrorProcessDelegate* pError)
	{
		m_LogProcessor = pLog;
		m_ErrorProcessor = pError;
	}

	void LogMgr::LogEnd()
	{
		std::string res(m_Stream.str());
		m_Stream.str("");

		if (m_LogProcessor)
		{
			(*m_LogProcessor)(res);
		}
		else
		{
			std::cout << res << std::endl;
		}
	}

	void LogMgr::ErrorEnd()
	{
		std::string res(m_Stream.str());
		m_Stream.str("");

		if (m_ErrorProcessor)
		{
			(*m_ErrorProcessor)(res);
		}
		else
		{
			std::cout << res << std::endl;
		}
	}

	std::ostream& ProcessLogEnd(std::ostream& ss)
	{
		LogMgr::Instance()->LogEnd();
		return ss;
	}

	std::ostream& ProcessErrorEnd(std::ostream& ss)
	{
		LogMgr::Instance()->ErrorEnd();
		return ss;
	}
}
