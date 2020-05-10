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


        public Server(int port) {

            Listener = new TcpListener(IPAddress.Any, port);
            Players = new Dictionary<string, Player>();
        }

        private void WaitForConnections() {
            while (true) {

                TcpClient connectedClient = Listener.AcceptTcpClient();
                SendText(connectedClient, "welcome to server");

                new Thread(() => ReceiveData(connectedClient)).Start();
                new Thread(SendData).Start();


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

            while (true) {

                try {
                    NetworkStream netStream = client.GetStream();
                    byte[] bytes = new byte[1024];
                    netStream.Read(bytes, 0, bytes.Length);
                    object obj = ByteArrayToObject(bytes);

                    if(obj is PlayerPacket) {

                        PlayerPacket playerPacket = obj as PlayerPacket;
                        switch (playerPacket.PacketType) {
                            case PacketType.PlayerJoined:

                                Players.Add(playerPacket.Name, new Player(playerPacket, client));
                                Console.WriteLine(playerPacket.Name + " the " + playerPacket.CharacterName + " has joined to the " + playerPacket.TeamName + " team!");

                                break;
                            case PacketType.PlayerData:
                                Players[playerPacket.Name].PlayerPacket = playerPacket;
                                Console.WriteLine("<Server>: Receiving data from " + playerPacket.Name);
                                break;
                        }

 
                    }
                }catch(Exception e) {
                    Console.WriteLine(e.Message);
                }

                Thread.Sleep(100);
            }

        }

        private void SendData() {

            while (true) {

                try {

                    foreach (KeyValuePair<string, Player> player in Players) {
                        NetworkStream netStream = player.Value.Client.GetStream();
                        byte[] bytes = ObjectToByteArray(player.Value.PlayerPacket);
                        netStream.Write(bytes, 0, bytes.Length);
                        Console.WriteLine("<Server>: Sending " + player.Value.PlayerPacket.Name + "'s data to other players..");
                    }
                }
                catch (Exception e) {

                }

                Thread.Sleep(500);

            }

        }

        private void SendToOtherClients(TcpClient client) {

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

            Thread a = new Thread(WaitForConnections);
            a.Start();
        }

    }
}
