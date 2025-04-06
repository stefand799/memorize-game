using CommunityToolkit.Mvvm.ComponentModel;

namespace MemorizeGame.Models
{
    public partial class Card : ObservableObject
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int PairId { get; set; } // Added to identify matching pairs
        
        [ObservableProperty]
        private bool _isFlipped;
        
        [ObservableProperty]
        private bool _isMatched;
        
        public Card(int id, string imagePath, int pairId)
        {
            Id = id;
            ImagePath = imagePath;
            PairId = pairId;
            _isFlipped = false;
            _isMatched = false;
        }
        
        // Default constructor for serialization
        public Card()
        {
            Id = 0;
            ImagePath = string.Empty;
            PairId = 0;
            _isFlipped = false;
            _isMatched = false;
        }
    }
}