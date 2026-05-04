using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ManagerOfItProjects.Services
{
    public static class NotificationService
    {
        public static void CreateNotification(int userId, string title, string message, string type, int? relatedEntityId = null)
        {
            var db = ITProjectsManagerEntities.GetContext();
            db.Database.ExecuteSqlCommand(
                "INSERT INTO Notifications (UserID, Title, Message, Type, CreatedAt, IsRead, RelatedEntityID) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6)",
                userId, title, message, type, DateTime.Now, false, (object)relatedEntityId ?? DBNull.Value);
        }

        public static List<NotificationItem> GetNotifications(int userId, bool? isRead = null)
        {
            var db = ITProjectsManagerEntities.GetContext();
            string sql = "SELECT NotificationID, UserID, Title, Message, Type, CreatedAt, IsRead, RelatedEntityID FROM Notifications WHERE UserID = @p0";
            var args = new List<object> { userId };
            if (isRead.HasValue)
            {
                sql += " AND IsRead = @p1";
                args.Add(isRead.Value);
            }
            sql += " ORDER BY CreatedAt DESC";
            return db.Database.SqlQuery<NotificationItem>(sql, args.ToArray()).ToList();
        }

        public static void MarkAllRead(int userId)
        {
            var db = ITProjectsManagerEntities.GetContext();
            db.Database.ExecuteSqlCommand("UPDATE Notifications SET IsRead = 1 WHERE UserID = @p0 AND IsRead = 0", userId);
        }

        public static void MarkRead(int notificationId)
        {
            var db = ITProjectsManagerEntities.GetContext();
            db.Database.ExecuteSqlCommand("UPDATE Notifications SET IsRead = 1 WHERE NotificationID = @p0", notificationId);
        }

        public static int GetUnreadCount(int userId)
        {
            var db = ITProjectsManagerEntities.GetContext();
            return db.Database.SqlQuery<int>("SELECT COUNT(1) FROM Notifications WHERE UserID = @p0 AND IsRead = 0", userId).FirstOrDefault();
        }

        public static void CreateDeadlineNotifications()
        {
            var db = ITProjectsManagerEntities.GetContext();
            DateTime tomorrow = DateTime.Today.AddDays(1);
            var projects = db.Projects.Where(p => DbFunctions.TruncateTime(p.Deadline) == tomorrow).ToList();
            foreach (var project in projects)
            {
                var userIds = db.ProjectUsers.Where(pu => pu.ProjectID == project.ProjectID).Select(pu => pu.UserID).Distinct().ToList();
                foreach (var userId in userIds)
                {
                    int exists = db.Database.SqlQuery<int>(
                        "SELECT COUNT(1) FROM Notifications WHERE UserID = @p0 AND Type = @p1 AND RelatedEntityID = @p2 AND CAST(CreatedAt as date)=@p3",
                        userId, "DeadlineWarning", project.ProjectID, DateTime.Today).FirstOrDefault();
                    if (exists == 0)
                    {
                        CreateNotification(userId, "Приближается дедлайн", $"Дедлайн проекта {project.ProjectName} наступает через 1 день", "DeadlineWarning", project.ProjectID);
                    }
                }
            }
        }
    }
}
