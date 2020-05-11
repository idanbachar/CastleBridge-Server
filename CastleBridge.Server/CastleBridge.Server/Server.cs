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
        private const int ThreadSleep = 100;

        public Server(string ip, int port) {

            Listener = new TcpListener(IPAddress.Parse(ip), port);
            Players = new Dictionary<string, Player>();
        }

        private void WaitForConnections() {
 
            while (true) {

                TcpClient connectedClient = Listener.AcceptTcpClient();
                new Thread(() => ReceiveData(connectedClient)).Start();
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
                    object obj = ByteArrayToObject(bytes);


                    if (obj is PlayerPacket) {

                        PlayerPacket playerPacket = obj as PlayerPacket;

                        lock (Players) {
                            if (!Players.ContainsKey(playerPacket.Name)) {
                                Players.Add(playerPacket.Name, new Player(playerPacket, client));
                                Console.WriteLine(playerPacket.Name + " the " + playerPacket.CharacterName + " has joined to the " + playerPacket.TeamName + " team!");
                            }
                            else {
                                Players[playerPacket.Name].PlayerPacket.CharacterName = playerPacket.CharacterName;
                                Players[playerPacket.Name].PlayerPacket.Name = playerPacket.Name;
                                Players[playerPacket.Name].PlayerPacket.Direction = playerPacket.Direction;
                                Players[playerPacket.Name].PlayerPacket.PacketType = playerPacket.PacketType;
                                Players[playerPacket.Name].PlayerPacket.PlayerState = playerPacket.PlayerState;
                                Players[playerPacket.Name].PlayerPacket.CurrentSpeed = playerPacket.CurrentSpeed;
                                Players[playerPacket.Name].PlayerPacket.CurrentLocation = playerPacket.CurrentLocation;
                                Players[playerPacket.Name].PlayerPacket.Rectangle = new RectanglePacket(playerPacket.Rectangle.X, playerPacket.Rectangle.Y, playerPacket.Rectangle.Width, playerPacket.Rectangle.Height);
                            }
                        }
                        SendToOtherClients(playerPacket.Name);
                    }

                }
                catch(Exception e) {
                    Console.WriteLine(e.Message);
                }

                Thread.Sleep(ThreadSleep);
            }

        }

        private void SendToOtherClients(string playerName) {
 
            foreach (KeyValuePair<string, Player> player in Players) {
                try {

                    if (player.Value.PlayerPacket.Name == playerName)
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
