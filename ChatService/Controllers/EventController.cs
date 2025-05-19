using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Mapper;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController(IEventRepository eventRepository) : Controller
    {
        private readonly IEventRepository _eventRepository = eventRepository;

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _eventRepository.GetEventsAsync();
            return Ok(events);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventRequest eventData)
        {
            var newEvent = eventData.ToEntity();
            try
            {
                await _eventRepository.CreateEventAsync(newEvent);
                return Created();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}