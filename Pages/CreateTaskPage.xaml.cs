using ManagerOfItProjects.DataBase;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class CreateTaskPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        int projectId;
        int currentUserId = 1;

        public CreateTaskPage(int id)
        {
            InitializeComponent();
            projectId = id;
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusBox.ItemsSource = db.Statuses.ToList();
                StatusBox.DisplayMemberPath = "StatusName";

                if (StatusBox.Items.Count == 0)
                {
                    MessageBox.Show("В базе данных отсутствуют статусы. Невозможно создать задачу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusBox.SelectedIndex = 0;

                UserBox.ItemsSource = db.Users.ToList();
                UserBox.DisplayMemberPath = "Login";

                if (UserBox.Items.Count == 0)
                {
                    MessageBox.Show("В базе данных отсутствуют пользователи. Невозможно назначить исполнителя.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик нажатия кнопки создания задачи
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskNameBox.Text))
            {
                MessageBox.Show("Введите название задачи.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TaskNameBox.Focus();
                return;
            }

            if (StatusBox.SelectedItem == null || !(StatusBox.SelectedItem is Statuses selectedStatus))
            {
                MessageBox.Show("Выберите статус задачи.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            if (UserBox.Items.Count == 0)
            {
                MessageBox.Show("В системе нет пользователей для назначения задачи.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var task = new Tasks
                {
                    TaskName = TaskNameBox.Text.Trim(),
                    TaskDescription = DescriptionBox.Text.Trim(),
                    ProjectID = projectId,
                    StatusID = selectedStatus.StatusID,
                    AssignedTo = (UserBox.SelectedItem as Users)?.UserID,
                    CreatedBy = currentUserId,
                    Priority = "Средний"
                };

                db.Tasks.Add(task);
                db.SaveChanges();

                MessageBox.Show("Задача успешно создана!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new TasksPage(projectId));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при создании задачи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TasksPage(projectId));
        }
    }
}