using CastleBridge.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Text;

namespace CastleBridge.Server {
    public class Map {

        private Dictionary<string, MapEntityPacket> Entities;
        private Random Rnd;
        private int Width;
        private int Height;
        private int TotalItemsCounter;

        public Map() {
            Entities = new Dictionary<string, MapEntityPacket>();
            Rnd = new Random();
            Width = 10000;
            Height = 2000;
            TotalItemsCounter = 0;

            InitMap();
        }

        private void InitMap() {

            for (int i = 1; i <= 100; i++) {
                GenerateWorldEntity();
            }

            Console.WriteLine("<Server>: Map entities loaded successfully.");
        }

        private void GenerateWorldEntity() {

            TotalItemsCounter++;
            int x = 0;
            int y = 0;
            MapEntityName entity = (MapEntityName)Rnd.Next(0, 5);

            x = Rnd.Next(150, Width - 150);
            y = Rnd.Next(400, Height - 150);

            string key = "Entity#" + TotalItemsCounter;

            MapEntityPacket MapEntityPacket = new MapEntityPacket();
            MapEntityPacket.X = x;
            MapEntityPacket.Y = y;
            MapEntityPacket.CurrentLocation = "Outside";
            MapEntityPacket.IsTouchable = !entity.Equals("Tree");
            MapEntityPacket.Name = entity.ToString();
            MapEntityPacket.Direction = "Left";
            MapEntityPacket.IsActive = true;
            MapEntityPacket.Key = key;
            Entities.Add(key, MapEntityPacket);
        }


        public void RemoveEntity(string key) {
            if (Entities.ContainsKey(key)) {

                lock (Entities) {
                    Entities.Remove(key);
                }
            }
        }

        public Dictionary<string, MapEntityPacket> GetEntities() {
            return Entities;
        }
    }
}
