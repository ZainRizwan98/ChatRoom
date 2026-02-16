using ChatRoom.Models;
using ChatRoom.Models.Enums;
using ChatRoom.Services;

namespace ChatRoom.Tests
{
    [TestClass]
    public class ChatRoomTests
    {
        public ChatRoomTests()
        {
        }

        [TestMethod]
        public void TeamACapacityCalculation()
        {
            var teamService = new TeamManagementService();
            var teams = teamService.GetAllAgents()
                .Where(a => a.Name.StartsWith("TeamLead A") || a.Name.StartsWith("MidLevel A") || a.Name.StartsWith("Junior A"))
                .ToList();

            // Team A: 1x team lead (5), 2x mid-level (6 each), 1x junior (4)
            var expectedCapacity = (1 * 10 * 0.5) + (2 * 10 * 0.6) + (1 * 10 * 0.4);
            var actualCapacity = teams.Sum(a => a.MaxConcurrentChats);

            Assert.AreEqual(21, (int)expectedCapacity);
            Assert.AreEqual(21, actualCapacity);
        }

        [TestMethod]
        public void TeamBCapacityCalculation()
        {
            var teamService = new TeamManagementService();
            var teams = teamService.GetAllAgents()
                .Where(a => a.Name.StartsWith("Senior B") || a.Name.StartsWith("MidLevel B") || a.Name.StartsWith("Junior B"))
                .ToList();

            // Team B: 1x senior (8), 1x mid-level (6), 2x junior (4 each)
            var expectedCapacity = (1 * 10 * 0.8) + (1 * 10 * 0.6) + (2 * 10 * 0.4);
            var actualCapacity = teams.Sum(a => a.MaxConcurrentChats);

            // Assert
            Assert.AreEqual(22, (int)expectedCapacity);
            Assert.AreEqual(22, actualCapacity);
        }

        [TestMethod]
        public void TwoAgentsOneSeniorOneJunior()
        {
            // 5 chats arrive. 4 to junior, 1 to senior

            var teamService = new TeamManagementService();
            var queueService = new QueueService(teamService);
            var assignmentService = new ChatAssignmentService(
                teamService, queueService);

            // Create a custom team for this test
            var testAgents = new List<Agent>
            {
                new Agent
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Senior",
                    Seniority = Seniority.Senior,
                    IsAvailable = true
                },
                new Agent
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Junior",
                    Seniority = Seniority.Junior,
                    IsAvailable = true
                }
            };

            for (int i = 0; i < 5; i++)
            {
                queueService.EnqueueChatRequest($"User{i}").Wait();
            }

            var assignments = new List<(string AgentName, Seniority Seniority)>();

            var availableAgents = testAgents.OrderBy(a => a.Seniority).ToList();

            for (int i = 0; i < 5; i++)
            {
                var agent = availableAgents
                    .Where(a => a.ActiveChats.Count < a.MaxConcurrentChats)
                    .OrderBy(a => a.Seniority)
                    .ThenBy(a => a.ActiveChats.Count)
                    .FirstOrDefault();

                if (agent != null)
                {
                    var session = new Session
                    {
                        Id = Guid.NewGuid(),
                        UserId = $"User{i}",
                        CreatedAt = DateTime.UtcNow,
                        Status = SessionStatus.Active
                    };
                    agent.ActiveChats.Add(session);
                    assignments.Add((agent.Name, agent.Seniority));
                }
            }

            var juniorAssignments = assignments.Count(a => a.Seniority == Seniority.Junior);
            var seniorAssignments = assignments.Count(a => a.Seniority == Seniority.Senior);

            Assert.AreEqual(4, juniorAssignments);
            Assert.AreEqual(1, seniorAssignments);
        }

        [TestMethod]
        public void ThreeAgentsTwoJuniorOneMid()
        {
            // 6 chats arrive 3 each to the juniors zero to the mid.

            var testAgents = new List<Agent>
            {
                new Agent
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Junior 1",
                    Seniority = Seniority.Junior,
                    IsAvailable = true
                },
                new Agent
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Junior 2",
                    Seniority = Seniority.Junior,
                    IsAvailable = true
                },
                new Agent
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Mid",
                    Seniority = Seniority.MidLevel,
                    IsAvailable = true
                }
            };

            var assignments = new Dictionary<string, int>
            {
                ["Test Junior 1"] = 0,
                ["Test Junior 2"] = 0,
                ["Test Mid"] = 0
            };

            for (int i = 0; i < 6; i++)
            {
                var agent = testAgents
                    .Where(a => a.ActiveChats.Count < a.MaxConcurrentChats)
                    .OrderBy(a => a.Seniority)
                    .ThenBy(a => a.ActiveChats.Count)
                    .FirstOrDefault();

                if (agent != null)
                {
                    var session = new Session
                    {
                        Id = Guid.NewGuid(),
                        UserId = $"User{i}",
                        CreatedAt = DateTime.UtcNow,
                        Status = SessionStatus.Active
                    };
                    agent.ActiveChats.Add(session);
                    assignments[agent.Name]++;
                }
            }
            Assert.AreEqual(3, assignments["Test Junior 1"]);
            Assert.AreEqual(3, assignments["Test Junior 2"]);
            Assert.AreEqual(0, assignments["Test Mid"]);
        }

        [TestMethod]
        public void OverflowTeamCapacityCalculation()
        {
            var teamService = new TeamManagementService();
            var overflowTeam = teamService.GetOverflowTeam();

            var expectedCapacity = 6 * 10 * 0.4;
            var actualCapacity = overflowTeam.TotalCapacity;

            Assert.AreEqual(24, (int)expectedCapacity);
            Assert.AreEqual(24, actualCapacity);
        }

        [TestMethod]
        public void MaxQueueLength()
        {
            var teamService = new TeamManagementService();

            var capacity = teamService.CalculateTotalCapacity();
            var maxQueue = teamService.CalculateMaxQueueLength();

            Assert.AreEqual((int)(capacity * 1.5), maxQueue);
        }

        [TestMethod]
        public async Task QueueRefusalWhenFull()
        {
            var teamService = new TeamManagementService();
            var queueService = new QueueService(teamService);

            var maxQueue = teamService.CalculateMaxQueueLength() + teamService.CalculateOverflowCapacity();

            for (int i = 0; i < maxQueue; i++)
            {
                await queueService.EnqueueChatRequest($"User{i}");
            }

            var result = await queueService.EnqueueChatRequest("UserOverflow");

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
        }
    }
}