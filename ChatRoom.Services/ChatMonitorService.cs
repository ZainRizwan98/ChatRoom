using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Services
{
    public class ChatMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(15);
        private ChatRoom.Models.Enums.ShiftType? _lastShift = null;

        public ChatMonitorService(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var teamService = scope.ServiceProvider.GetRequiredService<TeamManagementService>();
                    var queueService = scope.ServiceProvider.GetRequiredService<QueueService>();
                    var assignmentService = scope.ServiceProvider.GetRequiredService<ChatAssignmentService>();

                    // detect shift transitions
                    var currentShift = teamService.GetCurrentShift();
                    if (_lastShift == null)
                    {
                        _lastShift = currentShift;
                    }
                    else if (_lastShift != currentShift)
                    {
                        // handle end of previous shift
                        assignmentService.HandleShiftTransition();
                        try
                        {
                            // minimal action: log the summary to console. Consumers can expand this: persist to DB, emit events, etc.
                            Console.WriteLine($"Shift changed from {_lastShift} to {currentShift}.");
                        }
                        catch { }
                        _lastShift = currentShift;
                    }

                    await MonitorInactiveSessions(queueService, assignmentService);

                    await AssignPendingChats(assignmentService);

                    await Task.Delay(_monitorInterval, stoppingToken);
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task MonitorInactiveSessions(
           QueueService queueService,
            ChatAssignmentService assignmentService)
        {
            var inactiveSessions = queueService.GetInactiveSessions();

            foreach (var session in inactiveSessions)
            {
                assignmentService.MarkChatInactive(session.Id);
            }
        }

        private async Task AssignPendingChats(ChatAssignmentService assignmentService)
        {
            int assignedCount = 0;
            while (await assignmentService.TryAssignNextChat())
            {
                assignedCount++;
            }
        }
    }
}
