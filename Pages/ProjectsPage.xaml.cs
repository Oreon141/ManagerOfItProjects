using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ManagerOfItProjects.Pages
{
    public partial class ProjectsPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        List<Projects> allProjects;

        public ProjectsPage()
        {
            InitializeComponent();
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                allProjects = db.Projects.ToList();

                StatusFilter.ItemsSource = db.Statuses.ToList();
                StatusFilter.DisplayMemberPath = "StatusName";
                StatusFilter.SelectedValuePath = "StatusID";

                var priorities = allProjects
                    .Select(p => p.Priority)
                    .Distinct()
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();

                PriorityFilter.Items.Clear();
                PriorityFilter.Items.Add("Все");
                foreach (var priority in priorities)
                {
                    PriorityFilter.Items.Add(priority);
                }
                PriorityFilter.SelectedIndex = 0;

                ConfigureActionButtons();

                UpdateGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Настройка видимости кнопок действий в зависимости от роли пользователя
        private void ConfigureActionButtons()
        {
            CreateProjectButton.Visibility = CurrentUser.CanManageProjects ?
                Visibility.Visible : Visibility.Collapsed;

            if (CurrentUser.CanViewOnly)
            {
                foreach (var column in ProjectsGrid.Columns)
                {
                    if (column.Header.ToString() == "Действия")
                    {
                        column.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
        }

        /// Обработчик изменения фильтров
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            UpdateGrid();
        }

        /// Обработчик сброса всех фильтров
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusFilter.SelectedIndex = -1;
            PriorityFilter.SelectedIndex = 0;
            UpdateGrid();
        }

        /// Обновление данных в таблице с учетом текущих фильтров
        private void UpdateGrid()
        {
            var list = allProjects.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string searchText = SearchBox.Text.ToLower();
                list = list.Where(x => (x.ProjectName ?? "").ToLower().Contains(searchText) ||
                                       (x.Description ?? "").ToLower().Contains(searchText));
            }

            if (StatusFilter.SelectedIndex != -1 && StatusFilter.SelectedItem != null)
            {
                int statusId = (int)StatusFilter.SelectedValue;
                list = list.Where(x => x.StatusID == statusId);
            }

            if (PriorityFilter.SelectedIndex > 0 && PriorityFilter.SelectedItem != null)
            {
                string selectedPriority = PriorityFilter.SelectedItem.ToString();
                if (selectedPriority != "Все")
                {
                    list = list.Where(x => x.Priority == selectedPriority);
                }
            }

            ProjectsGrid.ItemsSource = list.ToList();
        }

        /// Обработчик загрузки строк таблицы
        private void ProjectsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Height = double.NaN;
        }

        /// Обработчик автоматического создания колонок
        private void ProjectsGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is DataGridTextColumn textColumn)
            {
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                textColumn.ElementStyle = style;
                textColumn.MinWidth = 80;

                if (textColumn.Binding is Binding binding)
                {
                    binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                }
            }
        }

        /// Обработчик двойного клика по строке таблицы
        private void ProjectsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProjectsGrid.SelectedItem is Projects selectedProject)
            {
                OpenProjectDetails(selectedProject.ProjectID);
            }
        }

        /// Открытие страницы деталей проекта
        private void OpenProjectDetails(int projectId)
        {
            var projectDetailsPage = new ProjectDetailsPage(projectId);
            NavigationService.Navigate(projectDetailsPage);
        }

        /// Обработчик нажатия кнопки создания проекта
        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для создания проектов!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var createProjectPage = new CreateProjectPage();
            NavigationService.Navigate(createProjectPage);
        }

        /// Обработчик нажатия кнопки "Открыть"
        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            int projectId = Convert.ToInt32((sender as Button).Tag);
            OpenProjectDetails(projectId);
        }

        /// Обработчик удаления проекта
        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для удаления проектов!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int projectId = Convert.ToInt32((sender as Button).Tag);

            var project = db.Projects.FirstOrDefault(p => p.ProjectID == projectId);
            if (project == null)
            {
                MessageBox.Show("Проект не найден.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить проект «{project.ProjectName}»?\nЭто действие удалит все связанные записи (задачи, этапы, участников и т.д.).",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var tasks = db.Tasks.Where(t => t.ProjectID == projectId).ToList();
                foreach (var task in tasks)
                    db.Tasks.Remove(task);

                var stages = db.ProjectStages.Where(s => s.ProjectID == projectId).ToList();
                foreach (var stage in stages)
                    db.ProjectStages.Remove(stage);

                var projectUsers = db.ProjectUsers.Where(x => x.ProjectID == projectId).ToList();
                foreach (var user in projectUsers)
                    db.ProjectUsers.Remove(user);

                var reports = db.Reports.Where(r => r.ProjectID == projectId).ToList();
                foreach (var report in reports)
                    db.Reports.Remove(report);

                db.Projects.Remove(project);
                db.SaveChanges();

                allProjects = db.Projects.ToList();
                UpdateGrid();

                MessageBox.Show("Проект успешно удалён.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}