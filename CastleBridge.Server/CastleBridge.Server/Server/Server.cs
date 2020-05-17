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

        private TcpListener Listener; //Listener
        private Dictionary<string, Player> Players; //Connected players
        private const int ThreadSleep = 100; //Thread sleep time
        private Map Map; //Map

        /// <summary>
        /// Receives ip, port
        /// and creates a server
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public Server(string ip, int port) {

            Listener = new TcpListener(IPAddress.Parse(ip), port);
            Players = new Dictionary<string, Player>();
            Map = new Map();
        }

        /// <summary>
        /// Start listening for new connections
        /// </summary>
        public void Start() {

            Listener.Start();
            Console.WriteLine("<Server>: Server started.");
            new Thread(WaitForConnections).Start();
        }

        /// <summary>
        /// Waiting for connections thread
        /// when player connects, two threads start to work: 
        /// 1. Send map's entities to player
        /// 2. Receive data from player
        /// </summary>
        private void WaitForConnections() {
 
            while (true) {
                Console.WriteLine("<Server>: Waiting for players...");
                TcpClient connectedClient = Listener.AcceptTcpClient();
                new Thread(() => SendMapEntities(connectedClient)).Start();
                new Thread(() => ReceiveData(connectedClient)).Start();
            }
        }

        /// <summary>
        /// Sends map's entities into connected client
        /// </summary>
        /// <param name="client"></param>
        private void SendMapEntities(TcpClient client) {

            //Run always:
            while (true) {

                NetworkStream netStream = null;
                byte[] bytes = new byte[1024];

                //Try to send a string of command into array of bytes and send it to connected client:
                try {

                    //Sends total count of map's entities into client:
                    netStream = client.GetStream();
                    bytes = Encoding.ASCII.GetBytes(Map.GetEntities().Count + "|map_entities_count");
                    netStream.Write(bytes, 0, bytes.Length);

                    //Run on each map's entities:
                    foreach (KeyValuePair<string, MapEntityPacket> mapEntity in Map.GetEntities()) {

                        //Convert map entity packet's object into array of bytes and send it to server:
                        netStream = client.GetStream();
                        bytes = ObjectToByteArray(mapEntity.Value);
                        netStream.Write(bytes, 0, bytes.Length);

                        Console.WriteLine("<Server>: Sending " + mapEntity.Value.Name + " to player..");

                        //Start thread sleep:
                        Thread.Sleep(ThreadSleep);
                    }

                    //After all map's entities sent to the connected client, sends completed map entities command into client:
                    netStream = client.GetStream();
                    bytes = Encoding.ASCII.GetBytes("Completed Map Entities");
                    netStream.Write(bytes, 0, bytes.Length);

                    Console.WriteLine("<Server>: Completed sending map entities to player.");
                    break;
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Receives data from client
        /// </summary>
        /// <param name="client"></param>
        private void ReceiveData(TcpClient client) {

            //Run always:
            while (true) {

                //Try to read stream data from connected client:
                try {

                    NetworkStream netStream = client.GetStream();
                    byte[] bytes = new byte[1024];
                    netStream.Read(bytes, 0, bytes.Length);
                    object obj = null;

                    //Try to convert received array of bytes data into packet's object:
                    try {
                        obj = ByteArrayToObject(bytes);

                        //Checks if received obj is type of player packet:
                        if (obj is PlayerPacket) {

                            PlayerPacket playerPacket = obj as PlayerPacket;

                            //Using lock function to avoid multi task crash:
                            lock (Players) {

                                //Checks if this is the first time of receiving current player's data (never connected before):
                                if (!Players.ContainsKey(playerPacket.Name)) {

                                    //Add new player to the connected players's dictionary:
                                    Players.Add(playerPacket.Name, new Player(playerPacket, client));

                                    Console.WriteLine("<Server>: " + playerPacket.Name + " the " + playerPacket.CharacterName + " has joined to the " + playerPacket.TeamName + " team!");
                                }
                                else { //If this is not the first time receving current player's data (connected before):

                                    //Sets all received player packet's vars into current player's vars in the players's dictionary:
                                    Players[playerPacket.Name].PlayerPacket = playerPacket;
                                }
                            }

                            //Sends current received player's data to other connected players by using current received player's name:
                            SendPlayerDataToOtherPlayers(playerPacket.Name);
                        }
                        else if(obj is DiamondPacket) {

                            DiamondPacket diamondPacket = obj as DiamondPacket;
                            string diamondTeam = diamondPacket.TeamName;
                            string diamondKey = diamondPacket.Key;
                            
                            if (Map.GetTeams()[diamondTeam].GetDiamonds().ContainsKey(diamondKey)) {
                                Map.GetTeams()[diamondTeam].UpdateDiamond(diamondKey, diamondPacket);

                                SendDiamondsChangesToOtherPlayers(diamondKey, diamondTeam, diamondPacket.Owner);
                            }
                        }
                    }
                    //Checks if failed to convert received array of bytes into an object, because of the received data is string:
                    catch (Exception e) {
                        Console.WriteLine(e.Message);

                        //Get data and convert it from array of bytes into string:
                        string data = Encoding.ASCII.GetString(bytes).Split('\0')[0];

                        //Checks if data contains 'Remove Entity..' command:
                        if (data.IndexOf("Remove Entity") != -1) {

                            //Get entity's key:
                            string key = data.Split('_')[1];

                            //Get player's name:
                            string playerName = data.Split('_')[2];

                            //Checks if exists entity with the received key in the dictionary:
                            if (Map.GetEntities().ContainsKey(key)) {

                                //Remove entity from map:
                                Map.RemoveEntity(key);

                                Console.WriteLine("<Server>: entity has removed. there are " + Map.GetEntities().Count + " more entities.");

                                //Sends entity removed update to other connected players by using current received (key, player's name):
                                SendMapEntitiesChangesToOtherPlayers(key, playerName);
                            }

                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }

                //Start thread sleep:
                Thread.Sleep(ThreadSleep);
            }
        }

        /// <summary>
        /// Receives entity's key, player's name
        /// and sends entity changes update to other connected players
        /// </summary>
        /// <param name="entityKey"></param>
        /// <param name="playerName"></param>
        private void SendMapEntitiesChangesToOtherPlayers(string entityKey, string playerName) {

            //Run on each connected player:
            foreach (KeyValuePair<string, Player> player in Players) {
                try {

                    //Checks if received player's name is equals to current player, if true then skip, else keep going:
                    if (player.Value.PlayerPacket.Name == playerName)
                        continue;

                    //Sends entity changes update:
                    NetworkStream netStream = player.Value.Client.GetStream();
                    byte[] bytes = Encoding.ASCII.GetBytes("Remove Entity_" + entityKey);
                    netStream.Write(bytes, 0, bytes.Length);

                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Receives diamond's key, diamond's team, player's name
        /// and sends diamond changes update to other connected players
        /// </summary>
        /// <param name="diamondKey"></param>
        /// <param name="team"></param>
        /// <param name="playerName"></param>
        private void SendDiamondsChangesToOtherPlayers(string diamondKey, string team, string playerName) {

            //Run on each connected player:
            foreach (KeyValuePair<string, Player> player in Players) {
                try {

                    //Checks if received player's name is equals to current player, if true then skip, else keep going:
                    if (player.Value.PlayerPacket.Name == playerName)
                        continue;

                    //Get diamond by received parameters:
                    DiamondPacket diamond = Map.GetTeams()[team].GetDiamonds()[diamondKey];

                    //Sends diamond changes update:
                    NetworkStream netStream = player.Value.Client.GetStream();
                    byte[] bytes = ObjectToByteArray(diamond);
                    netStream.Write(bytes, 0, bytes.Length);

                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Receives player's name
        /// and sends player's data to other connected players
        /// </summary>
        /// <param name="playerName"></param>
        private void SendPlayerDataToOtherPlayers(string playerName) {

            //Run on each connected player:
            foreach (KeyValuePair<string, Player> player in Players) {
                try {

                    //Checks if received player's name is equals to current player or current player is still downloading map's data, if true then skip, else keep going:
                    if (player.Value.PlayerPacket.Name == playerName || !player.Value.PlayerPacket.IsAllMapEntitiesLoaded)
                        continue;


                    //Try to convert player packet's object into array of bytes and send it to client:
                    NetworkStream netStream = player.Value.Client.GetStream();
                    byte[] bytes = ObjectToByteArray(Players[playerName].PlayerPacket);
                    netStream.Write(bytes, 0, bytes.Length);
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Receives an object
        /// and returns it as array of bytes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        private byte[] ObjectToByteArray<T>(T obj) {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Receives array of bytes
        /// and returns it as an object
        /// </summary>
        /// <param name="arrBytes"></param>
        /// <returns></returns>
        private object ByteArrayToObject(byte[] arrBytes) {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();

            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);

            object obj = (object)binForm.Deserialize(memStream);

            return obj;
        }
    }
}
