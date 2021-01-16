using System.ComponentModel.DataAnnotations;

namespace Chat.Client.Shared.Models
{
    public class UserNewRoomModel
    {
        [Required, MaxLength(200), MinLength(1)]
        public string NewChatRoomName { get; set; } = null!;
    }
}
