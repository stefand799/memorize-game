namespace MemorizeGame.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }
        
        public Card(int id, string imagePath)
        {
            Id = id;
            ImagePath = imagePath;
            IsFlipped = false;
            IsMatched = false;
        }
    }
}