using System;
using System.Collections.Generic;

namespace MemorizeGame.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        
        // Constructor
        public User(string username, string imagePath)
        {
            Username = username;
            ImagePath = imagePath;
            GamesPlayed = 0;
            GamesWon = 0;
        }
    }
}