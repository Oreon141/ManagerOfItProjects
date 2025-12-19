using ManagerOfItProjects.DataBase;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class EditProjectPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        Projects currentProject;

        public EditProjectPage(int projectId)
        {
            InitializeComponent();
            LoadFields(projectId);
        }

        /// Загрузка данных проекта в поля формы
        void LoadFields(int id)
        {
            try
            {
                currentProject = db.Projects.FirstOrDefault(p => p.ProjectID == id);

                if (currentProject == null)
                {
                    MessageBox.Show("Проект не найден!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                ProjectNameBox.Text = currentProject.ProjectName;
                DescriptionBox.Text = currentProject.Description;
                DeadlinePicker.SelectedDate = currentProject.Deadline;

                PriorityBox.ItemsSource = new string[]
                {
                    "Низкий", "Средний", "Высокий", "Критический"
                };
                PriorityBox.SelectedItem = currentProject.Priority;

                var statuses = db.Statuses.ToList();
                if (statuses.Count == 0)
                {
                    MessageBox.Show("В базе данных отсутствуют статусы.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusBox.ItemsSource = statuses;
                StatusBox.DisplayMemberPath = "StatusName";
                StatusBox.SelectedItem = currentProject.Statuses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных проекта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик нажатия кнопки сохранения
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectNameBox.Text))
            {
                MessageBox.Show("Название проекта обязательно.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Focus();
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

            if (StatusBox.SelectedItem == null || !(StatusBox.SelectedItem is Statuses selectedStatus))
            {
                MessageBox.Show("Выберите статус проекта.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusBox.Focus();
                return;
            }

            if (PriorityBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите приоритет проекта.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriorityBox.Focus();
                return;
            }

            string newProjectName = ProjectNameBox.Text.Trim();
            bool nameExists = db.Projects
                .Any(p => p.ProjectName == newProjectName && p.ProjectID != currentProject.ProjectID);

            if (nameExists)
            {
                MessageBox.Show($"Проект с названием '{newProjectName}' уже существует.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameBox.Focus();
                ProjectNameBox.SelectAll();
                return;
            }

            try
            {
                currentProject.ProjectName = newProjectName;
                currentProject.Description = DescriptionBox.Text.Trim();
                currentProject.Priority = PriorityBox.Text;
                currentProject.Deadline = DeadlinePicker.SelectedDate.Value;
                currentProject.StatusID = selectedStatus.StatusID;

                db.SaveChanges();

                MessageBox.Show("Изменения успешно сохранены!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new ProjectDetailsPage(currentProject.ProjectID));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                NavigationService.Navigate(new ProjectDetailsPage(currentProject.ProjectID));
            }
            else
            {
                NavigationService.GoBack();
            }
        }
    }
}