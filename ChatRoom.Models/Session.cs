using ChatRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Models
{
    public class Session
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastPollTime { get; set; }
        public SessionStatus Status { get; set; }
        public Agent? AssignedAgent { get; set; }
        public int MissedPolls { get; set; } = 0;
        public List<Message> Messages { get; set; } = new();

        public bool IsInactive()
        {
            // Mark inactive if 3 consecutive polls have been missed
            var timeSinceLastPoll = DateTime.UtcNow - LastPollTime;
            return timeSinceLastPoll.TotalSeconds >= 3;
        }

        public void RecordPoll()
        {
            LastPollTime = DateTime.UtcNow;
            MissedPolls = 0;
        }
    }
}
