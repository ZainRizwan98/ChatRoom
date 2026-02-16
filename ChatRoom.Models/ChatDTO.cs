using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRoom.Models
{
    public class CreateChatSessionRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }

    public class CreateChatSessionResponse
    {
        public bool Success { get; set; }
        public Guid? SessionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PollRequest
    {
        public Guid SessionId { get; set; }
    }

    public class PollResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public AgentInfo? Agent { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
        public int QueuePosition { get; set; }
    }

    public class AgentInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Seniority { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsFromAgent { get; set; }
    }

    public class SendMessageRequest
    {
        public Guid SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class SendMessageResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public MessageDto? Message { get; set; }
    }

    public class CompleteChatRequest
    {
        public Guid SessionId { get; set; }
    }

    public class CompleteChatResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
