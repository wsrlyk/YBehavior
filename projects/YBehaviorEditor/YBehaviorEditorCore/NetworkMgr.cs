using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace YBehavior.Editor.Core
{
    struct HalfWord
    {
        private byte byte0;
        private byte byte1;
        public HalfWord(byte[] data, int startIndex)
        {
            byte0 = data[startIndex];
            byte1 = data[startIndex + 1];
        }

        public int ToInt()
        {
            return (int)(byte0 << 4 | byte1);
        }
    }
    public class MsgReceiver
    {
        // Assumes the following message format:
        //  - 1 byte with message size (not including this byte),
        //  - message contents

        public List<byte[]> OnDataReceived(byte[] dataBuffer, int receivedBytes)
        {
            List<byte[]> messages = new List<byte[]>();

            if (receivedBytes > 0)
            {
                int dataIndex = 0;

                // Combine data that we received now with whatever incomplete messages
                // we may have from previous call.
                if (m_pendingData != null)
                {
                    byte[] combinedBuffer = new byte[receivedBytes + m_pendingData.Length];
                    Array.Copy(m_pendingData, 0, combinedBuffer, 0, m_pendingData.Length);
                    Array.Copy(dataBuffer, 0, combinedBuffer, m_pendingData.Length, receivedBytes);
                    receivedBytes += m_pendingData.Length;
                    dataBuffer = combinedBuffer;
                    m_pendingData = null;
                }

                while (dataIndex < receivedBytes)
                {
                    // We only got message size, maybe not even that. Save & continue next time.
                    if (receivedBytes - dataIndex <= 2)
                    {
                        SavePendingData(dataBuffer, dataIndex, receivedBytes);
                        break;
                    }

                    HalfWord msgSize = new HalfWord(dataBuffer, dataIndex);
                    int messageSize = msgSize.ToInt();

                    // Incomplete message.
                    if (receivedBytes - (dataIndex + 2) < messageSize)
                    {
                        SavePendingData(dataBuffer, dataIndex, receivedBytes);
                        break;
                    }

                    dataIndex += 2;
                    byte[] fullMessage = new byte[messageSize];
                    Array.Copy(dataBuffer, dataIndex, fullMessage, 0, messageSize);
                    dataIndex += messageSize;
                    messages.Add(fullMessage);
                }
            }

            return messages;
        }
        void SavePendingData(byte[] data, int index, int len)
        {
            int toSave = len - index;
            m_pendingData = new byte[toSave];
            Array.Copy(data, index, m_pendingData, 0, toSave);
        }

        byte[] m_pendingData = null;
    }

    public class NetworkMgr : Singleton<NetworkMgr>
    {
        class SocketPacket
        {
            public enum CommandID
            {
                INITIAL_SETTINGS = 1,
                TEXT,
                MAX
            };

            public byte[] dataBuffer;
        }


        private const int kMaxTextLength = 4228 + 1 + 1;
        private const int BUFFER_SIZE = 16384 * 10;

        private uint m_packetsReceived = 0;
        private MsgReceiver m_msgReceiver = new MsgReceiver();
        private Socket m_clientSocket = null;
        private AsyncCallback m_pfnCallBack = null;

        public MessageProcessor MessageProcessor { get; } = new MessageProcessor();

        public bool IsConnected
        {
            get
            {
                return m_clientSocket != null && m_clientSocket.Connected;
            }
        }

        public bool Connect(string strIP, int iPort)
        {
            if (m_clientSocket == null)
            {
                try
                {
                    // Create the socket instance
                    m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    m_clientSocket.ReceiveBufferSize = BUFFER_SIZE;
                    m_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                    //m_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    //m_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

                    IPAddress ip = IPAddress.Parse(strIP);
                    IPEndPoint ipEnd = new IPEndPoint(ip, iPort);
                    m_clientSocket.Connect(ipEnd);

                    if (m_clientSocket.Connected)
                    {
                        onConnect();
                        return waitForData();
                    }

                }
                catch (SocketException se)
                {
                    MessageBox.Show(se.Message, "ConnectError");

                    m_clientSocket = null;
                }
            }

            return false;
        }

        private void onConnect()
        {
            this.m_packetsReceived = 0;
            MessageProcessor.OnNetworkConnectionChanged(true);
            NetworkConnectionChangedArg arg = new NetworkConnectionChangedArg()
            {
                bConnected = true
            };
            EventMgr.Instance.Send(arg);
        }

        public void Disconnect()
        {
            string msg = string.Format("[closeconnection]\n");

            this.SendText(msg);

            System.Threading.Thread.Sleep(200);

            closeSocket();
        }

        private void closeSocket()
        {
            try
            {
                if (m_clientSocket != null)
                {
                    m_clientSocket.Shutdown(SocketShutdown.Both);
                    m_clientSocket.Close();
                }

            }
            catch (SocketException)
            {
            }

            m_clientSocket = null;

            NetworkConnectionChangedArg arg = new NetworkConnectionChangedArg()
            {
                bConnected = false
            };
            EventMgr.Instance.Send(arg);
            MessageProcessor.OnNetworkConnectionChanged(false);
            DebugMgr.Instance.Clear();
        }

        SocketPacket _theSocPkt = null;
        private bool waitForData()
        {
            try
            {
                if (m_clientSocket == null || !m_clientSocket.Connected)
                {
                    return false;
                }

                if (m_pfnCallBack == null)
                {
                    m_pfnCallBack = new AsyncCallback(onDataReceived);
                }

                if (_theSocPkt == null)
                {
                    _theSocPkt = new SocketPacket();
                    _theSocPkt.dataBuffer = new byte[BUFFER_SIZE];
                }

                if (m_clientSocket != null)
                {
                    // Start listening to the data asynchronously
                    m_clientSocket.BeginReceive(_theSocPkt.dataBuffer,
                                                0, _theSocPkt.dataBuffer.Length,
                                                SocketFlags.None,
                                                m_pfnCallBack,
                                                _theSocPkt);

                    return true;
                }

            }
            catch (SocketException se)
            {
                //MessageBox.Show(se.Message, Resources.ConnectError);
                LogMgr.Instance.Error("ConnectError: " + se.Message);
            }

            return false;
        }

        private void onDataReceived(IAsyncResult asyn)
        {
            try
            {
                if (m_clientSocket == null || !m_clientSocket.Connected)
                {
                    return;
                }

                SocketPacket packet = (SocketPacket)asyn.AsyncState;
                int receivedBytes = m_clientSocket.EndReceive(asyn);
                if (receivedBytes == 0)
                {
                    LogMgr.Instance.Error("ConnectError");
                    closeSocket();
                    return;
                }

                List<byte[]> messages = m_msgReceiver.OnDataReceived(packet.dataBuffer, receivedBytes);
                m_packetsReceived += (uint)messages.Count;

                for (int i = 0; i < messages.Count; ++i)
                {
                    handleMessage(messages[i]);
                }

                if (m_clientSocket != null && m_clientSocket.Connected)
                {
                    waitForData();
                }

            }
            catch (NullReferenceException)
            {
                // Socket closed (most probably)
            }
            catch (ObjectDisposedException)
            {
                // Socket closed
            }
            catch (SocketException exc)
            {
                MessageBox.Show(exc.Message);
                LogMgr.Instance.Error("ConnectError: " + exc.Message);

                closeSocket();
                //Invoke(m_delegateOnDisconnect);

            }
            catch (Exception e)
            {
                LogMgr.Instance.Error("Exception: " + e.Message);

            }
        }

        public void SendText(string msg)
        {
            if (m_clientSocket != null && m_clientSocket.Connected)
            {
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(msg);
                try
                {
                    m_clientSocket.Send(bytes);
                }
                catch (SocketException e)
                {
                    LogMgr.Instance.Error("Exception: " + e.Message);
                    closeSocket();
                }
                catch (Exception e)
                {
                    LogMgr.Instance.Error("Exception: " + e.Message);

                }
            }
        }

        public uint PacketsReceived
        {
            get
            {
                return this.m_packetsReceived;
            }
        }

        static private uint GetInt(byte[] data, int i)
        {
            return (uint)((data[i + 3] << 24) + (data[i + 2] << 16) + (data[i + 1] << 8) + (data[i + 0]));
        }

        private static string GetStringFromBuffer(byte[] data, int dataIdx, int maxLen, bool isAsc)
        {
            Encoding ecode;

            if (isAsc)
            {
                ecode = new ASCIIEncoding();

            }
            else
            {
                ecode = new UTF8Encoding();
            }

//            Debug.Check(data.Length <= maxLen);

            if (data.Length <= maxLen)
            {
                maxLen = data.Length;

                string ret = ecode.GetString(data, dataIdx, maxLen);
                char[] zeroChars = { '\0', '?' };

                return ret.TrimEnd(zeroChars);
            }

            return "[Error]The length of the message is above the Max value!";
        }

        int[] m_packets = new int[(int)SocketPacket.CommandID.MAX];

        private void handleMessage(byte[] msgData)
        {
            try
            {
                if (msgData.Length > 0)
                {
                    //SocketPacket.CommandID commandId = (SocketPacket.CommandID)(msgData[0]);

                    //m_packets[(int)commandId]++;

                    //switch (commandId)
                    //{
                    //    case SocketPacket.CommandID.INITIAL_SETTINGS:
                    //        {
                    //            int platform = (int)msgData[1];
                    //            int processId = (int)GetInt(msgData, 2);
                    //            break;
                    //        }

                    //    case SocketPacket.CommandID.TEXT:
                    //        {
                                handleText(msgData);
                    //            break;
                    //        }

                    //    default:
                    //        {
                    //            System.Diagnostics.Debug.Fail("Unknown command ID: " + commandId);
                    //            break;
                    //        }
                    //}
                }

            }
            catch (Exception e)
            {
                LogMgr.Instance.Error("ConnectError: " + e.Message);
                //MessageBox.Show(e.Message, Resources.ConnectError);
            }
        }

        //int _lastIndex = -1;

        private void handleText(byte[] msgData)
        {
            string text = GetStringFromBuffer(msgData, 0, kMaxTextLength, true);
            if (Config.Instance.PrintIntermediateInfo)
                LogMgr.Instance.Log(text);
            MessageProcessor.Receive(text);
        }

    }
}
