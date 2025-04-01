﻿using ChatService.Application.DTOs;
using ChatService.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageRepository _messageRepository;

        public MessagesController(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        [HttpGet("{userId1}/{userId2}")]
        public async Task<IActionResult> GetChatHistory(int userId1, int userId2)
        {
            var messages = await _messageRepository.GetMessagesBetweenUsersAsync(userId1, userId2);
            var messageDtos = messages.Select(m => new MessageDto
            {
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead
            });
            return Ok(messageDtos);
        }
    }
}
