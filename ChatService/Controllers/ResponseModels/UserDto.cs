namespace ChatService.Controllers.ResponseModels
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FirebaseUid { get; set; }
        public string DisplayName { get; set; }
    }
}