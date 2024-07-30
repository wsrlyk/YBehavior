#ifdef YSHARP
#ifndef _SHARPLAUNCH_H_
#define _SHARPLAUNCH_H_

#include "YBehavior/launcher.h"

namespace YBehavior
{
	typedef void(STDCALL *SharpLogDelegate)();

	class SharpLaunchCore : public LaunchCore
	{
	private:
		int m_Port{};
		static STRING m_WorkingDir;
	public:
		SharpLaunchCore(int debugport);
		int StartWithDebugListeningPort() const override;
		STRING WorkingDir() const override { return m_WorkingDir; }

		void GetLogProcessor(LogProcessDelegate &pLog, ErrorProcessDelegate & pError) const override;
		void GetThreadLogProcessor(LogProcessDelegate &pLog, ErrorProcessDelegate & pError) const override;
	
		static void SetCallback(
			SharpLogDelegate log, 
			SharpLogDelegate error, 
			SharpLogDelegate threadlog, 
			SharpLogDelegate threaderror);
		static void SetWorkingDir(CSTRING_CONST path);
	private:
		static void s_ProcessLog(const STRING& str);
		static void s_ProcessError(const STRING& str);
		static void s_ProcessThreadLog(const STRING& str);
		static void s_ProcessThreadError(const STRING& str);

	private:
		static SharpLogDelegate s_LogCallback;
		static SharpLogDelegate s_ErrorCallback;
		static SharpLogDelegate s_ThreadLogCallback;
		static SharpLogDelegate s_ThreadErrorCallback;
	};
}


#endif // _SHARPNODE_H_
#endif