using CastleBridge.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace CastleBridge.Server {
    public class Player {

        public PlayerPacket PlayerPacket;
        public TcpClient Client;
        public Player(PlayerPacket playerPacket, TcpClient client) {

            PlayerPacket = playerPacket;
            Client = client;

        }
    }
}
