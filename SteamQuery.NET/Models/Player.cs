using System;

namespace SteamQuery.Models
{
    public sealed class Player
    {
        public byte Index { get; set; }
        public string Name { get; set; }
        public long Score { get; set; }

        private float _durationSecond;
        public float DurationSecond
        {
            get => _durationSecond;
            set
            {
                _durationSecond = value;
                DurationTimeSpan = TimeSpan.FromSeconds(value);
                Duration = DurationTimeSpan.ToString(@"hh\:mm\:ss");
            }
        }
        public TimeSpan DurationTimeSpan { get; set; }
        public string Duration { get; set; }
    }
}