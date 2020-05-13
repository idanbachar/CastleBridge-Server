using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CastleBridge.OnlineLibraries;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net.Http.Headers;

namespace CastleBridge.Server {
    public class Server {

        private TcpListener Listener;
        private Dictionary<string, Player> Players;
        private List<MapEntityPacket> MapEntities;
        private const int ThreadSleep = 100;
        private Random Rnd;

        public Server(string ip, int port) {

            Listener = new TcpListener(IPAddress.Parse(ip), port);
            Players = new Dictionary<string, Player>();
            MapEntities = new List<MapEntityPacket>();
            Rnd = new Random();
            InitMap();
        }

        private void WaitForConnections() {
 
            while (true) {

                TcpClient connectedClient = Listener.AcceptTcpClient();
                new Thread(() => SendMapEntities(connectedClient)).Start();
                new Thread(() => ReceiveData(connectedClient)).Start();
            }
        }

        private void InitMap() {
            for (int i = 1; i <= 100; i++) {
                GenerateWorldEntity();
            }
        }

        private void GenerateWorldEntity() {

            int x = Rnd.Next(0, 10000);
            int y = Rnd.Next(400, 2000);
            MapEntityName entity = (MapEntityName)Rnd.Next(0, 5);

            MapEntityPacket MapEntityPacket = new MapEntityPacket();
            MapEntityPacket.X = x;
            MapEntityPacket.Y = y;
            MapEntityPacket.CurrentLocation = "Outside";
            MapEntityPacket.IsTouchable = !entity.Equals("Tree");
            MapEntityPacket.Name = entity.ToString();
            MapEntityPacket.Direction = "Left";
            MapEntities.Add(MapEntityPacket);
        }

        private void SendMapEntities(TcpClient client) {
            while (true) {
                NetworkStream netStream = null;
                byte[] bytes = new byte[1024];
                try {
                    foreach (MapEntityPacket mapEntity in MapEntities) {

                        netStream = client.GetStream();
                        bytes = ObjectToByteArray(mapEntity);
                        netStream.Write(bytes, 0, bytes.Length);
                        Console.WriteLine("<Server>: Sending " + mapEntity.Name + " to player..");

                        Thread.Sleep(ThreadSleep);
                    }


                    netStream = client.GetStream();
                    bytes = Encoding.ASCII.GetBytes("Completed Map Entities");
                    netStream.Write(bytes, 0, bytes.Length);

                    Console.WriteLine("Completed sending map entities to player.");

                    break;

                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private byte[] ObjectToByteArray<T>(T obj) {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private object ByteArrayToObject(byte[] arrBytes) {
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
                    object obj = null;

                    try {
                        obj = ByteArrayToObject(bytes);

                        if (obj is PlayerPacket) {

                            PlayerPacket playerPacket = obj as PlayerPacket;
                            
                            lock (Players) {
                                if (!Players.ContainsKey(playerPacket.Name)) {
                                    Players.Add(playerPacket.Name, new Player(playerPacket, client));
                                    Console.WriteLine(playerPacket.Name + " the " + playerPacket.CharacterName + " has joined to the " + playerPacket.TeamName + " team!");
                                }
                                else {
                                    Players[playerPacket.Name].PlayerPacket = playerPacket;
                                }
                            }
                            SendPlayerDataToOtherPlayers(playerPacket.Name);
                        }
                    }
                    catch(Exception e) {
                        Console.WriteLine(e.Message);

                        string data = Encoding.ASCII.GetString(bytes).Split('\0')[0];
                        Console.WriteLine("<Client>: " + data);

                    }

                }
                catch(Exception e) {
                    Console.WriteLine(e.Message);
                }

                Thread.Sleep(ThreadSleep);
            }

        }

        private void SendPlayerDataToOtherPlayers(string playerName) {
 
            foreach (KeyValuePair<string, Player> player in Players) {
                try {

                    if (player.Value.PlayerPacket.Name == playerName || !player.Value.PlayerPacket.IsAllMapEntitiesLoaded)
                        continue;

                    NetworkStream netStream = player.Value.Client.GetStream();
                    byte[] bytes = ObjectToByteArray(Players[playerName].PlayerPacket);
                    netStream.Write(bytes, 0, bytes.Length);

                    //Console.WriteLine("<Server>: Sending " + playerName + "'s data to other players..");

                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }


            }

        }
 

        public void Start() {

            Listener.Start();
            Console.WriteLine("Server started.");
            Console.WriteLine("Waiting for players...");

            WaitForConnections();
 
        }

    }
}
