using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class ProjectMembersPage : Page
    {
        private readonly ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private int _projectId;

        public ProjectMembersPage(int projectId)
        {
            InitializeComponent();
            _projectId = projectId;

            LoadMembers();
            LoadUsers();
            LoadRoles();

            RoleFilter.ItemsSource = db.ProjectRole.ToList();

            ConfigureActionButtons();
        }

        /// Настройка видимости кнопок действий в зависимости от роли пользователя
        private void ConfigureActionButtons()
        {
            var rightPanel = this.FindName("RightPanel") as Border;
            if (rightPanel != null)
            {
                rightPanel.Visibility = CurrentUser.CanManageProjects ?
                    Visibility.Visible : Visibility.Collapsed;
            }

            if (CurrentUser.CanViewOnly)
            {
                foreach (var column in MembersGrid.Columns)
                {
                    if (column.Header.ToString() == "Удалить")
                    {
                        column.Visibility = Visibility.Collapsed;
                        break;
                    }
                }
            }
        }

        /// Загрузка списка участников проекта
        void LoadMembers()
        {
            try
            {
                var members = db.ProjectUsers
                    .Where(p => p.ProjectID == _projectId)
                    .Select(m => new MemberVM
                    {
                        ProjectUserID = m.ProjectUserID,
                        FullName = m.Users.Surname + " " + m.Users.FirstName + " " + m.Users.LastName,
                        RoleName = m.ProjectRole.ProjectRoleName,
                        RoleId = m.ProjectRoleID
                    })
                    .ToList();

                MembersGrid.ItemsSource = members;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке участников: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Загрузка пользователей, которых можно добавить в проект
        void LoadUsers()
        {
            try
            {
                var existingUsers = db.ProjectUsers
                    .Where(p => p.ProjectID == _projectId)
                    .Select(p => p.UserID)
                    .ToList();

                UserBox.ItemsSource = db.Users
                    .Where(u => !existingUsers.Contains(u.UserID))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Загрузка списка ролей
        void LoadRoles()
        {
            try
            {
                RoleBox.ItemsSource = db.ProjectRole.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик изменения фильтров (поиск и фильтр по роли)
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var members = db.ProjectUsers
                    .Where(p => p.ProjectID == _projectId)
                    .Select(m => new MemberVM
                    {
                        ProjectUserID = m.ProjectUserID,
                        FullName = m.Users.Surname + " " + m.Users.FirstName + " " + m.Users.LastName,
                        RoleName = m.ProjectRole.ProjectRoleName,
                        RoleId = m.ProjectRoleID
                    });

                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    string searchText = SearchBox.Text.ToLower();
                    members = members.Where(m => m.FullName.ToLower().Contains(searchText));
                }

                if (RoleFilter.SelectedItem is ProjectRole selectedRole)
                {
                    members = members.Where(m => m.RoleId == selectedRole.ProjectRoleID);
                }

                MembersGrid.ItemsSource = members.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Сброс всех фильтров
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            RoleFilter.SelectedIndex = -1;
            LoadMembers();
        }

        /// Добавление нового участника в проект
        private void AddMember_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для добавления участников проекта!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UserBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UserBox.Focus();
                return;
            }

            if (RoleBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                RoleBox.Focus();
                return;
            }

            try
            {
                var member = new ProjectUsers
                {
                    ProjectID = _projectId,
                    UserID = (int)UserBox.SelectedValue,
                    ProjectRoleID = (int)RoleBox.SelectedValue,
                    AssignedDate = DateTime.Now
                };

                db.ProjectUsers.Add(member);
                db.SaveChanges();

                LoadMembers();
                LoadUsers();

                MessageBox.Show("Участник успешно добавлен в проект.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении участника: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Удаление участника из проекта
        private void DeleteMember_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageProjects)
            {
                MessageBox.Show("У вас нет прав для удаления участников проекта!",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            if (!int.TryParse(button.Tag.ToString(), out int memberId))
                return;

            var member = db.ProjectUsers.FirstOrDefault(m => m.ProjectUserID == memberId);
            if (member == null)
            {
                MessageBox.Show("Участник не найден.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                "Удалить участника проекта?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                db.ProjectUsers.Remove(member);
                db.SaveChanges();

                LoadMembers();
                LoadUsers();

                MessageBox.Show("Участник успешно удален.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении участника: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }

    public class MemberVM
    {
        public int ProjectUserID { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public int RoleId { get; set; }
    }
}