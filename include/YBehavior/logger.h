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
		void SetProcessorThread(LogProcessDelegate pLog, ErrorProcessDelegate pError);
	protected:
		LogProcessDelegate m_LogProcessor;
		ErrorProcessDelegate m_ErrorProcessor;
		LogProcessDelegate m_LogThreadProcessor;
		ErrorProcessDelegate m_ErrorThreadProcessor;

		std::stringstream m_Stream;
		char m_Buffer[1024];
	public:
		inline std::stringstream& LogBegin() { return m_Stream; }
		inline std::stringstream& ErrorBegin() { return m_Stream; }
		void LogEnd(bool bThread = false);
		void ErrorEnd(bool bThread = false);
		void Log(const char* fmt, ...);
		void Error(const char* fmt, ...);
		void LogThread(const char* fmt, ...);
		void ErrorThread(const char* fmt, ...);
	protected:
		void _Format(const char* fmt, va_list ap);
	};

	std::ostream& ProcessLogEnd(std::ostream& ss);
	std::ostream& ProcessErrorEnd(std::ostream& ss);
	std::ostream& ProcessLogThreadEnd(std::ostream& ss);
	std::ostream& ProcessErrorThreadEnd(std::ostream& ss);

#define LOG_BEGIN YBehavior::LogMgr::Instance()->LogBegin()
#define LOG_END YBehavior::ProcessLogEnd
#define LOG_THREAD_END YBehavior::ProcessLogThreadEnd
#define LOG_FORMAT(fmt, ...) YBehavior::LogMgr::Instance()->Log(fmt, __VA_ARGS__)
#define LOG_THREAD_FORMAT(fmt, ...) YBehavior::LogMgr::Instance()->LogThread(fmt, __VA_ARGS__)

#define ERROR_BEGIN  YBehavior::LogMgr::Instance()->ErrorBegin()
#define ERROR_END YBehavior::ProcessErrorEnd
#define ERROR_THREAD_END YBehavior::ProcessErrorThreadEnd
#define ERROR_FORMAT(fmt, ...) YBehavior::LogMgr::Instance()->Error(fmt, __VA_ARGS__)
#define ERROR_THREAD_FORMAT(fmt, ...) YBehavior::LogMgr::Instance()->ErrorThread(fmt, __VA_ARGS__)

}

#endif