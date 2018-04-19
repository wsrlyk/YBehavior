#include "YBehavior/define.h"

#ifdef GCC
#include "YBehavior/network/network.h"

#include "YBehavior/logger.h"

namespace YBehavior
{
	const size_t	kMaxPacketDataSize = 230;
	const size_t	kMaxPacketSize = 256;
	const size_t	kSocketBufferSize = 16384;
	const size_t	kGlobalQueueSize = (1024 * 32);
	const size_t	kLocalQueueSize = (1024 * 8);

	namespace Socket {
		bool InitSockets()
		{
			return true;
		}

		void ShutdownSockets() {
			
		}

		bool TestConnection(Handle h) {
			

			return false;
		}

		void Close(Handle& h)
		{
		}

		Handle CreateSocket(bool bBlock)
		{
			return Handle(0);
		}

		Handle Accept(Handle listeningSocket, size_t bufferSize) {
			return Handle(0);
		}

		bool Listen(Handle h, Port port, int maxConnections) {
			return true;
		}

		static size_t gs_packetsSent = 0;
		static size_t gs_packetsReceived = 0;

		bool Write(Handle& h, const void* buffer, size_t bytes, size_t& outBytesWritten) {
			return true;
		}

		size_t Read(Handle& h, const void* buffer, size_t bytesMax) {
			
			return 0;
		}

		size_t GetPacketsSent() {
			return gs_packetsSent;
		}

		size_t GetPacketsReceived() {
			return gs_packetsReceived;
		}
	}

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	namespace Thread
	{
		ThreadHandle CreateThread(ThreadFunction* function, void* arg)
		{
			return nullptr;
		}
		void SleepMilli (int millisec)
		{

		}
	}

	struct Mutex::MutexImpl {
	};

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////

	Mutex::Mutex()
	{

	}

	Mutex::~Mutex()
	{

	}

	void Mutex::Lock()
	{

	}

	void Mutex::Unlock()
	{

	}
}

#endif
