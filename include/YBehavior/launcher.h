#ifndef _YBEHAVIOR_LAUNCHER_H_
#define _YBEHAVIOR_LAUNCHER_H_

#include "YBehavior/nodefactory.h"
namespace YBehavior
{
	class LaunchCore
	{
	public:
		virtual void RegisterActions() const;
		virtual int StartWithDebugListeningPort() const { return 0; }
#ifndef YSHARP
		virtual STRING WorkingDir() const { return ""; }
#endif
		virtual void GetLogProcessor(LogProcessDelegate &pLog, ErrorProcessDelegate & pError) const { return; }
		virtual void GetThreadLogProcessor(LogProcessDelegate &pLog, ErrorProcessDelegate & pError) const { return; }
	protected:
		template<typename T>
		void _Register() const;
	};

	template<typename T>
	inline void LaunchCore::_Register() const
	{
		REGISTER_TYPE(NodeFactory::Instance(), T);
	}


	//////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////
	class Launcher
	{
	public:
		static bool Launch(const LaunchCore& core);
	};
}

#endif