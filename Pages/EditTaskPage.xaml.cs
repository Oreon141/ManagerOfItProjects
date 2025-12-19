using ManagerOfItProjects.DataBase;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class EditTaskPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        Tasks task;

        public EditTaskPage(int taskId)
        {
            InitializeComponent();
            LoadTask(taskId);
        }

        /// Загрузка данных задачи в поля формы
        void LoadTask(int id)
        {
            try
            {
                task = db.Tasks.FirstOrDefault(t => t.TaskID == id);

                if (task == null)
                {
                    MessageBox.Show("Задача не найдена!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                TaskNameBox.Text = task.TaskName;
                DescriptionBox.Text = task.TaskDescription;

                StatusBox.ItemsSource = db.Statuses.ToList();
                StatusBox.SelectedItem = task.Statuses;

                UserBox.ItemsSource = db.Users.ToList();
                UserBox.SelectedItem = task.Users;

                PriorityBox.ItemsSource = new[] { "Низкий", "Средний", "Высокий", "Критический" };
                PriorityBox.SelectedItem = task.Priority;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных задачи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик нажатия кнопки сохранения
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskNameBox.Text))
            {
                MessageBox.Show("Введите название задачи!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TaskNameBox.Focus();
                return;
            }

            if (StatusBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус задачи!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            if (PriorityBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите приоритет задачи!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriorityBox.Focus();
                return;
            }

            try
            {
                task.TaskName = TaskNameBox.Text.Trim();
                task.TaskDescription = DescriptionBox.Text.Trim();

                var selectedStatus = StatusBox.SelectedItem as Statuses;
                if (selectedStatus != null)
                    task.StatusID = selectedStatus.StatusID;

                var selectedUser = UserBox.SelectedItem as Users;
                task.AssignedTo = selectedUser?.UserID;

                task.Priority = PriorityBox.SelectedItem?.ToString();

                db.SaveChanges();

                MessageBox.Show("Изменения успешно сохранены!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new TasksPage(task.ProjectID));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}