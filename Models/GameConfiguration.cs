namespace MemorizeGame.Models
{
    public enum GameCategory
    {
        Category1,
        Category2,
        Category3
    }
    
    public enum GameMode
    {
        Standard,  // 4x4
        Custom     // User-defined dimensions
    }
    
    public class GameConfiguration
    {
        public GameCategory Category { get; set; }
        public GameMode Mode { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int TotalTime { get; set; }  // In seconds
        public int ElapsedTime { get; set; } // In seconds
        
        // For standard 4x4 game
        public GameConfiguration()
        {
            Category = GameCategory.Category1;
            Mode = GameMode.Standard;
            Rows = 4;
            Columns = 4;
            TotalTime = 120; // 2 minutes default
            ElapsedTime = 0;
        }
        
        // For custom game
        public GameConfiguration(GameCategory category, int rows, int columns, int totalTime)
        {
            Category = category;
            Mode = GameMode.Custom;
            Rows = rows;
            Columns = columns;
            TotalTime = totalTime;
            ElapsedTime = 0;
        }
    }
}