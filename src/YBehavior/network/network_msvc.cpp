#ifdef YDEBUGGER
#include "YBehavior/define.h"

#ifdef YB_MSVC
#include "YBehavior/network/network.h"

#include <windows.h>
#include <process.h>		// beginthreadex
#include "YBehavior/logger.h"
#include <winsock.h>
#include <assert.h>
#pragma comment(lib, "Ws2_32.lib")

namespace YBehavior
{
	const size_t	kMaxPacketDataSize = 230;
	const size_t	kMaxPacketSize = 256;
	const size_t	kSocketBufferSize = 16384;
	const size_t	kGlobalQueueSize = (1024 * 32);
	const size_t	kLocalQueueSize = (1024 * 8);

	SOCKET AsWinSocket(Handle h)
	{
		return (SOCKET)(h);
	}

	namespace Socket {
		bool InitSockets()
		{
			WSADATA wsaData;
			int ret = WSAStartup(MAKEWORD(2, 2), &wsaData);
			return (ret == 0);
		}

		void ShutdownSockets()
		{
			WSACleanup();
		}

		bool TestConnection(Handle h) 
		{
			SOCKET winSocket = AsWinSocket(h);
			fd_set readSet;
			FD_ZERO(&readSet);
			FD_SET(winSocket, &readSet);
			timeval timeout = { 0, 17000 };
			int res = ::select(0, &readSet, 0, 0, &timeout);

			if (res > 0) {
				if (FD_ISSET(winSocket, &readSet))
				{
					return true;
				}
			}

			return false;
		}

		void Close(Handle& h)
		{
			::closesocket(AsWinSocket(h));
			h = Handle(0);
		}

		Handle CreateSocket(bool bBlock)
		{
			SOCKET winSocket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

			if (winSocket == INVALID_SOCKET)
			{
				return Handle(0);
			}

			Handle r = Handle(winSocket);

			unsigned long inonBlocking = (bBlock ? 0 : 1);

			if (ioctlsocket(winSocket, FIONBIO, &inonBlocking) == 0) 
			{
				return r;
			}

			Close(r);

			return Handle(0);
		}

		Handle Accept(Handle listeningSocket, size_t bufferSize) 
		{
			typedef int socklen_t;
			sockaddr_in addr;
			socklen_t len = sizeof(sockaddr_in);
			memset(&addr, 0, sizeof(sockaddr_in));
			SOCKET outSocket = ::accept(AsWinSocket(listeningSocket), (sockaddr*)&addr, &len);

			if (outSocket != SOCKET_ERROR)
			{
				int sizeOfBufSize = sizeof(bufferSize);
				::setsockopt(outSocket, SOL_SOCKET, SO_RCVBUF, (const char*)&bufferSize, sizeOfBufSize);
				::setsockopt(outSocket, SOL_SOCKET, SO_SNDBUF, (const char*)&bufferSize, sizeOfBufSize);
				return Handle(outSocket);
			}

			return Handle(0);
		}

		bool Listen(Handle h, Port port, int maxConnections)
		{
			SOCKET winSocket = AsWinSocket(h);
			sockaddr_in addr = { 0 };
			addr.sin_addr.s_addr = INADDR_ANY;
			addr.sin_family = AF_INET;
			addr.sin_port = htons(port);
			memset(addr.sin_zero, 0, sizeof(addr.sin_zero));

			int bReuseAddr = 1;
			::setsockopt(winSocket, SOL_SOCKET, SO_REUSEADDR, (const char*)&bReuseAddr, sizeof(bReuseAddr));

			//int rcvtimeo = 1000;
			//::setsockopt(winSocket, SOL_SOCKET, SO_RCVTIMEO, (const char*)&rcvtimeo, sizeof(rcvtimeo));

			if (::bind(winSocket, reinterpret_cast<const sockaddr*>(&addr), sizeof(addr)) == SOCKET_ERROR)
			{
				Close(h);
				return false;
			}

			if (::listen(winSocket, maxConnections) == SOCKET_ERROR)
			{
				Close(h);
				return false;
			}

			return true;
		}

		static size_t gs_packetsSent = 0;
		static size_t gs_packetsReceived = 0;

		bool Write(Handle& h, const void* buffer, size_t bytes, size_t& outBytesWritten)
		{
			outBytesWritten = 0;

			if (bytes == 0 || !h)
			{
				return bytes == 0;
			}

			int res = ::send(AsWinSocket(h), (const char*)buffer, (int)bytes, 0);

			if (res == SOCKET_ERROR)
			{
				int err = WSAGetLastError();

				if (err == WSAECONNRESET || err == WSAECONNABORTED)
				{
					Close(h);
				}
			}
			else 
			{
				outBytesWritten = (size_t)res;
				gs_packetsSent++;
			}

			return outBytesWritten != 0;
		}

		size_t Read(Handle& h, const void* buffer, size_t bytesMax)
		{
			size_t bytesRead = 0;

			if (bytesMax == 0 || !h)
			{
				return bytesRead;
			}

			fd_set readfds;
			FD_ZERO(&readfds);
			FD_SET(AsWinSocket(h), &readfds);

			struct timeval tv;

			tv.tv_sec = 0;
			tv.tv_usec = 100000;//0.1s

			int rv = ::select(2, &readfds, 0, 0, &tv);

			if (rv == -1)
			{
			}
			else if (rv == 0)
			{
				//timeout
			}
			else
			{
				int res = ::recv(AsWinSocket(h), (char*)buffer, (int)bytesMax, 0);

				if (res == SOCKET_ERROR) 
				{
					int err = WSAGetLastError();

					if (err == WSAECONNRESET || err == WSAECONNABORTED)
					{
						Close(h);
					}
				}
				else if (res == 0)
				{
					///> Client has been closed.
					Close(h);
				}
				else
				{
					bytesRead = (size_t)res;
					gs_packetsReceived++;
				}

				return bytesRead;
			}

			return 0;
		}

		size_t GetPacketsSent()
		{
			return gs_packetsSent;
		}

		size_t GetPacketsReceived()
		{
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
			const uint32_t creationFlags = 0x0;
			uintptr_t hThread = ::_beginthreadex(NULL, (unsigned)300, function, arg, creationFlags, NULL);
			return (ThreadHandle)hThread;
		}
		void SleepMilli (int millisec)
		{
			Sleep(millisec);
		}
	}

	struct Mutex::MutexImpl
	{
		CRITICAL_SECTION    _criticalSection;
	};

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////

	Mutex::Mutex()
	{
		// Be sure that the shadow is large enough
		assert(sizeof(m_buffer) >= sizeof(MutexImpl));

		// Use the shadow as memory space for the platform specific implementation
		_impl = (MutexImpl*)m_buffer;

		InitializeCriticalSection(&_impl->_criticalSection);
	}

	Mutex::~Mutex()
	{
		DeleteCriticalSection(&_impl->_criticalSection);
	}

	void Mutex::Lock()
	{
		EnterCriticalSection(&_impl->_criticalSection);
	}

	void Mutex::Unlock()
	{
		LeaveCriticalSection(&_impl->_criticalSection);
	}
}
#endif
#endif // YDEBUGGER
