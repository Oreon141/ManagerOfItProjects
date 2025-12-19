using ManagerOfItProjects.DataBase;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class CreateProjectPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();

        public CreateProjectPage()
        {
            InitializeComponent();
        }

        /// Обработчик загрузки страницы - инициализация элементов управления
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PriorityBox.ItemsSource = new[]
                {
                    "Низкий", "Средний", "Высокий", "Критический"
                };
                PriorityBox.SelectedIndex = 1;

                var statuses = db.Statuses.ToList();
                if (statuses.Count == 0)
                {
                    MessageBox.Show("В базе данных отсутствуют статусы. Невозможно создать проект.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                StatusBox.ItemsSource = statuses;
                StatusBox.DisplayMemberPath = "StatusName";
                StatusBox.SelectedIndex = 0;

                DeadlinePicker.SelectedDate = DateTime.Now.AddDays(30);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик нажатия кнопки создания проекта
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectNameBox.Text))
            {
                MessageBox.Show("Введите название проекта.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Focus();
                return;
            }

            if (PriorityBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите приоритет проекта.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriorityBox.Focus();
                return;
            }

            if (StatusBox.SelectedItem == null || !(StatusBox.SelectedItem is Statuses selectedStatus))
            {
                MessageBox.Show("Выберите статус проекта.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            if (DeadlinePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите дедлайн проекта.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DeadlinePicker.Focus();
                return;
            }

            if (DeadlinePicker.SelectedDate.Value.Date < DateTime.Now.Date)
            {
                MessageBox.Show("Дедлайн не может быть в прошлом.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DeadlinePicker.Focus();
                return;
            }

            string projectName = ProjectNameBox.Text.Trim();
            bool projectExists = db.Projects.Any(p => p.ProjectName == projectName);
            if (projectExists)
            {
                MessageBox.Show($"Проект с названием '{projectName}' уже существует.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Focus();
                ProjectNameBox.SelectAll();
                return;
            }

            try
            {
                Projects project = new Projects
                {
                    ProjectName = projectName,
                    Description = DescriptionBox.Text.Trim(),
                    Priority = PriorityBox.Text,
                    Deadline = DeadlinePicker.SelectedDate.Value,
                    StatusID = selectedStatus.StatusID,
                    StartDate = DateTime.Now
                };

                db.Projects.Add(project);
                db.SaveChanges();

                MessageBox.Show("Проект успешно создан!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new ProjectsPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании проекта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProjectsPage());
        }
    }
}