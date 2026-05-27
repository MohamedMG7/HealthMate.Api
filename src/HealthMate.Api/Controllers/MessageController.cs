using System.Security.Claims;
using HealthMate.Application.Manager.MessageManager;
using HealthMate.Application.Messaging.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers{
    [Authorize(Policy = "PatientOrHealthCareProvider")]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase{
        private readonly IMessageManager _messageManager;
        public MessageController(IMessageManager messageManager)
        {
            _messageManager = messageManager;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null)
                return Unauthorized();

            await _messageManager.SendMessageAsync(dto, senderId);
            return Ok(new { message = "Message sent successfully." });
        }

        [HttpGet("inbox")]
        public async Task<IActionResult> GetInbox()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var inbox = await _messageManager.GetInboxAsync(userId);
            return Ok(inbox);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessageById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var message = await _messageManager.GetMessageByIdAsync(id, userId);
            if (message == null)
                return NotFound();

            return Ok(message);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            await _messageManager.MarkAsReadAsync(id, userId);
            return Ok(new { message = "Message marked as read." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            await _messageManager.DeleteMessageAsync(id, userId);
            return Ok(new { message = "Message deleted successfully." });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            int count = await _messageManager.GetUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

        [HttpGet("available-receivers")]
        public async Task<IActionResult> GetAvailableReceivers()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var receivers = await _messageManager.GetAvailableReceiversAsync(userId);
            return Ok(receivers);
        }
	}
}