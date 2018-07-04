#ifdef DEBUGGER
#include "YBehavior/network/network.h"
#include "YBehavior/logger.h"
#include "YBehavior/network/messageprocessor.h"
#include <string.h>

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
			//wait for connecting
			//while (!m_bTerminating)
			//{
			//	//Log("Socket::TestConnection.\n");
			//	LOG_BEGIN << "Socket::TestConnection" << LOG_END;

			//	if (Socket::TestConnection(m_ListeningHandle))
			//	{
			//		break;
			//	}

			//	Thread::SleepMilli(100);
			//}


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
				LOG_BEGIN << "Socket::Connection Accept" << LOG_END;

				{

					++m_isConnected;
					Thread::SleepMilli(1);

					OnConnection();

					//AtomicInc(m_isConnectedFinished);
					Thread::SleepMilli(1);

					//this->OnConnectionFinished();
				}

				while (!m_bTerminating && this->m_WriteSocket)
				{
					Thread::SleepMilli(1);
					SendAllPackets();

					ReceivePackets();

					// LOG_BEGIN << "Socket::Send & Receive" << LOG_END;

				}

				// One last time, to send any outstanding packets out there.
				if (this->m_WriteSocket)
				{
					SendAllPackets();

					Socket::Close(m_WriteSocket);

					LOG_BEGIN << "Socket::Close" << LOG_END;

				}

				this->ClearOneConnection();
			}
		}

		Socket::Close(m_ListeningHandle);

		this->ClearAll();

		LOG_BEGIN << "Network Thread Shutdown" << LOG_END;
	}

	void Network::SendAllPackets()
	{
		if (ms_sendBuffer.length() > 0)
		{
			ScopedLock lock(m_Mutex);

			LOG_BEGIN << "Try Send: " << ms_sendBuffer << LOG_END;
			size_t len;
			if (Socket::Write(m_WriteSocket, ms_sendBuffer.c_str(), ms_sendBuffer.length(), len) && len != ms_sendBuffer.length())
			{
				ERROR_BEGIN << "Network Send Error: " << ms_sendBuffer << ERROR_END;
			}

			ms_sendBuffer = "";
		}
	}

	bool Network::ReceivePackets(const char* msgCheck /*= 0*/)
	{
		const int kBufferLen = 2048;
		char buffer[kBufferLen];

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

			HalfWord halfword(text.size());
			ms_sendBuffer += halfword.ToString();
			ms_sendBuffer += text;

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
		LOG_BEGIN << "Receive: " << msg << LOG_END;
		MessageProcessor::Instance()->ProcessOne(msg);
	}

}
#endif // DEBUGGER
