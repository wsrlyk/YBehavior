#ifdef DEBUGGER
#include "YBehavior/define.h"

#ifdef GCC
#include <pthread.h>		// beginthreadex
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/ioctl.h>
#include <unistd.h>
#include <fcntl.h>
#include <netdb.h>
#include <netinet/in.h>
#include <memory.h>
#include "YBehavior/network/network.h"
#include "YBehavior/logger.h"

namespace YBehavior
{
	const size_t	kMaxPacketDataSize = 230;
	const size_t	kMaxPacketSize = 256;
	const size_t	kSocketBufferSize = 16384;
	const size_t	kGlobalQueueSize = (1024 * 32);
	const size_t	kLocalQueueSize = (1024 * 8);

	typedef int SOCKET;
	SOCKET AsSocket(Handle h) 
	{
		return (SOCKET)(h);
	}

	namespace Socket
	{
		bool InitSockets()
		{
			return true;
		}

		void ShutdownSockets()
		{
			
		}

		bool TestConnection(Handle h)
		{
			SOCKET winSocket = AsSocket(h);
			fd_set readSet;
			FD_ZERO(&readSet);
			FD_SET(winSocket, &readSet);
			timeval timeout = { 0, 17000 };
			int res = ::select(winSocket + 1, &readSet, 0, 0, &timeout);

			if (res > 0)
			{
				LOG_BEGIN << "Select Res: " << res << LOG_THREAD_END;
				if (FD_ISSET(winSocket, &readSet)) 
				{
					return true;
				}
			}

			return false;
		}

		void Close(Handle& h)
		{
			close(AsSocket(h));
			h = Handle(0);
		}

		Handle CreateSocket(bool bBlock)
		{
			SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

			if (s < 0)
			{
				return Handle(0);
			}

//#ifndef SO_NONBLOCK
//#ifdef SO_NOSIGPIPE
//			int noSigpipe = 1;
//			setsockopt(s, SOL_SOCKET, SO_NOSIGPIPE, &noSigpipe, sizeof(noSigpipe));
//#endif
//			int inonBlocking = (bBlock ? 0 : 1);
//			return setsockopt(s, SOL_SOCKET, SO_NONBLOCK, &inonBlocking, sizeof(inonBlocking)) == 0 ? Handle(s) : Handle(0));
//#else
			int v = fcntl(s, F_GETFL, 0);
			int cntl = !bBlock ? (v | O_NONBLOCK) : (v & ~O_NONBLOCK);

			return fcntl(s, F_SETFL, cntl) != -1 ? Handle(s) : Handle(0);
//#endif
//			return Handle(0);
		}

		Handle Accept(Handle listeningSocket, size_t bufferSize)
		{
#ifndef  __GNUC__
			typedef int socklen_t;
#endif
			sockaddr_in addr;
			socklen_t len = sizeof(sockaddr_in);
			memset(&addr, 0, sizeof(sockaddr_in));
			SOCKET outSocket = ::accept(AsSocket(listeningSocket), (sockaddr*)&addr, &len);

			if (outSocket > 0)
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
			SOCKET winSocket = AsSocket(h);
			sockaddr_in addr;

			memset(&addr, 0, sizeof(addr));
			addr.sin_addr.s_addr = htonl(INADDR_ANY);
			addr.sin_family = AF_INET;
			addr.sin_port = htons(port);
			memset(addr.sin_zero, 0, sizeof(addr.sin_zero));

			int bReuseAddr = 1;
			::setsockopt(winSocket, SOL_SOCKET, SO_REUSEADDR, (const char*)&bReuseAddr, sizeof(bReuseAddr));

			//int rcvtimeo = 1000;
			//::setsockopt(winSocket, SOL_SOCKET, SO_RCVTIMEO, (const char*)&rcvtimeo, sizeof(rcvtimeo));

			int res = (bind(winSocket, reinterpret_cast<const sockaddr*>(&addr), sizeof(addr)) < 0);
			if (res)
			{
				Close(h);
				LOG_BEGIN << "Listen failed at bind, " << strerror(res) << LOG_THREAD_END;
				return false;
			}

			res = (listen(winSocket, maxConnections) < 0);
			if (res)
			{
				Close(h);
				LOG_BEGIN << "Listen failed at listen, " << strerror(res) << LOG_THREAD_END;
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

			const int flags = MSG_NOSIGNAL;

			int res = ::send(AsSocket(h), (const char*)buffer, (int)bytes, flags);

			if (res < 0)
			{
				//BEHAVIAC_ASSERT(0);
				// int err = WSAGetLastError();

				// if (err == WSAECONNRESET || err == WSAECONNABORTED)
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
			FD_SET(AsSocket(h), &readfds);
			int maxfd1 = h + 1;

			struct timeval tv;

			tv.tv_sec = 0;
			tv.tv_usec = 100000;//0.1s

			int rv = ::select(maxfd1, &readfds, 0, 0, &tv);

			if (rv < 0)
			{
				//BEHAVIAC_ASSERT(0);
			}
			else if (rv == 0)
			{
				//timeout
			}
			else
			{
				int res = ::recv(AsSocket(h), (char*)buffer, (int)bytesMax, 0);

				if (res <= 0)
				{
					LOG_BEGIN << "Recv Error: " << strerror(res) << LOG_THREAD_END;
					// int err = WSAGetLastError();

					// if (err == WSAECONNRESET || err == WSAECONNABORTED)
					{
						Close(h);
					}
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
			typedef void* ThreadFunction_t(void * arg);

			pthread_t tid;
			int rc = pthread_create(&tid, NULL, (ThreadFunction_t*)function, arg);

			if (rc)
			{
				return 0;
			}

			return (ThreadHandle)tid;
		}
		void SleepMilli (int millisec)
		{
			usleep(millisec * 1000);
		}
	}

	struct Mutex::MutexImpl
	{
		pthread_mutex_t _mutex;
	};

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////

	Mutex::Mutex()
	{
		_impl = (MutexImpl*)m_buffer;

		pthread_mutex_init(&_impl->_mutex, 0);
	}

	Mutex::~Mutex()
	{
		pthread_mutex_destroy(&_impl->_mutex);
	}

	void Mutex::Lock()
	{
		pthread_mutex_lock(&_impl->_mutex);
	}

	void Mutex::Unlock()
	{
		pthread_mutex_unlock(&_impl->_mutex);
	}
}

#endif
#endif // DEBUGGER
