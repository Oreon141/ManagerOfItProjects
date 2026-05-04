using System;

namespace ManagerOfItProjects.DataBase
{
    public partial class Notifications
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? RelatedEntityID { get; set; }

        public virtual Users Users { get; set; }
    }
}
