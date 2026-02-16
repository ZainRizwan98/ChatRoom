using ChatRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Models
{
    public class Agent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Seniority Seniority { get; set; }
        public Team Team { get; set; }
        public List<Session> ActiveChats { get; set; } = new();
        public bool IsAvailable { get; set; } = true;
        public bool IsInShiftTransition { get; set; } = false;

        public int MaxConcurrentChats => (int)(10 * GetEfficiencyMultiplier());

        public double GetEfficiencyMultiplier()
        {
            return Seniority switch
            {
                Seniority.Junior => 0.4,
                Seniority.MidLevel => 0.6,
                Seniority.Senior => 0.8,
                Seniority.TeamLead => 0.5,
                _ => 0.4
            };
        }

        public int CurrentCapacity => MaxConcurrentChats - ActiveChats.Count;

        public bool CanAcceptNewChat()
        {
            return IsAvailable && !IsInShiftTransition && ActiveChats.Count < MaxConcurrentChats;
        }
    }
}
