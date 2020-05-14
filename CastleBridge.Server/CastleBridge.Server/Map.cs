using CastleBridge.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Text;

namespace CastleBridge.Server {
    public class Map {

        private List<MapEntityPacket> Entities;
        private Random Rnd;
        private int Width;
        private int Height;

        public Map() {
            Entities = new List<MapEntityPacket>();
            Rnd = new Random();
            Width = 10000;
            Height = 2000;
        }

        public void InitMap() {

            for (int i = 1; i <= 100; i++) {
                GenerateWorldEntity();
            }

            Console.WriteLine("<Server>: Map entities loaded successfully.");
        }

        private void GenerateWorldEntity() {

            int x = 0;
            int y = 0;
            MapEntityName entity = (MapEntityName)Rnd.Next(0, 5);

            x = Rnd.Next(150, Width - 150);
            y = Rnd.Next(400, Height - 150);

            MapEntityPacket MapEntityPacket = new MapEntityPacket();
            MapEntityPacket.X = x;
            MapEntityPacket.Y = y;
            MapEntityPacket.CurrentLocation = "Outside";
            MapEntityPacket.IsTouchable = !entity.Equals("Tree");
            MapEntityPacket.Name = entity.ToString();
            MapEntityPacket.Direction = "Left";
            Entities.Add(MapEntityPacket);
        }

        public List<MapEntityPacket> GetEntities() {
            return Entities;
        }
 

    }
}
