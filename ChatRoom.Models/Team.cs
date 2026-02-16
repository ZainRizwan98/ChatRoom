using ChatRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Models
{
    public class Team
    {
        public TeamType Type { get; set; }
        public List<Agent> Agents { get; set; } = new();
        public ShiftType? CurrentShift { get; set; }
        public bool IsOverflowTeam { get; set; } = false;

        public int TotalCapacity
        {
            get
            {
                return (int)Agents
                    .Where(a => a.IsAvailable && !a.IsInShiftTransition)
                    .Sum(a => a.MaxConcurrentChats);
            }
        }

        public List<Agent> GetAvailableAgents()
        {
            return Agents
                .Where(a => a.CanAcceptNewChat())
                .OrderBy(a => a.Seniority) // Junior first, then Mid, Senior, TeamLead
                .ThenBy(a => a.ActiveChats.Count)
                .ToList();
        }
    }
}
