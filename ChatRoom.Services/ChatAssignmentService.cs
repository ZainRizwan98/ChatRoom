using ChatRoom.Models;
using ChatRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Services
{
    public class ChatAssignmentService
    {
        private readonly TeamManagementService _teamService;
        private readonly QueueService _queueService;

        public ChatAssignmentService(
            TeamManagementService teamService,
            QueueService queueService)
        {
            _teamService = teamService;
            _queueService = queueService;
        }

        public async Task<bool> TryAssignNextChat()
        {
            // Try to assign from main queue first
            var session = _queueService.DequeueNextSession(fromOverflow: false);

            // If main queue is empty, try overflow
            if (session == null)
            {
                session = _queueService.DequeueNextSession(fromOverflow: true);
            }

            if (session == null)
            {
                return false;
            }

            // Get available agents using round-robin (junior first)
            var agent = GetNextAvailableAgent(session);

            if (agent == null)
            {
                return false;
            }
            // Track per-shift total as well
            agent.TotalChatsDuringShift++;
            session.AssignedAgent = agent;
            session.Status = SessionStatus.Active;
            agent.ActiveChats.Add(session);
            return true;
        }

        private Agent? GetNextAvailableAgent(Session session)
        {
            var activeTeams = _teamService.GetActiveTeams();
            int currentQueuesUsed = _teamService.GetCurrentActiveChatsCount(activeTeams);
            // Include ocerflow queue if needed
            if (_teamService.IsOfficeHours() && currentQueuesUsed >= activeTeams.First().TotalCapacity)
            {
                var overflowTeam = _teamService.GetOverflowTeam();
                activeTeams.Add(overflowTeam);
            }

            var availableAgents = activeTeams
                .SelectMany(t => t.GetAvailableAgents())
                .ToList();

            if (!availableAgents.Any())
            {
                return null;
            }

            // assign to junior most first and that too with lower number of chats
            var agent = availableAgents
                .OrderBy(a => GetSeniorityPriority(a.Seniority))
                .ThenBy(a => a.ActiveChats.Count).ThenBy(a => a.TotalChatsDuringShift)
                .FirstOrDefault();

            return agent;
        }

        private int GetSeniorityPriority(Seniority seniority)
        {
            return seniority switch
            {
                Seniority.Junior => 1,
                Seniority.MidLevel => 2,
                Seniority.Senior => 3,
                Seniority.TeamLead => 4,
            };
        }

        public void CompleteChat(Guid sessionId)
        {
            var session = _queueService.GetSession(sessionId);

            if (session == null)
            {
                return;
            }

            if (session.AssignedAgent != null)
            {
                session.AssignedAgent.ActiveChats.Remove(session);
            }

            session.Status = SessionStatus.Completed;
            _queueService.RemoveSession(sessionId);
        }

        public void MarkChatInactive(Guid sessionId)
        {
            var session = _queueService.GetSession(sessionId);

            if (session == null)
            {
                return;
            }

            if (session.AssignedAgent != null)
            {
                session.AssignedAgent.ActiveChats.Remove(session);
            }

            session.Status = SessionStatus.Inactive;
            _queueService.RemoveSession(sessionId);
        }

        public Dictionary<string, object> GetSystemStatus()
        {
            var activeTeams = _teamService.GetActiveTeams();
            var allAgents = _teamService.GetAllAgents();

            return new Dictionary<string, object>
            {
                ["mainQueueCount"] = _queueService.MainQueueCount,
                ["overflowQueueCount"] = _queueService.OverflowQueueCount,
                ["activeSessionCount"] = _queueService.ActiveSessionCount,
                ["totalCapacity"] = _teamService.CalculateTotalCapacity(),
                ["maxQueueLength"] = _teamService.CalculateMaxQueueLength(),
                ["currentShift"] = _teamService.GetCurrentShift().ToString(),
                ["isOfficeHours"] = _teamService.IsOfficeHours(),
                ["agentStatus"] = allAgents.Select(a => new
                {
                    name = a.Name,
                    seniority = a.Seniority.ToString(),
                    activeChats = a.ActiveChats.Count,
                    maxChats = a.MaxConcurrentChats,
                    isAvailable = a.IsAvailable,
                    isInTransition = a.IsInShiftTransition
                }).ToList()
            };
        }

        // Called when a shift transition is detected. Returns a summary of totals for the ended shift
        public void HandleShiftTransition(/*ShiftType endedShift, ShiftType startedShift*/)
        {
            var allAgents = _teamService.GetAllAgents();

            var perAgent = allAgents.Select(a => a.TotalChatsDuringShift = 0).ToList();
        }
    }
}
