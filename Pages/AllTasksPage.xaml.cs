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
    public partial class AllTasksPage : Page
    {
        private ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private List<Tasks> allTasks;

        public AllTasksPage()
        {
            InitializeComponent();
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            allTasks = db.Tasks.ToList();

            ProjectFilter.ItemsSource = db.Projects.ToList();
            ProjectFilter.DisplayMemberPath = "ProjectName";
            ProjectFilter.SelectedValuePath = "ProjectID";

            StatusFilter.ItemsSource = db.Statuses.ToList();
            StatusFilter.DisplayMemberPath = "StatusName";
            StatusFilter.SelectedValuePath = "StatusID";

            ConfigureActionButtons();
            UpdateGrid();
        }

        /// Настройка видимости кнопок действий в зависимости от роли пользователя
        private void ConfigureActionButtons()
        {
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

        /// Обработчик изменения фильтров
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            UpdateGrid();
        }

        /// Обработчик сброса всех фильтров
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            ProjectFilter.SelectedIndex = -1;
            StatusFilter.SelectedIndex = -1;
            UpdateGrid();
        }

        /// Обновление данных в таблице с учетом текущих фильтров
        private void UpdateGrid()
        {
            var list = allTasks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                list = list.Where(x => x.TaskName.ToLower().Contains(SearchBox.Text.ToLower()));

            if (ProjectFilter.SelectedIndex != -1)
            {
                int pid = (int)ProjectFilter.SelectedValue;
                list = list.Where(x => x.ProjectID == pid);
            }

            if (StatusFilter.SelectedIndex != -1)
            {
                int sid = (int)StatusFilter.SelectedValue;
                list = list.Where(x => x.StatusID == sid);
            }

            TasksGrid.ItemsSource = list.ToList();
        }

        /// Обработчик загрузки строк таблицы
        private void TasksGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Height = double.NaN;
        }

        /// Обработчик автоматического создания колонок
        private void TasksGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is DataGridTextColumn textColumn)
            {
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));

                string header = e.Column.Header.ToString();
                if (header == "Проект" || header == "Исполнитель" ||
                    header == "Дата создания" || header == "Дедлайн" ||
                    header == "Статус")
                {
                    style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                }

                textColumn.ElementStyle = style;
                textColumn.MinWidth = 80;

                if (textColumn.Binding is Binding binding)
                {
                    binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                }
            }
        }

        /// Обработчик двойного клика по строке таблицы
        private void TasksGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TasksGrid.SelectedItem is Tasks selectedTask)
            {
                OpenTaskDetails(selectedTask.TaskID);
            }
        }

        /// Обработчик клика по кнопке "Открыть"
        private void OpenTask_Click(object sender, RoutedEventArgs e)
        {
            int taskId = Convert.ToInt32((sender as Button).Tag);
            OpenTaskDetails(taskId);
        }

        /// Открытие страницы деталей задачи
        private void OpenTaskDetails(int taskId)
        {
            var taskDetailsPage = new TaskDetails(taskId);
            NavigationService.Navigate(taskDetailsPage);
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

            int taskId = Convert.ToInt32((sender as Button).Tag);

            var task = db.Tasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task == null)
            {
                MessageBox.Show("Задача не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить задачу «{task.TaskName}»?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var taskComments = db.TaskComments.Where(tc => tc.TaskID == taskId).ToList();
                foreach (var tc in taskComments)
                    db.TaskComments.Remove(tc);

                db.Tasks.Remove(task);
                db.SaveChanges();

                allTasks = db.Tasks.ToList();
                UpdateGrid();

                MessageBox.Show("Задача успешно удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}