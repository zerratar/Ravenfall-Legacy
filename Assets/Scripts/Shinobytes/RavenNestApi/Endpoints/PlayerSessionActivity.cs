
using System;

namespace RavenNest.SDK.Endpoints
{
    public class PlayerSessionActivity
    {
        public Guid CharacterId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
        public TimeSpan AvgResponseTime { get; set; }
        public int ResponseStreak { get; set; }
        public int MaxResponseStreak { get; set; }
        public int TotalTriggerCount { get; set; }
        public int TotalInputCount { get; set; }
        public int TripCount { get; set; }

        public bool Tripped { get; set; }
    }
}