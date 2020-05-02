namespace SteamQuery.Models
{
    public sealed class Player
    {
        public byte Index { get; set; }
        public string Name { get; set; }
        public long Score { get; set; }
        public float Duration { get; set; }
    }
}