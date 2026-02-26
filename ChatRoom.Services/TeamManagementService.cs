using ChatRoom.Models;
using ChatRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Services
{
    public class TeamManagementService
    {
        private readonly List<Team> _teams = new();

        public TeamManagementService()
        {
            InitializeTeams();
        }

        private void InitializeTeams()
        {
            // Team A: 1x team lead, 2x mid-level, 1x junior
            var teamA = new Team
            {
                Type = TeamType.TeamA,
                CurrentShift = ShiftType.Morning,
                Agents = new List<Agent>
                {
                    new Agent { Id = Guid.NewGuid(), Name = "TeamLead A1", Seniority = Seniority.TeamLead },
                    new Agent { Id = Guid.NewGuid(), Name = "MidLevel A1", Seniority = Seniority.MidLevel },
                    new Agent { Id = Guid.NewGuid(), Name = "MidLevel A2", Seniority = Seniority.MidLevel },
                    new Agent { Id = Guid.NewGuid(), Name = "Junior A1", Seniority = Seniority.Junior },
                   // new Agent { Id = Guid.NewGuid(), Name = "Junior A2", Seniority = Seniority.Junior }
                }
            };

            // Team B: 1x senior, 1x mid-level, 2x junior
            var teamB = new Team
            {
                Type = TeamType.TeamB,
                CurrentShift = ShiftType.Afternoon,
                Agents = new List<Agent>
                {
                    new Agent { Id = Guid.NewGuid(), Name = "Senior B1", Seniority = Seniority.Senior },
                    new Agent { Id = Guid.NewGuid(), Name = "MidLevel B1", Seniority = Seniority.MidLevel },
                    new Agent { Id = Guid.NewGuid(), Name = "Junior B1", Seniority = Seniority.Junior },
                    new Agent { Id = Guid.NewGuid(), Name = "Junior B2", Seniority = Seniority.Junior },
                    //new Agent { Id = Guid.NewGuid(), Name = "MidLevel B2", Seniority = Seniority.MidLevel },
                   // new Agent { Id = Guid.NewGuid(), Name = "Senior B2", Seniority = Seniority.Senior }
                }
            };

            // Team C: 2x mid-level (night shift team)
            var teamC = new Team
            {
                Type = TeamType.TeamC,
                CurrentShift = ShiftType.Night,
                Agents = new List<Agent>
                {
                    new Agent { Id = Guid.NewGuid(), Name = "MidLevel C1", Seniority = Seniority.MidLevel },
                    new Agent { Id = Guid.NewGuid(), Name = "MidLevel C2", Seniority = Seniority.MidLevel },
                   // new Agent { Id = Guid.NewGuid(), Name = "TeamLead C1", Seniority = Seniority.MidLevel },
                   // new Agent { Id = Guid.NewGuid(), Name = "MidLevel C3", Seniority = Seniority.MidLevel }
                }
            };

            // Overflow team: x6 considered Junior
            var overflowTeam = new Team
            {
                Type = TeamType.Overflow,
                IsOverflowTeam = true,
                Agents = new List<Agent>
                {
                    new Agent { Id = Guid.NewGuid(), Name = "Overflow 1", Seniority = Seniority.Junior },
                    new Agent { Id = Guid.NewGuid(), Name = "Overflow 2", Seniority = Seniority.Junior },
                    new Agent { Id = Guid.NewGuid(), Name = "Overflow 3", Seniority = Seniority.Junior },
                    new Agent { Id = Guid.NewGuid(), Name = "Overflow 4", Seniority = Seniority.Junior },
                    new Agent { Id = Guid.NewGuid(), Name = "Overflow 5", Seniority = Seniority.Junior },
                    new Agent { Id = Guid.NewGuid(), Name = "Overflow 6", Seniority = Seniority.Junior }
                }
            };

            foreach (var team in new[] { teamA, teamB, teamC, overflowTeam })
            {
                foreach (var agent in team.Agents)
                {
                    agent.Team = team;
                }
            }

            _teams.AddRange(new[] { teamA, teamB, teamC, overflowTeam });
        }

        public List<Team> GetActiveTeams()
        {
            var currentShift = GetCurrentShift();
            return _teams
                .Where(t => !t.IsOverflowTeam && t.CurrentShift == currentShift)
                .ToList();
        }

        public Team GetOverflowTeam()
        {
            return _teams.First(t => t.IsOverflowTeam);
        }

        public ShiftType GetCurrentShift()
        {
            var hour = DateTime.UtcNow.Hour;

            // Morning: 8am - 4pm
            if (hour >= 8 && hour < 16)
                return ShiftType.Morning;

            // Afternoon: 4pm - 12am
            if (hour >= 16 && hour < 24)
                return ShiftType.Afternoon;

            // Night: 12am - 8am
            return ShiftType.Night;
        }

        public int GetCurrentActiveChatsCount(List<Team> teams)
        {
            int totalCount = 0;
            foreach (var team in teams)
            {
                totalCount = totalCount + team.Agents.Sum(y => y.ActiveChats.Count);
            }
            return totalCount;
        }

        public bool IsOfficeHours()
        {
            var hour = DateTime.UtcNow.Hour;
            var dayOfWeek = DateTime.UtcNow.DayOfWeek;

            // Office hours: Monday-Friday, 8am-6pm
            return dayOfWeek != DayOfWeek.Saturday
                && dayOfWeek != DayOfWeek.Sunday
                && hour >= 8
                && hour < 18;
        }

        public void MarkAgentInShiftTransition(Agent agent, bool inTransition)
        {
            agent.IsInShiftTransition = inTransition;
        }

        public List<Agent> GetAllAgents()
        {
            return _teams.SelectMany(t => t.Agents).ToList();
        }

        public Agent? GetAgent(Guid agentId)
        {
            return _teams.SelectMany(t => t.Agents).FirstOrDefault(a => a.Id == agentId);
        }

        public int CalculateTotalCapacity()
        {
            var activeTeams = GetActiveTeams();
            return activeTeams.Sum(t => t.TotalCapacity);
        }
        public int CalculateOverflowCapacity()
        {
            var activeTeams = _teams.Where(x => x.IsOverflowTeam);
            return (int)(activeTeams.Sum(t => t.TotalCapacity) * 1.5);
        }

        public int CalculateMaxQueueLength()
        {
            return (int)(CalculateTotalCapacity() * 1.5);
        }
    }
}
