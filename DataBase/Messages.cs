using System;

namespace ManagerOfItProjects.DataBase
{
    public partial class Messages
    {
        public int MessageID { get; set; }
        public int ChatID { get; set; }
        public int SenderID { get; set; }
        public string MessageText { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        public virtual Chats Chats { get; set; }
        public virtual Users Users { get; set; }
    }
}
