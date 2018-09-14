#ifndef _YBEHAVIOR_LOGGER_H_
#define _YBEHAVIOR_LOGGER_H_

//#include "iostream"
#include "sstream"
#include "singleton.h"

namespace YBehavior
{
	typedef void (*LogProcessDelegate)(const std::string& s);
	typedef void (*ErrorProcessDelegate)(const std::string& s);

	class LogMgr : public Singleton<LogMgr>
	{
	public:
		LogMgr();
		void SetProcessor(LogProcessDelegate pLog, ErrorProcessDelegate pError);
	protected:
		LogProcessDelegate m_LogProcessor;
		ErrorProcessDelegate m_ErrorProcessor;

		std::stringstream m_Stream;
	public:
		inline std::stringstream& LogBegin() { return m_Stream; }
		inline std::stringstream& ErrorBegin() { return m_Stream; }
		void LogEnd();
		void ErrorEnd();
	};

	std::ostream& ProcessLogEnd(std::ostream& ss);
	std::ostream& ProcessErrorEnd(std::ostream& ss);

#define LOG_BEGIN YBehavior::LogMgr::Instance()->LogBegin()
#define LOG_END YBehavior::ProcessLogEnd

#define ERROR_BEGIN  YBehavior::LogMgr::Instance()->ErrorBegin()
#define ERROR_END YBehavior::ProcessErrorEnd

}

#endif