using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using ManagerOfItProjects.Services;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ManagerOfItProjects.Pages
{
    public partial class NotificationsPage : Page
    {
        private readonly ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        public NotificationsPage()
        {
            InitializeComponent();
            NotificationService.CreateDeadlineNotifications();
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            var query = db.Notifications.Where(n => n.UserID == CurrentUser.UserID);
            var filter = (FilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (filter == "Непрочитанные") query = query.Where(n => !n.IsRead);
            if (filter == "Прочитанные") query = query.Where(n => n.IsRead);

            var items = query.OrderByDescending(n => n.CreatedAt).ToList().Select(n => new
            {
                Entity = n,
                n.NotificationID,
                n.Title,
                n.Message,
                n.CreatedAt,
                ReadStatus = n.IsRead ? "Прочитано" : "Новое",
                TypeIcon = GetTypeIcon(n.Type),
                RowBrush = GetTypeBrush(n.Type)
            }).ToList();
            NotificationsGrid.ItemsSource = items;
        }

        private string GetTypeIcon(string type) => type == "TaskAssigned" ? "🟦" : type == "StatusChanged" ? "🟩" : type == "DeadlineWarning" ? "🟥" : "🟪";
        private Brush GetTypeBrush(string type) => type == "TaskAssigned" ? Brushes.LightBlue : type == "StatusChanged" ? Brushes.LightGreen : type == "DeadlineWarning" ? Brushes.MistyRose : Brushes.Plum;

        private void MarkAllRead_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var list = db.Notifications.Where(n => n.UserID == CurrentUser.UserID && !n.IsRead).ToList();
            list.ForEach(n => n.IsRead = true);
            db.SaveChanges();
            LoadNotifications();
        }

        private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadNotifications();

        private void NotificationsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic selected = NotificationsGrid.SelectedItem;
            if (selected == null) return;
            Notifications notification = selected.Entity as Notifications;
            if (notification == null) return;
            notification.IsRead = true;
            db.SaveChanges();

            if (notification.Type == "TaskAssigned" || notification.Type == "StatusChanged")
                NavigationService.Navigate(new TaskDetails(notification.RelatedEntityID ?? 0));
            else if (notification.Type == "DeadlineWarning")
                NavigationService.Navigate(new ProjectDetailsPage(notification.RelatedEntityID ?? 0));
            else
                LoadNotifications();
        }
    }
}
