#ifdef YSHARP

#include "YBehavior/sharp/sharplaunch.h"
#include "YBehavior/sharp/sharpentry_buffer.h"

namespace YBehavior
{
	YBehavior::SharpLogDelegate SharpLaunchCore::s_LogCallback{};
	YBehavior::SharpLogDelegate SharpLaunchCore::s_ErrorCallback{};
	YBehavior::SharpLogDelegate SharpLaunchCore::s_ThreadLogCallback{};
	YBehavior::SharpLogDelegate SharpLaunchCore::s_ThreadErrorCallback{};

	SharpLaunchCore::SharpLaunchCore(int debugport)
		: m_Port(debugport)
	{
		s_LogCallback = nullptr;
		s_ErrorCallback = nullptr;
		s_ThreadLogCallback = nullptr;
		s_ThreadErrorCallback = nullptr;
	}

	int SharpLaunchCore::StartWithDebugListeningPort() const
	{
		return m_Port;
	}

	void SharpLaunchCore::GetLogProcessor(LogProcessDelegate &pLog, ErrorProcessDelegate & pError) const
	{
		pLog = ProcessLog;
		pError = ProcessError;
	}
	void SharpLaunchCore::GetThreadLogProcessor(LogProcessDelegate &pLog, ErrorProcessDelegate & pError) const
	{
		pLog = ProcessThreadLog;
		pError = ProcessThreadError;

	}

	void SharpLaunchCore::SetCallback(
		SharpLogDelegate log, 
		SharpLogDelegate error, 
		SharpLogDelegate threadlog, 
		SharpLogDelegate threaderror)
	{
		s_LogCallback = log;
		s_ErrorCallback = error;
		s_ThreadLogCallback = threadlog;
		s_ThreadErrorCallback = threaderror;
	}

	void SharpLaunchCore::ProcessLog(const STRING& str)
	{
		SharpBuffer::s_Buffer.m_String = str;
		if (s_LogCallback)
			s_LogCallback();
	}

	void SharpLaunchCore::ProcessError(const STRING& str)
	{
		SharpBuffer::s_Buffer.m_String = str;
		if (s_ErrorCallback)
			s_ErrorCallback();
	}

	void SharpLaunchCore::ProcessThreadLog(const STRING& str)
	{
		SharpBuffer::s_Buffer.m_String = str;
		if (s_ThreadLogCallback)
			s_ThreadLogCallback();
	}

	void SharpLaunchCore::ProcessThreadError(const STRING& str)
	{
		SharpBuffer::s_Buffer.m_String = str;
		if (s_ThreadErrorCallback)
			s_ThreadErrorCallback();
	}
}

#endif