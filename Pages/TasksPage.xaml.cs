using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class TasksPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        int projectId;

        public TasksPage(int id)
        {
            InitializeComponent();
            projectId = id;
            LoadPage();
        }

        /// Загрузка и инициализация страницы
        void LoadPage()
        {
            try
            {
                var project = db.Projects.FirstOrDefault(x => x.ProjectID == projectId);

                if (project == null)
                {
                    MessageBox.Show("Проект не найден.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                TitleBlock.Text = $"Задачи: {project.ProjectName}";

                StatusFilter.ItemsSource = db.Statuses.ToList();

                ConfigureActionButtons();

                LoadTasks();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке страницы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Настройка видимости кнопок действий в зависимости от роли пользователя
        private void ConfigureActionButtons()
        {
            var createTaskButton = this.FindName("CreateTaskBtn") as Button;
            if (createTaskButton != null)
            {
                createTaskButton.Visibility = CurrentUser.CanManageTasks ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            if (CurrentUser.CanViewOnly)
            {
                foreach (var column in TasksGrid.Columns)
                {
                    if (column.Header.ToString() == "Действия")
                    {
                        column.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
        }

        /// Загрузка задач проекта
        void LoadTasks()
        {
            try
            {
                TasksGrid.ItemsSource = db.Tasks
                    .Where(t => t.ProjectID == projectId)
                    .ToList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке задач: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик изменения фильтров (живая фильтрация)
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var tasks = db.Tasks.Where(t => t.ProjectID == projectId);

                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    string searchText = SearchBox.Text.ToLower();
                    tasks = tasks.Where(t =>
                        t.TaskName.ToLower().Contains(searchText) ||
                        (t.TaskDescription ?? "").ToLower().Contains(searchText));
                }

                if (StatusFilter.SelectedItem is Statuses selectedStatus)
                {
                    tasks = tasks.Where(t => t.StatusID == selectedStatus.StatusID);
                }

                TasksGrid.ItemsSource = tasks.ToList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации задач: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Сброс всех фильтров
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusFilter.SelectedIndex = -1;
            LoadTasks();
        }

        /// Обработчик нажатия кнопки "Назад"
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProjectDetailsPage(projectId));
        }

        /// Обработчик нажатия кнопки создания новой задачи
        private void CreateTask_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageTasks)
            {
                MessageBox.Show("У вас нет прав для создания задач!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new CreateTaskPage(projectId));
        }

        /// Обработчик открытия детальной информации о задаче
        private void OpenTask_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            int taskId = (int)button.Tag;
            NavigationService.Navigate(new TaskDetails(taskId));
        }

        /// Обработчик редактирования задачи
        private void EditTask_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageTasks)
            {
                MessageBox.Show("У вас нет прав для редактирования задач!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            int taskId = (int)button.Tag;
            NavigationService.Navigate(new EditTaskPage(taskId));
        }

        /// Обработчик удаления задачи
        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageTasks)
            {
                MessageBox.Show("У вас нет прав для удаления задач!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            int taskId = (int)button.Tag;

            if (MessageBox.Show("Вы уверены, что хотите удалить задачу?\nЭто действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var task = db.Tasks.FirstOrDefault(t => t.TaskID == taskId);
                if (task == null)
                {
                    MessageBox.Show("Задача не найдена.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                db.Tasks.Remove(task);
                db.SaveChanges();

                Filter_Changed(null, null);

                MessageBox.Show("Задача успешно удалена.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении задачи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}