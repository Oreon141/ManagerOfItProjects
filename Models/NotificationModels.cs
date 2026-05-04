using System;

namespace ManagerOfItProjects.Models
{
    public class NotificationItem
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? RelatedEntityID { get; set; }
    }
}
