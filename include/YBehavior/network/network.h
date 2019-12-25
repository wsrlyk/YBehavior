#ifndef _YBEHAVIOR_NETWORK_H_
#define _YBEHAVIOR_NETWORK_H_

#ifdef YDEBUGGER
#include "YBehavior/types.h"
#include <vector>
#include <sstream>
#include "YBehavior/singleton.h"

namespace YBehavior
{
#ifdef MSVC
	typedef unsigned int (_stdcall ThreadFunction)(void* arg);
#else
	typedef unsigned int (ThreadFunction)(void* arg);
#endif
	typedef void*		ThreadHandle;
	typedef int	Handle;
	typedef unsigned short	Port;
	namespace Socket
	{
		bool InitSockets();
		void ShutdownSockets();

		Handle CreateSocket(bool bBlock);
		void Close(Handle&);

		bool Listen(Handle, Port port, int maxConnections = 5);
		bool TestConnection(Handle);
		Handle Accept(Handle listeningSocket, size_t bufferSize);

		bool Write(Handle& h, const void* buffer, size_t bytes, size_t& outBytesWritten);

		size_t Read(Handle& h, const void* buffer, size_t bytes);

		size_t GetPacketsSent();
		size_t GetPacketsReceived();
	}

	namespace Thread
	{
		ThreadHandle CreateThread(ThreadFunction* function, void* arg);
		void SleepMilli(int millisec);
	}

	struct HalfWord
	{
		CHAR bytes[2];
		HalfWord(INT value)
		{
			value &= 0xffff;
			bytes[0] = (value >> 8);
			bytes[1] = (value & 0xff);
		}
		const STRING ToString() const
		{
			STRING s(bytes, 2);
			return s;
		}
	};

	class Mutex
	{
	public:
		Mutex();
		~Mutex();

		void Lock();
		void Unlock();

	private:
		struct MutexImpl;
		struct MutexImpl* _impl;

		static const int kMutexBufferSize = 40;

		uint8_t        m_buffer[kMutexBufferSize];
	};

	class ScopedLock
	{
		Mutex& m_mutex_;
	public:
		ScopedLock(Mutex& cs) : m_mutex_(cs) {
			m_mutex_.Lock();
		}

		~ScopedLock() {
			m_mutex_.Unlock();
		}
	};

	class Network : public Singleton<Network>
	{
	
		ThreadHandle m_ThreadHandle;
		Handle m_ListeningHandle;
		Handle	m_WriteSocket;

		int m_isConnected;
		bool m_bTerminating = false;
		int m_Port;
		String ms_texts;
		String ms_sendBuffer;

		Mutex m_Mutex;
	public:
		Network();
		bool IsConnected() const;

		void InitAndCreateThread(int port);
		void Close();

		static void _ThreadFunc(Network*);
		void ThreadFunc();

		void OnConnection() {} // used once after the connection is established, make some initialization between server and client;

		void SendAllPackets();
		bool ReceivePackets(const char* msgCheck = 0);
		bool ReadText(STRING& text);
		bool SendText(const STRING& text);
		void ClearOneConnection();
		void ClearAll();
		void OnRecieveMessages(const STRING& msg);
	};
}
#endif // YDEBUGGER

#endif
