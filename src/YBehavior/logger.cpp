#include "YBehavior/logger.h"
#include <iostream>
#include "stdarg.h"
#include <stdio.h>

namespace YBehavior
{

	LogMgr::LogMgr()
	{
		m_LogProcessor = nullptr;
		m_ErrorProcessor = nullptr;
	}

	void LogMgr::SetProcessor(LogProcessDelegate pLog, ErrorProcessDelegate pError)
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
}
