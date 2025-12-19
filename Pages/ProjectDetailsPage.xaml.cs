using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class ProjectDetailsPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private int projectId;
        private Projects project;

        public ProjectDetailsPage(int id)
        {
            InitializeComponent();
            projectId = id;
            LoadProject();
        }

        /// Загрузка данных проекта
        void LoadProject()
        {
            try
            {
                project = db.Projects.FirstOrDefault(x => x.ProjectID == projectId);

                if (project == null)
                {
                    MessageBox.Show("Проект не найден!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                ProjectTitle.Text = $"Проект: {project.ProjectName}";
                NameText.Text = project.ProjectName;
                DescriptionText.Text = project.Description;
                PriorityText.Text = project.Priority;
                StatusText.Text = project.Statuses?.StatusName ?? "Не указан";

                StartDateText.Text = project.StartDate.ToShortDateString();
                DeadlineText.Text = project.Deadline.ToShortDateString();

                ConfigureActionButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных проекта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Настройка видимости кнопок действий в зависимости от роли пользователя
        private void ConfigureActionButtons()
        {
            var editButton = this.FindName("EditButton") as Button;
            if (editButton != null)
            {
                editButton.Visibility = CurrentUser.CanManageProjects ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            if (CurrentUser.CanViewOnly)
            {
                var infoText = new TextBlock
                {
                    Text = "Режим просмотра (редактирование недоступно)",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var buttonPanel = this.FindName("ButtonPanel") as StackPanel;
                if (buttonPanel != null && !buttonPanel.Children.Contains(infoText))
                {
                    buttonPanel.Children.Add(infoText);
                }
            }
        }

        /// Обработчик нажатия кнопки "Назад"
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        /// Обработчик нажатия кнопки "Редактировать"
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (project == null)
                return;

            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для редактирования проектов!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new EditProjectPage(projectId));
        }

        /// Обработчик нажатия кнопки "Задачи"
        private void OpenTasks_Click(object sender, RoutedEventArgs e)
        {
            if (project == null)
                return;

            NavigationService.Navigate(new TasksPage(projectId));
        }

        /// Обработчик нажатия кнопки "Участники"
        private void OpenUsers_Click(object sender, RoutedEventArgs e)
        {
            if (project == null)
                return;

            NavigationService.Navigate(new ProjectMembersPage(projectId));
        }

        /// Обработчик нажатия кнопки "Стадии"
        private void OpenStages_Click(object sender, RoutedEventArgs e)
        {
            if (project == null)
                return;

            NavigationService.Navigate(new ProjectStagesPage(projectId));
        }
    }
}