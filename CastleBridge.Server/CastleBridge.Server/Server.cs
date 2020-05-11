using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CastleBridge.OnlineLibraries;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CastleBridge.Server {
    public class Server {

        private TcpListener Listener;
        private Dictionary<string, Player> Players;
        private const int ThreadSleep = 50;

        public Server(string ip, int port) {

            Listener = new TcpListener(IPAddress.Parse(ip), port);
            Players = new Dictionary<string, Player>();
        }

        private void WaitForConnections() {

            NetworkStream netStream = null;

            while (true) {

                TcpClient connectedClient = Listener.AcceptTcpClient();

                netStream = connectedClient.GetStream();
                byte[] bytes = new byte[1024];
                netStream.Read(bytes, 0, bytes.Length);
                object obj = ByteArrayToObject(bytes);

                if (obj is PlayerPacket) {

                    PlayerPacket playerPacket = obj as PlayerPacket;

                    switch (playerPacket.PacketType) {
                        case PacketType.PlayerJoined:

                            if (!Players.ContainsKey(playerPacket.Name)) {
                                Players.Add(playerPacket.Name, new Player(playerPacket, connectedClient));
                                Console.WriteLine(playerPacket.Name + " the " + playerPacket.CharacterName + " has joined to the " + playerPacket.TeamName + " team!");

                                Console.WriteLine("Connected from ip: " + ((IPEndPoint)connectedClient.Client.RemoteEndPoint).Address.ToString());
                                new Thread(() => ReceiveData(connectedClient)).Start();
                            }
                            break;
                    }
                }
 
            }
        }


        public byte[] ObjectToByteArray<T>(T obj) {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public object ByteArrayToObject(byte[] arrBytes) {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();

            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);

            object obj = (object)binForm.Deserialize(memStream);

            return obj;
        }


        private void ReceiveData(TcpClient client) {

            NetworkStream netStream = null;

            while (true) {

                try {
                    netStream = client.GetStream();
                    byte[] bytes = new byte[1024];
                    netStream.Read(bytes, 0, bytes.Length);
                    object obj = ByteArrayToObject(bytes);

                    if (obj is PlayerPacket) {

                        PlayerPacket playerPacket = obj as PlayerPacket;

                        lock (Players) {

                            Players[playerPacket.Name].PlayerPacket = playerPacket;
                            Players[playerPacket.Name].Client = client;
                            SendToOtherClients(Players[playerPacket.Name].PlayerPacket);
                        }
                    }

                }
                catch(Exception e) {
                    Console.WriteLine(e.Message);
                }

                Thread.Sleep(ThreadSleep);
            }

        }

        private void SendToOtherClients(PlayerPacket currentPlayer) {

            NetworkStream netStream = null;

            try {

                foreach (KeyValuePair<string, Player> player in Players) {

                    if (player.Value.PlayerPacket.Name == currentPlayer.Name)
                        continue;

                    netStream = player.Value.Client.GetStream();
                    byte[] bytes = ObjectToByteArray(currentPlayer);
                    netStream.Write(bytes, 0, bytes.Length);
                    netStream.Flush();
                    Console.WriteLine("<Server>: Sending " + player.Value.PlayerPacket.Name + "'s data to other players..");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }

        }
 
        private void SendText(TcpClient client, string text) {

            NetworkStream netStream = client.GetStream();
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            netStream.Write(bytes, 0, bytes.Length);

        }

        public void Start() {

            Listener.Start();
            Console.WriteLine("Server started." +
                "\n" +
                "waiting for clients..");

          new Thread(WaitForConnections).Start();
 

           // new Thread(SendToOtherClients).Start();
        }

    }
}
