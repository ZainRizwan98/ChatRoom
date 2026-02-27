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
        private readonly Queue<Session> _queue = new();
        private readonly object _queueLock = new();
        private readonly ConcurrentDictionary<Guid, Session> _activeSessions = new();
        private readonly TeamManagementService _teamService;

        public QueueService(TeamManagementService teamService)
        {
            _teamService = teamService;
        }

        public int MainQueueCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _queue.Count(s => !s.IsOverflow);
                }
            }
        }

        public int OverflowQueueCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _queue.Count(s => s.IsOverflow);
                }
            }
        }
        public int ActiveSessionCount => _activeSessions.Count;

        public async Task<(bool Success, Session? Session, string? ErrorMessage)> EnqueueChatRequest(string userId)
        {
            var currentTeams = _teamService.GetActiveTeams();
            var totalCapacity = currentTeams.Sum(t => t.TotalCapacity);
            var maxQueueLength = (int)(totalCapacity * 1.5);

            int mainCount;
            int overflowCount;
            lock (_queueLock)
            {
                mainCount = _queue.Count(s => !s.IsOverflow);
                overflowCount = _queue.Count(s => s.IsOverflow);
            }

            // If main queue is full, consider overflow (when in office hours)
            if (mainCount >= maxQueueLength)
            {
                if (_teamService.IsOfficeHours())
                {
                    var overflowTeam = _teamService.GetOverflowTeam();
                    var overflowMaxQueue = (int)(overflowTeam.TotalCapacity * 1.5);

                    if (overflowCount >= overflowMaxQueue)
                    {
                        return (false, null, "No agents available. Both queues are full. Please try again later.");
                    }

                    var overflowSession = CreateChatSession(userId, isOverflow: true);
                    lock (_queueLock)
                    {
                        _queue.Enqueue(overflowSession);
                    }
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
            lock (_queueLock)
            {
                _queue.Enqueue(session);
            }
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
                Status = SessionStatus.Queued,
                IsOverflow = isOverflow
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
            // Dequeue the next session that matches the requested overflow flag.
            lock (_queueLock)
            {
                if (_queue.Count == 0)
                    return null;

                var temp = new List<Session>();
                Session? found = null;

                while (_queue.Count > 0)
                {
                    var s = _queue.Dequeue();
                    if (found == null && s.IsOverflow == fromOverflow)
                    {
                        found = s;
                        break;
                    }
                    temp.Add(s);
                }

                // Put back the skipped sessions in the same order
                for (int i = 0; i < temp.Count; i++)
                {
                    _queue.Enqueue(temp[i]);
                }

                return found;
            }
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
