using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using CastleBridge.OnlineLibraries;

namespace CastleBridge.Server {
    public class Team {

        private Dictionary<string, DiamondPacket> Diamonds; //Team's diamonds
        private string TeamName; //Team's name
        private int Score; //Team's score

        /// <summary>
        /// Receives team name
        /// and creates a team
        /// </summary>
        /// <param name="teamName"></param>
        public Team(string teamName) {

            TeamName = teamName;
            Diamonds = new Dictionary<string, DiamondPacket>();

            Score = 0;

            //Initializes diamonds:
            InitDiamonds();
        }

        /// <summary>
        /// Initializes diamonds
        /// </summary>
        private void InitDiamonds() {

            for (int i = 1; i <= 3; i++) {
                string key = "diamond#" + TeamName + "#" + i;
                DiamondPacket diamond = new DiamondPacket(100, 350 + (i * 100), TeamName, key);
                AddDiamond(key, diamond);
            }
        }

        /// <summary>
        /// Receives diamond's key, diamond packet and applies in the diamonds dictionary with the key parameter 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="diamond"></param>
        public void UpdateDiamond(string key, DiamondPacket diamond) {

            lock (Diamonds) {
                Diamonds[key] = diamond;
            }
        }

        /// <summary>
        /// Get team name
        /// </summary>
        /// <returns></returns>
        public string GetTeamName() {
            return TeamName;
        }

        /// <summary>
        /// Add 1 score
        /// </summary>
        public void AddScore() {
            Score++;
        }

        /// <summary>
        /// Receives a diamond
        /// and adds it to the diamonds list
        /// </summary>
        /// <param name="diamond"></param>
        public void AddDiamond(string key, DiamondPacket diamond) {
            Diamonds.Add(key, diamond);
        }

        /// <summary>
        /// Get diamonds
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DiamondPacket> GetDiamonds() {
            return Diamonds;
        }

        /// <summary>
        /// Get team's score
        /// </summary>
        /// <returns></returns>
        public int GetScore() {
            return Score;
        }
    }
}
