using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class ProjectStagesPage : Page
    {
        private readonly ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private int _projectId;
        private List<ProjectStages> allStages;

        public ProjectStagesPage(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;

            ConfigureActionButtons();
        }

        /// Настройка видимости кнопок действий в зависимости от роли пользователя
        private void ConfigureActionButtons()
        {
            var createButton = this.FindName("CreateStageBtn") as Button;
            if (createButton != null)
            {
                createButton.Visibility = CurrentUser.CanManageProjects ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            if (CurrentUser.CanViewOnly)
            {
                foreach (var column in StagesGrid.Columns)
                {
                    if (column.Header.ToString() == "Действия")
                    {
                        column.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusViewFilter.ItemsSource = db.Statuses.ToList();
                StatusViewFilter.DisplayMemberPath = "StatusName";

                allStages = db.ProjectStages
                              .Where(s => s.ProjectID == _projectId)
                              .OrderBy(s => s.StageNumber)
                              .ToList();

                LoadStages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик изменения фильтров
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            LoadStages();
        }

        /// Обработчик сброса всех фильтров
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StatusViewFilter.SelectedIndex = -1;
            LoadStages();
        }

        /// Загрузка этапов с применением текущих фильтров
        private void LoadStages()
        {
            if (StagesGrid == null)
                return;

            try
            {
                var query = allStages.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    string searchText = SearchBox.Text.ToLower();

                    query = query.Where(s =>
                        s.StageName.ToLower().Contains(searchText) ||
                        s.StageDescription.ToLower().Contains(searchText));
                }

                if (StatusViewFilter.SelectedItem is Statuses selectedStatus)
                {
                    query = query.Where(s => s.StatusID == selectedStatus.StatusID);
                }

                StagesGrid.ItemsSource = query.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик кнопки "Назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        /// Обработчик кнопки создания нового этапа
        private void AddStage_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для создания этапов проекта!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new AddEditStagePage(_projectId));
        }

        /// Обработчик двойного клика по строке таблицы
        private void StagesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (StagesGrid.SelectedItem is ProjectStages selectedStage)
            {
                if (!CurrentUser.CanManageProjects)
                {
                    MessageBox.Show("У вас нет прав для редактирования этапов проекта!",
                        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                NavigationService.Navigate(new AddEditStagePage(selectedStage));
            }
        }

        /// Обработчик нажатия кнопки "Открыть"
        private void OpenStage_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для редактирования этапов проекта!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            if (!int.TryParse(button.Tag.ToString(), out int stageId))
                return;

            var stage = allStages.FirstOrDefault(s => s.StageID == stageId);
            if (stage != null)
            {
                NavigationService.Navigate(new AddEditStagePage(stage));
            }
        }

        /// Удаление этапа
        private void DeleteStage_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для удаления этапов проекта!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            if (!int.TryParse(button.Tag.ToString(), out int stageId))
                return;

            var stage = db.ProjectStages.FirstOrDefault(s => s.StageID == stageId);
            if (stage == null)
            {
                MessageBox.Show("Этап не найден.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить этап «{stage.StageName}»?\nВсе связанные данные будут также удалены.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                db.ProjectStages.Remove(stage);
                db.SaveChanges();

                allStages = db.ProjectStages
                              .Where(s => s.ProjectID == _projectId)
                              .OrderBy(s => s.StageNumber)
                              .ToList();
                LoadStages();

                MessageBox.Show("Этап успешно удалён.",
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