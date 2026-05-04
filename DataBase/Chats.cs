using System;
using System.Collections.Generic;

namespace ManagerOfItProjects.DataBase
{
    public partial class Chats
    {
        public Chats()
        {
            ChatParticipants = new HashSet<ChatParticipants>();
            Messages = new HashSet<Messages>();
        }

        public int ChatID { get; set; }
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ChatParticipants> ChatParticipants { get; set; }
        public virtual ICollection<Messages> Messages { get; set; }
    }
}
