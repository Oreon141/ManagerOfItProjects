using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagerOfItProjects.Services
{
    public static class NotificationService
    {
        public static void CreateNotification(int userId, string title, string message, string type, int? relatedEntityId = null)
        {
            var db = ITProjectsManagerEntities.GetContext();
            db.Notifications.Add(new Notifications
            {
                UserID = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now,
                IsRead = false,
                RelatedEntityID = relatedEntityId
            });
            db.SaveChanges();
        }

        public static int GetUnreadCount(int userId)
        {
            var db = ITProjectsManagerEntities.GetContext();
            return db.Notifications.Count(x => x.UserID == userId && !x.IsRead);
        }

        public static void CreateDeadlineNotifications()
        {
            var db = ITProjectsManagerEntities.GetContext();
            var tomorrow = DateTime.Today.AddDays(1);
            var projects = db.Projects.Where(p => p.Deadline == tomorrow).ToList();
            foreach (var project in projects)
            {
                List<int> participantIds = db.ProjectUsers.Where(pu => pu.ProjectID == project.ProjectID).Select(pu => pu.UserID).Distinct().ToList();
                foreach (var userId in participantIds)
                {
                    bool existsToday = db.Notifications.Any(n => n.UserID == userId && n.Type == "DeadlineWarning" && n.RelatedEntityID == project.ProjectID && n.CreatedAt >= DateTime.Today);
                    if (!existsToday)
                    {
                        db.Notifications.Add(new Notifications
                        {
                            UserID = userId,
                            Title = "Приближается дедлайн",
                            Message = $"Дедлайн проекта {project.ProjectName} наступает через 1 день",
                            Type = "DeadlineWarning",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            RelatedEntityID = project.ProjectID
                        });
                    }
                }
            }
            db.SaveChanges();
        }
    }
}
