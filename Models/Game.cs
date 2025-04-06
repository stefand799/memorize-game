using System;
using System.Collections.Generic;

namespace MemorizeGame.Models
{
    public class Game
    {
        public string Username { get; set; } = string.Empty;
        public GameConfiguration Configuration { get; set; }
        public List<Card> Cards { get; set; } = new List<Card>();
        public DateTime SavedAt { get; set; }
        public bool IsCompleted { get; set; }
        
        public Game(string username, GameConfiguration configuration)
        {
            Username = username;
            Configuration = configuration;
            IsCompleted = false;
        }
        
        public int RemainingTime => Math.Max(0, Configuration.TotalTime - Configuration.ElapsedTime);
    }
}