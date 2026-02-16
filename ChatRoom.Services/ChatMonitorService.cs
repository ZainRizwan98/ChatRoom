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
                    var queueService = scope.ServiceProvider.GetRequiredService<QueueService>();
                    var assignmentService = scope.ServiceProvider.GetRequiredService<ChatAssignmentService>();

                    await MonitorInactiveSessions(queueService, assignmentService);

                    await AssignPendingChats(assignmentService);

                    await Task.Delay(_monitorInterval, stoppingToken);
                }
                catch (Exception ex)
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
