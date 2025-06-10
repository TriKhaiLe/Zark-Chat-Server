using ChatService.Core.Interfaces;
using FirebaseAdmin.Messaging;

public class EventNotificationJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;

    public EventNotificationJob(IEventRepository eventRepository, IUserRepository userRepository)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
    }

    public async Task SendEventNotificationsAsync()
    {
        var now = DateTime.UtcNow;
        var events = await _eventRepository.GetEventsToNotifyAsync(now);
        foreach (var ev in events)
        {
            var acceptedParticipants = ev.Participants?.Where(p => p.Status == "Accepted").ToList() ?? [];

            var tokens = await _userRepository.GetFcmTokensByUserIdsAsync(acceptedParticipants.Select(p => p.UserId).ToList());
            if (tokens.Any())
            {
                var message = new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new Notification
                    {
                        Title = $"Reminder: {ev.Title}",
                        Body = $"Event starts at {ev.StartTime.ToLocalTime():HH:mm dd/MM}"
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "eventId", ev.Id.ToString() }
                    }
                };

                await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                await _eventRepository.MarkNotificationSent(ev.Id);
            }
        }
    }
}