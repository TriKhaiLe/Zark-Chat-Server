using System.Runtime.InteropServices.JavaScript;
using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Mapper;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController(IEventRepository eventRepository, IUserRepository userRepository) : Controller
    {
        private readonly IEventRepository _eventRepository = eventRepository;
        private readonly IUserRepository _userRepository = userRepository;

        // [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] int userId)
        {
            var events = await _eventRepository.GetEventsByIdAsync(userId);
            if (events.Count == 0)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = $"No events found for user with id: {userId}"
                });
            }

            return Ok(new { StatusCode = 200, Message = events });
        }

        // [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventRequest? eventData)
        {
            if (eventData == null)
            {
                return BadRequest("Event data is null");
            }

            // Check if user not exists in database
            try
            {
                var user = await _userRepository.GetUserByIdAsync(eventData.CreatorId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = 500, Message = ex.Message });
            }

            if (DateTime.Compare(eventData.StartTime, eventData.EndTime) >= 0)
            {
                return BadRequest(new { StatusCode = 500, Message = "Start time must be before end time" });
            }

            var newEvent = eventData.ToEntity();

            try
            {
                await _eventRepository.CreateEventAsync(newEvent);
                return Created(
                    uri: $"/api/events/{newEvent.Id}",
                    value: newEvent
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = 500, Message = ex.Message });
            }
        }

        // [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteEvent([FromQuery] int userId, [FromQuery] string eventId)
        {
            try
            {
                await _eventRepository.DeleteEventAsync(userId, eventId);
                return Ok(new { StatusCode = 200, Message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = 500, Message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateEvent([FromBody] Event? eventData)
        {
            if (eventData == null)
            {
                return BadRequest(new { statusCode = 500, message = "Event data is null" });
            }

            try
            {
                await _eventRepository.UpdateEventAsync(eventData);
                return Ok(new { statusCode = 200, message = "Event updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 500, message = ex.Message });
            }
        }
    }
}