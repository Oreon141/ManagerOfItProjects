using System;

namespace ManagerOfItProjects.Models
{
    public class ChatItem
    {
        public int ChatID { get; set; }
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageItem
    {
        public int MessageID { get; set; }
        public int ChatID { get; set; }
        public int SenderID { get; set; }
        public string SenderLogin { get; set; }
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class ChatParticipantItem
    {
        public int UserID { get; set; }
        public string Login { get; set; }
        public string RoleName { get; set; }
    }
}
