using System.Data.Entity;

namespace ManagerOfItProjects.DataBase
{
    public partial class ITProjectsManagerEntities
    {
        public virtual DbSet<Notifications> Notifications { get; set; }
        public virtual DbSet<Chats> Chats { get; set; }
        public virtual DbSet<ChatParticipants> ChatParticipants { get; set; }
        public virtual DbSet<Messages> Messages { get; set; }
    }
}
