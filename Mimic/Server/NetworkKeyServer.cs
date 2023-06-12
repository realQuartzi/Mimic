using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Mimic
{
    public class NetworkKeyServer : BaseServer
    {
        ConcurrentDictionary<IPEndPoint, byte[]> keyPairs;

        /// <summary>
        /// Create and Start a new NetworkServer which will listen to the given port
        /// </summary>
        /// <param name="port"></param>
        public NetworkKeyServer(int port = 4117)
        {
            Console.WriteLine("[Server] Setting Up NetworkServer...");
            Console.WriteLine("[Server] Set Listening port to: " + port);

#if DEBUG
            Console.WriteLine("DEBUG: [Server] Setting up Socket...");
#endif

            serverConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), "localhost", port);
            serverConnection.isServer = true;

            keyPairs = new ConcurrentDictionary<IPEndPoint, byte[]>();

            RegisterDefaultHandlers();

            StartServer();
        }

        protected override void AcceptConnectionCallback(IAsyncResult result)
        {
            Socket clientSocket = serverSocket.EndAccept(result);

#if DEBUG
            Console.WriteLine("DEBUG: [Server] New Client Connected!");
#endif

            IPEndPoint clientIPEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            string clientIP = clientIPEndPoint.Address.ToString();
            int clientPort = clientIPEndPoint.Port;

            NetworkConnectionToClient clientConnection = new NetworkConnectionToClient(clientSocket, clientIPEndPoint);
            clientConnections.TryAdd(clientIPEndPoint, clientConnection);

            clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);

            ConnectKeySuccessMessage message = new ConnectKeySuccessMessage();
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();

                keyPairs.TryAdd(clientIPEndPoint, aes.Key);

                message.length = aes.Key.Length;
                message.key = aes.Key;
            }

            SendKey(message, clientIPEndPoint);

            // Start Accepting new Clients Again
            serverSocket.BeginAccept(new AsyncCallback(AcceptConnectionCallback), null);
        }

        // Handle the received data sent by a connection
        void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                Socket clientSocket = (Socket)result.AsyncState;
                IPEndPoint clientIPEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;

                int received = clientSocket.EndReceive(result);

                byte[] dataBuffer = new byte[received];
                Array.Copy(globalBuffer, dataBuffer, received);

                byte[] key;
                if(keyPairs.TryGetValue(clientIPEndPoint, out key))
                {
                    dataBuffer = DecryptData(key, dataBuffer);

                    if (dataBuffer.Length > 0)
                    {
                        serverConnection.OnReceivedData(dataBuffer, clientIPEndPoint);
                        if (clientSocket.Connected)
                        {
                            clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Server] Error: Received databuffer with a size of 0 | Client is being disonnected!");

                        NetworkConnectionToClient conn;
                        clientConnections.TryRemove(clientIPEndPoint, out conn);

                        conn.Disconnect();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Server] Receive Exception: " + e);
            }

        }

        void RegisterDefaultHandlers()
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Server] Registering Default Handlers");
#endif

            RegisterHandler<DisconnectMessage>(DisconnectReceived, false);
            RegisterHandler<PongMessage>(PongReceived, false);
        }

        void PongReceived(PongMessage message, IPEndPoint endpoint)
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Server] Pong Recieved: " + endpoint.Address.ToString());
#endif
        }

        void DisconnectReceived(DisconnectMessage message, IPEndPoint endpoint)
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Server] Removing Disconnected Client: " + endpoint);
#endif

            NetworkConnectionToClient conn;
            clientConnections.TryRemove(endpoint, out conn);

            conn.Disconnect();
        }

        void SendKey<T>(T message, IPEndPoint ipEndPoint) where T: INetworkMessage
        {
            NetworkConnectionToClient conn = null;
            if (clientConnections.TryGetValue(ipEndPoint, out conn))
            {
                byte[] toSend = MessagePacker.Pack(message);

                NetworkDiagnostic.OnSend(message, toSend.Length);

                conn.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.socket);
            }
        }

        public override void Send<T>(T message, IPEndPoint ipEndPoint)
        {
            NetworkConnectionToClient conn = null;
            if(clientConnections.TryGetValue(ipEndPoint, out conn))
            {
                byte[] key;
                if(keyPairs.TryGetValue(ipEndPoint, out key))
                {
                    byte[] toSend = MessagePacker.Pack(message);

                    //Encrypt the message using the clients public key
                    toSend = EncryptData(key, toSend);

                    NetworkDiagnostic.OnSend(message, toSend.Length);

                    conn.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.socket);
                }
            }
        }

        public override void SendToAll<T>(T message)
        {
            if (clientConnections.Count <= 0)
                return;

            int count = 0;
            byte[] toSend = MessagePacker.Pack(message);

            foreach (KeyValuePair<IPEndPoint, NetworkConnectionToClient> conn in clientConnections)
            {
                byte[] key;
                if(keyPairs.TryGetValue(conn.Key, out key))
                {
#if DEBUG
                Console.WriteLine("DEBUG: [Server] Sending to: " + conn.Key.ToString());
#endif

                    //Encrypt the message using the clients public key
                    toSend = EncryptData(key, toSend);

                    count++;
                    conn.Value.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.Value.socket);
                }
            }

            NetworkDiagnostic.OnSend(message, toSend.Length * count);
        }

        byte[] EncryptData(byte[] encryptKey, byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        byte[] DecryptData(byte[] encryptKey, byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }
    }


}