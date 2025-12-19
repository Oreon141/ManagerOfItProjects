using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class TaskDetails : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private Tasks task;

        public TaskDetails(int taskId)
        {
            InitializeComponent();
            LoadTaskData(taskId);
        }

        /// Загрузка данных задачи по идентификатору
        void LoadTaskData(int taskId)
        {
            try
            {
                task = db.Tasks.FirstOrDefault(t => t.TaskID == taskId);

                if (task == null)
                {
                    MessageBox.Show("Задача не найдена!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                LoadTaskDetails();
                ConfigureEditButton();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных задачи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Настройка видимости кнопки редактирования в зависимости от роли пользователя
        private void ConfigureEditButton()
        {
            var editButton = this.FindName("EditTaskButton") as Button;

            if (editButton != null)
            {
                editButton.Visibility = CurrentUser.CanManageTasks ?
                    Visibility.Visible : Visibility.Collapsed;

                if (CurrentUser.CanViewOnly)
                {
                    var infoText = new TextBlock
                    {
                        Text = "Режим просмотра (редактирование недоступно)",
                        Foreground = System.Windows.Media.Brushes.Gray,
                        FontSize = 14,
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    var parentStackPanel = editButton.Parent as StackPanel;
                    if (parentStackPanel != null)
                    {
                        int buttonIndex = parentStackPanel.Children.IndexOf(editButton);
                        parentStackPanel.Children.Insert(buttonIndex + 1, infoText);
                    }
                }
            }
        }

        /// Загрузка детальной информации о задаче в элементы интерфейса
        void LoadTaskDetails()
        {
            TitleText.Text = task.TaskName;
            DescriptionText.Text = task.TaskDescription ?? "Описание отсутствует";
            ProjectText.Text = task.Projects?.ProjectName ?? "Не указан";
            StatusText.Text = task.Statuses?.StatusName ?? "Не указан";

            if (task.Users1 != null)
            {
                UserText.Text = $"{task.Users1.Surname} {task.Users1.FirstName}";
            }
            else
            {
                UserText.Text = "Не назначен";
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        /// Обработчик нажатия кнопки "Редактировать задачу"
        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            if (task == null)
            {
                MessageBox.Show("Данные задачи не загружены.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CurrentUser.CanManageTasks)
            {
                MessageBox.Show("У вас нет прав для редактирования задач!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new EditTaskPage(task.TaskID));
        }
    }
}