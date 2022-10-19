#include "YBehavior/logger.h"
#include <iostream>
#include "stdarg.h"

namespace YBehavior
{

	LogMgr::LogMgr()
	{
		m_LogProcessor = nullptr;
		m_ErrorProcessor = nullptr;
		m_LogThreadProcessor = nullptr;
		m_ErrorThreadProcessor = nullptr;
	}

	void LogMgr::SetProcessor(LogProcessDelegate pLog, ErrorProcessDelegate pError)
	{
		m_LogProcessor = pLog;
		m_ErrorProcessor = pError;
	}

	void LogMgr::SetProcessorThread(LogProcessDelegate pLog, ErrorProcessDelegate pError)
	{
		m_LogThreadProcessor = pLog;
		m_ErrorThreadProcessor = pError;
	}

	void LogMgr::LogEnd(bool bThread)
	{
		std::string res(m_Stream.str());
		m_Stream.str("");

		if (bThread)
		{
			if (m_LogThreadProcessor)
			{
				(*m_LogThreadProcessor)(res);
				return;
			}
		}
		else
		{
			if (m_LogProcessor)
			{
				(*m_LogProcessor)(res);
				return;
			}
		}
		
		{
			std::cout << res << std::endl;
		}
	}

	void LogMgr::ErrorEnd(bool bThread)
	{
		std::string res(m_Stream.str());
		m_Stream.str("");

		if (bThread)
		{
			if (m_ErrorThreadProcessor)
			{
				(*m_ErrorThreadProcessor)(res);
				return;
			}
		}
		else
		{
			if (m_ErrorProcessor)
			{
				(*m_ErrorProcessor)(res);
				return;
			}
		}

		{
			std::cout << res << std::endl;
		}
	}

	void LogMgr::Log(const char* fmt, ...)
	{
		va_list ap;
		va_start(ap, fmt);
		_Format(fmt, ap);
		va_end(ap);

		LogEnd();
	}

	void LogMgr::Error(const char* fmt, ...)
	{
		va_list ap;
		va_start(ap, fmt);
		_Format(fmt, ap);
		va_end(ap);

		ErrorEnd();
	}

	void LogMgr::LogThread(const char* fmt, ...)
	{
		va_list ap;
		va_start(ap, fmt);
		_Format(fmt, ap);
		va_end(ap);

		LogEnd(true);
	}

	void LogMgr::ErrorThread(const char* fmt, ...)
	{
		va_list ap;
		va_start(ap, fmt);
		_Format(fmt, ap);
		va_end(ap);

		ErrorEnd(true);
	}

	void LogMgr::_Format(const char* fmt, va_list ap)
	{
		int len = vsnprintf(m_Buffer, sizeof(m_Buffer), fmt, ap);

		if (len <= 0)
			return;
		std::string s(m_Buffer, len);
		m_Stream << s;
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

	std::ostream& ProcessLogThreadEnd(std::ostream& ss)
	{
		LogMgr::Instance()->LogEnd(true);
		return ss;
	}

	std::ostream& ProcessErrorThreadEnd(std::ostream& ss)
	{
		LogMgr::Instance()->ErrorEnd(true);
		return ss;
	}
}
