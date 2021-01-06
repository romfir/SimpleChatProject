using System;
using System.ComponentModel.DataAnnotations;

namespace Chat.Shared
{
    public class Message
    {
        public Message()
        { }

        public Message(string user, string text, DateTime timeStamp)
        {
            User = user;
            Text = text;
            TimeStamp = timeStamp;
        }

        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string User { get; set; } = null!;

        [Required, MaxLength(200), MinLength(1)]
        public string Text { get; set; } = null!;

        public DateTime TimeStamp { get; set; }
    }
}
