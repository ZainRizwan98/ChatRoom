using ChatRoom.Models;
using ChatRoom.Models.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Services
{
    public class QueueService
    {
        private readonly ConcurrentQueue<Session> _mainQueue = new();
        private readonly ConcurrentQueue<Session> _overflowQueue = new();
        private readonly ConcurrentDictionary<Guid, Session> _activeSessions = new();
        private readonly TeamManagementService _teamService;

        public QueueService(TeamManagementService teamService)
        {
            _teamService = teamService;
        }

        public int MainQueueCount => _mainQueue.Count;
        public int OverflowQueueCount => _overflowQueue.Count;
        public int ActiveSessionCount => _activeSessions.Count;

        public async Task<(bool Success, Session? Session, string? ErrorMessage)> EnqueueChatRequest(string userId)
        {
            var currentTeams = _teamService.GetActiveTeams();
            var totalCapacity = currentTeams.Sum(t => t.TotalCapacity);
            var maxQueueLength = (int)(totalCapacity * 1.5);

            // Check if main queue is full
            if (_mainQueue.Count >= maxQueueLength)
            {
                // Check if we're in office hours and can use overflow
                if (_teamService.IsOfficeHours())
                {
                    var overflowTeam = _teamService.GetOverflowTeam();
                    var overflowMaxQueue = (int)(overflowTeam.TotalCapacity * 1.5);

                    if (_overflowQueue.Count >= overflowMaxQueue)
                    {
                        return (false, null, "No agents available. Both queues are full. Please try again later.");
                    }

                    // Add to overflow queue
                    var overflowSession = CreateChatSession(userId, isOverflow: true);
                    _overflowQueue.Enqueue(overflowSession);
                    _activeSessions.TryAdd(overflowSession.Id, overflowSession);
                    return (true, overflowSession, null);
                }
                else
                {
                    return (false, null, "No agents available at the moment. Please try again later.");
                }
            }

            // Add to main queue
            var session = CreateChatSession(userId, isOverflow: false);
            _mainQueue.Enqueue(session);
            _activeSessions.TryAdd(session.Id, session);
            return (true, session, null);
        }

        private Session CreateChatSession(string userId, bool isOverflow)
        {
            return new Session
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastPollTime = DateTime.UtcNow,
                Status = SessionStatus.Queued
            };
        }

        public Session? GetSession(Guid sessionId)
        {
            _activeSessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public void RecordPoll(Guid sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.RecordPoll();
            }
        }

        public Session? DequeueNextSession(bool fromOverflow = false)
        {
            var queue = fromOverflow ? _overflowQueue : _mainQueue;

            if (queue.TryDequeue(out var session))
            {
                return session;
            }
            return null;
        }

        public void RemoveSession(Guid sessionId)
        {
            _activeSessions.TryRemove(sessionId, out _);
        }

        public List<Session> GetInactiveSessions()
        {
            return _activeSessions.Values
                .Where(s => s.Status == SessionStatus.Queued || s.Status == SessionStatus.Active)
                .Where(s => s.IsInactive())
                .ToList();
        }
    }
}
