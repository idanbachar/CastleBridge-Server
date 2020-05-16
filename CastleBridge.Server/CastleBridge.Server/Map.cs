using CastleBridge.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Text;

namespace CastleBridge.Server {
    public class Map {

        private Dictionary<string, MapEntityPacket> Entities; //Map's entities
        private Random Rnd;
        private int Width; //Map's width
        private int Height; //Map's height
        private int TotalItemsCounter; //Map's total items counter

        /// <summary>
        /// Creates a map
        /// </summary>
        public Map() {

            Entities = new Dictionary<string, MapEntityPacket>();
            Rnd = new Random();
            Width = 10000;
            Height = 2000;
            TotalItemsCounter = 0;

            //Initializes map:
            InitMap();
        }

        /// <summary>
        /// Initializes map
        /// </summary>
        private void InitMap() {

            //Generate 100 world entities:
            for (int i = 1; i <= 100; i++)
                GenerateWorldEntity();
        }

        /// <summary>
        /// Generates randomized world entity
        /// </summary>
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


        /// <summary>
        /// Receives entity's key and removes it from dictionary
        /// </summary>
        /// <param name="key"></param>
        public void RemoveEntity(string key) {

            //Remove entity only if entities dictionary contains received key:
            if (Entities.ContainsKey(key)) {
                lock (Entities) {
                    Entities.Remove(key);
                }
            }
        }

        /// <summary>
        /// Get entities
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, MapEntityPacket> GetEntities() {
            return Entities;
        }
    }
}
