using ChatRoom.Models;
using ChatRoom.Models.Enums;
using ChatRoom.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace ChatRoom.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatRoomController : Controller
    {
        private readonly QueueService _queueService;
        private readonly ChatAssignmentService _assignmentService;

        public ChatRoomController(
            QueueService queueService,
            ChatAssignmentService assignmentService)
        {
            _queueService = queueService;
            _assignmentService = assignmentService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<CreateChatSessionResponse>> CreateSession(
            [FromBody] CreateChatSessionRequest request)
        {
            var (success, session, errorMessage) = await _queueService.EnqueueChatRequest(request.UserId);

            if (!success)
            {
                return Ok(new CreateChatSessionResponse
                {
                    Success = false,
                    Status = "refused",
                    ErrorMessage = errorMessage
                });
            }

            return Ok(new CreateChatSessionResponse
            {
                Success = true,
                SessionId = session?.Id,
                Status = "queued"
            });
        }

        [HttpPost("poll")]
        public async Task<ActionResult<PollResponse>> Poll([FromBody] PollRequest request)
        {
            var session = _queueService.GetSession(request.SessionId);

            if (session == null)
            {
                return NotFound(new PollResponse
                {
                    Success = false,
                    Status = "not_found"
                });
            }

            _queueService.RecordPoll(request.SessionId);

            var response = new PollResponse
            {
                Success = true,
                Status = session.Status.ToString().ToLower(),
                Messages = session.Messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    IsFromAgent = m.IsFromAgent
                }).ToList()
            };

            if (session.AssignedAgent != null)
            {
                response.Agent = new AgentInfo
                {
                    Name = session.AssignedAgent.Name,
                    Seniority = session.AssignedAgent.Seniority.ToString()
                };
            }

            return Ok(response);
        }

        [HttpPost("message")]
        public async Task<ActionResult<SendMessageResponse>> SendMessage(
            [FromBody] SendMessageRequest request)
        {
            var session = _queueService.GetSession(request.SessionId);

            if (session == null)
            {
                return NotFound(new SendMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Session not found"
                });
            }

            if (session.Status != SessionStatus.Active)
            {
                return BadRequest(new SendMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Session is not active"
                });
            }

            var message = new Models.Message
            {
                Id = Guid.NewGuid(),
                SenderId = session.UserId,
                Content = request.Content,
                Timestamp = DateTime.UtcNow,
                IsFromAgent = false
            };

            session.Messages.Add(message);

            return Ok(new SendMessageResponse
            {
                Success = true,
                Message = new MessageDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    IsFromAgent = message.IsFromAgent
                }
            });
        }

        [HttpPost("complete")]
        public async Task<ActionResult<CompleteChatResponse>> CompleteChat(
            [FromBody] CompleteChatRequest request)
        {
            var session = _queueService.GetSession(request.SessionId);

            if (session == null)
            {
                return NotFound(new CompleteChatResponse
                {
                    Success = false,
                    ErrorMessage = "Session not found"
                });
            }

            _assignmentService.CompleteChat(request.SessionId);

            return Ok(new CompleteChatResponse
            {
                Success = true
            });
        }

        [HttpGet("status")]
        public ActionResult<object> GetStatus()
        {
            var status = _assignmentService.GetSystemStatus();
            return Ok(status);
        }
    }
}
