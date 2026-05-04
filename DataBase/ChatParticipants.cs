namespace ManagerOfItProjects.DataBase
{
    public partial class ChatParticipants
    {
        public int ChatParticipantID { get; set; }
        public int ChatID { get; set; }
        public int UserID { get; set; }

        public virtual Chats Chats { get; set; }
        public virtual Users Users { get; set; }
    }
}
