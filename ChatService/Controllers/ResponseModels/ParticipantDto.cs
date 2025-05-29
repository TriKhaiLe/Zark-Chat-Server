namespace ChatService.Controllers.ResponseModels
{
    public class ParticipantDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public string Avatar { get; set; } = null!;
    }
}