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
    public class EventController(IEventRepository eventRepository, IUserRepository userRepository) : ControllerBase
    {
        private readonly IEventRepository _eventRepository = eventRepository;
        private readonly IUserRepository _userRepository = userRepository;

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var eventData = await _eventRepository.GetEventByIdAsync(id);
            if (eventData == null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "Event not found"
                });
            }

            return Ok(new { StatusCode = 200, Message = eventData });
        }

        // [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetEventsByUser([FromQuery] int userId)
        {
            var events = await _eventRepository.GetEventsByUserIdAsync(userId);
            if (!events.Any())
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "User have no events"
                });
            }

            return Ok(new { StatusCode = 200, Message = events });
        }

        // [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventRequest? eventData)
        {
            if (eventData is null)
            {
                return BadRequest(new { statusCode = 400, message = "Event data is null" });
            }

            // Check if user not exists in database
            try
            {
                var user = await _userRepository.GetUserByIdAsync(eventData.CreatorId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = 400, Message = ex.Message });
            }

            if (DateTime.Compare(eventData.StartTime, eventData.EndTime) >= 0)
            {
                return BadRequest(new { StatusCode = 400, Message = "Start time must be before end time" });
            }

            var newEvent = eventData.ToEntity();

            try
            {
                await _eventRepository.CreateEventAsync(newEvent);
                return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, newEvent);

            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = 400, Message = ex.Message });
            }
        }

        // [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteEvent(Guid eventId)
        {
            try
            {
                await _eventRepository.DeleteEventAsync(eventId);
                return Ok(new { StatusCode = 200, Message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = 400, Message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] Event eventData)
        {
            if (DateTime.Compare(eventData.StartTime, eventData.EndTime) >= 0)
            {
                return BadRequest(new { StatusCode = 400, Message = "Start time must be before end time" });
            }

            try
            {
                await _eventRepository.UpdateEventAsync(id, eventData);
                return Ok(new { statusCode = 200, message = "Event updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}