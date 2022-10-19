#ifdef YDEBUGGER
#include "YBehavior/network/network.h"
#include "YBehavior/logger.h"
#include "YBehavior/network/messageprocessor.h"
#include <cstring>

namespace YBehavior
{
	const size_t	kMaxPacketDataSize = 230;
	const size_t	kMaxPacketSize = 256;
	const size_t	kSocketBufferSize = 16384;
	const size_t	kGlobalQueueSize = (1024 * 32);
	const size_t	kLocalQueueSize = (1024 * 8);

	Network::Network()
	{
		m_ThreadHandle = 0;
	}

	bool Network::IsConnected() const
	{
		return m_isConnected != 0 && this->m_WriteSocket;
	}

	//bool Network::IsConnectedFinished() const {
	//	return m_isConnectedFinished != 0;
	//}

	//bool Network::IsInited() const {
	//	return m_isInited != 0;
	//}

	void Network::InitAndCreateThread(int port)
	{
		if (m_ThreadHandle)
		{
			ERROR_BEGIN << "Already has a thread." << ERROR_END;
			return;
		}

		Socket::InitSockets();
		m_Port = port;
		m_ThreadHandle = Thread::CreateThread((ThreadFunction*)&_ThreadFunc, this);
	}

	void Network::Close()
	{
		if (!m_ThreadHandle)
		{
			ERROR_BEGIN << "Has no thread." << ERROR_END;
			return;
		}

		m_bTerminating = true;
		Socket::Close(m_ListeningHandle);
		m_ThreadHandle = 0;
	}

	void Network::_ThreadFunc(Network* network)
	{
		network->ThreadFunc();
	}
	void Network::ThreadFunc()
	{
		m_ListeningHandle = Socket::CreateSocket(true);

		if (!Socket::Listen(m_ListeningHandle, m_Port, 1))
		{
			Socket::Close(m_ListeningHandle);
			return;
		}

		while (!m_bTerminating)
		{
			if (!m_bTerminating)
			{
				{
					m_WriteSocket = Socket::Accept(m_ListeningHandle, kSocketBufferSize);

					if (!m_WriteSocket)
					{
						Socket::Close(m_ListeningHandle);
						return;
					}
					
				}
				LOG_BEGIN << "Socket::Connection Accept" << LOG_THREAD_END;

				{

					++m_isConnected;
					Thread::SleepMilli(1);

					OnConnection();

					Thread::SleepMilli(1);
				}

				while (!m_bTerminating && this->m_WriteSocket)
				{
					Thread::SleepMilli(1);
					SendAllPackets();

					ReceivePackets();
				}

				if (this->m_WriteSocket)
				{
					SendAllPackets();

					Socket::Close(m_WriteSocket);

					LOG_BEGIN << "Socket::Close" << LOG_THREAD_END;

				}

				this->ClearOneConnection();
			}
		}

		Socket::Close(m_ListeningHandle);

		this->ClearAll();

		LOG_BEGIN << "Network Thread Shutdown" << LOG_THREAD_END;
	}

	void Network::SendAllPackets()
	{
		if (ms_sendBuffer.length() > 0)
		{
			ScopedLock lock(m_Mutex);
#ifdef PRINT_INTERMEDIATE_INFO
			LOG_BEGIN << "Try Send: " << ms_sendBuffer << LOG_THREAD_END;
#endif
			size_t len;
			if (Socket::Write(m_WriteSocket, ms_sendBuffer.c_str(), ms_sendBuffer.length(), len) && len != ms_sendBuffer.length())
			{
				LOG_BEGIN << "Network Send Error: " << ms_sendBuffer << LOG_THREAD_END;
			}

			ms_sendBuffer = "";
		}
	}

	bool Network::ReceivePackets(CSTRING_CONST msgCheck /*= 0*/)
	{
		const int kBufferLen = 2048;
		char buffer[kBufferLen + 1];

		bool found = false;

		while (size_t reads = Socket::Read(m_WriteSocket, buffer, kBufferLen))
		{
			buffer[reads] = '\0';
			//printf("ReceivePackets %s\n", buffer);

			{
				ScopedLock lock(m_Mutex);

				ms_texts += buffer;
			}

			if (msgCheck && strstr(buffer, msgCheck))
			{
				//printf("ReceivePackets found\n");
				found = true;
			}

			if (this->m_WriteSocket == 0)
			{
				break;
			}
		}

		///> Read text immediately
		{
			STRING msgs;

			if (this->ReadText(msgs))
			{
				this->OnRecieveMessages(msgs);

				return true;
			}
		}

		return found;
	}

	bool Network::ReadText(STRING& text)
	{
		if (this->IsConnected())
		{
			ScopedLock lock(m_Mutex);

			text = this->ms_texts;
			this->ms_texts.clear();

			return !text.empty();
		}

		return false;
	}

	bool Network::SendText(const STRING& text)
	{
		if (this->IsConnected())
		{
			ScopedLock lock(m_Mutex);

			HalfWord halfword((INT)text.size());
			ms_sendBuffer += halfword.ToString();
			ms_sendBuffer += text;
#ifdef PRINT_INTERMEDIATE_INFO
			LOG_BEGIN << "Message: " << text.size() << ": " << ms_sendBuffer << LOG_THREAD_END;
#endif

			return true;
		}

		return false;
	}

	void Network::ClearOneConnection()
	{
		m_isConnected = 0;
		ms_texts = "";
		m_WriteSocket = 0;

		MessageProcessor::Instance()->OnNetworkClosed();
	}

	void Network::ClearAll()
	{
		ClearOneConnection();
		m_bTerminating = false;
		m_ListeningHandle = 0;
	}

	void Network::OnRecieveMessages(const STRING& msg)
	{
		LOG_BEGIN << "Receive: " << msg << LOG_THREAD_END;
		MessageProcessor::Instance()->ProcessOne(msg);
	}

}
#endif // YDEBUGGER
