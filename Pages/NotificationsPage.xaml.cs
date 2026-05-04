using ManagerOfItProjects.Models;
using ManagerOfItProjects.Services;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace ManagerOfItProjects.Pages
{
    public partial class NotificationsPage : Page
    {
        public NotificationsPage()
        {
            InitializeComponent();
            NotificationService.CreateDeadlineNotifications();
            LoadNotifications();
        }

        private void LoadNotifications()
        {
            bool? filter = null;
            var filterTitle = (FilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (filterTitle == "Непрочитанные") filter = false;
            else if (filterTitle == "Прочитанные") filter = true;

            var items = NotificationService.GetNotifications(CurrentUser.UserID, filter)
                .Select(n => new
                {
                    Entity = n,
                    n.Title,
                    n.Message,
                    n.CreatedAt,
                    ReadStatus = n.IsRead ? "Прочитано" : "Новое",
                    TypeIcon = n.Type == "TaskAssigned" ? "🟦" : n.Type == "StatusChanged" ? "🟩" : n.Type == "DeadlineWarning" ? "🟥" : "🟪"
                }).ToList();

            NotificationsGrid.ItemsSource = items;
        }

        private void MarkAllRead_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NotificationService.MarkAllRead(CurrentUser.UserID);
            LoadNotifications();
        }

        private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadNotifications();

        private void NotificationsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic selected = NotificationsGrid.SelectedItem;
            if (selected == null) return;
            var notification = selected.Entity as NotificationItem;
            if (notification == null) return;

            NotificationService.MarkRead(notification.NotificationID);

            if (notification.Type == "TaskAssigned" || notification.Type == "StatusChanged")
                NavigationService.Navigate(new TaskDetails(notification.RelatedEntityID ?? 0));
            else if (notification.Type == "DeadlineWarning")
                NavigationService.Navigate(new ProjectDetailsPage(notification.RelatedEntityID ?? 0));
            else
                LoadNotifications();
        }
    }
}
