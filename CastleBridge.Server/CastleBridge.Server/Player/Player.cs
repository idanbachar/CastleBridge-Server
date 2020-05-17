using CastleBridge.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CastleBridge.Server {
    public class Player {

        public PlayerPacket PlayerPacket; //Player's packet
        public TcpClient Client; //Player's client

        /// <summary>
        /// Receives player packet, client
        /// and creates player
        /// </summary>
        /// <param name="playerPacket"></param>
        /// <param name="client"></param>
        public Player(PlayerPacket playerPacket, TcpClient client) {

            PlayerPacket = playerPacket;
            Client = client;
        }
    }
}
