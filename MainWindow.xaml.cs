using ManagerOfItProjects.Models;
using ManagerOfItProjects.Pages;
using ManagerOfItProjects.Windows;
using ManagerOfItProjects.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ManagerOfItProjects
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer notificationsTimer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();

            if (CurrentUser.UserID == 0)
            {
                MessageBox.Show("Ошибка авторизации. Пожалуйста, войдите в систему.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }

            ConfigureNavigationByRole();
            InitializeNotificationsCounter();
            LoadDefaultPage();
        }

        /// Настройка видимости кнопок навигации в зависимости от роли пользователя
        private void ConfigureNavigationByRole()
        {
            UserInfoText.Text = $"{CurrentUser.Login} ({CurrentUser.Role})";

            ProjectsNavButton.Visibility = Visibility.Visible;
            TasksNavButton.Visibility = Visibility.Visible;
            UsersNavButton.Visibility = CurrentUser.CanManageUsers ?
                Visibility.Visible : Visibility.Collapsed;

            this.Title = $"IT Projects Manager - {CurrentUser.Login} ({CurrentUser.Role})";
        }


        private void InitializeNotificationsCounter()
        {
            notificationsTimer.Interval = TimeSpan.FromSeconds(10);
            notificationsTimer.Tick += (s, e) => UpdateNotificationsCounter();
            notificationsTimer.Start();
            UpdateNotificationsCounter();
        }

        private void UpdateNotificationsCounter()
        {
            int unread = NotificationService.GetUnreadCount(CurrentUser.UserID);
            NotificationsNavButton.Content = unread > 0 ? $"🔔  Уведомления ({unread})" : "🔔  Уведомления";
        }

        /// Загрузка страницы по умолчанию в зависимости от роли пользователя
        private void LoadDefaultPage()
        {
            MainFrame.Navigate(new Pages.ProjectsPage());
        }

        /// Обработчик нажатия кнопки "Календарь"
        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CalendarPage());
        }

        /// Обработчик нажатия кнопки "Прогресс"
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.ProgressPage());
        }

        /// Обработчик нажатия кнопки "Проекты"
        private void Projects_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.ProjectsPage());
        }

        /// Обработчик нажатия кнопки "Задачи"
        private void Tasks_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.AllTasksPage());
        }


        private void Notifications_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new NotificationsPage());
            UpdateNotificationsCounter();
        }

        private void Chat_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ChatPage());
        }

        /// Обработчик нажатия кнопки "Пользователи"
        private void Users_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.CanManageUsers)
            {
                MainFrame.Navigate(new Pages.UsersPage());
            }
            else
            {
                MessageBox.Show("Доступ запрещен! Требуются права администратора.",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                MainFrame.Navigate(new Pages.ProjectsPage());
            }
        }

        /// Обработчик нажатия кнопки "Отчёты"
        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.ReportsPage());
        }

        /// Обработчик нажатия кнопки "Выход"
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentUser.Clear();

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}