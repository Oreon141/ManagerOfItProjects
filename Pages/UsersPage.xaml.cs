using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{
    public partial class UsersPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        Users selectedUser;

        public UsersPage()
        {
            InitializeComponent();

            if (!CurrentUser.CanManageUsers)
            {
                MessageBox.Show("Доступ к странице пользователей запрещен! Требуются права администратора.",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadRoles();
                LoadUsers();
                UpdateButtonsState();
                ConfigureUIByRole();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Настройка видимости элементов интерфейса в зависимости от роли
        private void ConfigureUIByRole()
        {
            if (!CurrentUser.IsAdmin())
            {
                var rightBorder = this.FindName("RightPanel") as Border;
                if (rightBorder != null)
                    rightBorder.Visibility = Visibility.Collapsed;

                UsersGrid.IsReadOnly = true;
            }
        }

        /// Загрузка списка ролей пользователей
        void LoadRoles()
        {
            try
            {
                var roles = db.UserRoles.ToList();
                RoleFilter.ItemsSource = roles;
                RoleBox.ItemsSource = roles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Загрузка списка пользователей с фильтрацией
        void LoadUsers()
        {
            try
            {
                var list = db.Users.AsNoTracking().ToList();

                var selectedRole = RoleFilter.SelectedItem as UserRoles;
                if (selectedRole != null)
                {
                    int roleId = selectedRole.RoleID;
                    list = list.Where(u => u.RoleID == roleId).ToList();
                }

                UsersGrid.ItemsSource = list;
                UpdateRowHeights();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обновление высоты строк таблицы
        private void UpdateRowHeights()
        {
            try
            {
                foreach (var item in UsersGrid.Items)
                {
                    var row = UsersGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (row != null)
                    {
                        row.Height = double.NaN;
                    }
                }
            }
            catch { }
        }

        /// Обработчик изменения фильтра по роли
        private void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadUsers();
        }

        /// Обработчик кнопки сброса выбора и фильтров
        private void ResetSelection_Click(object sender, RoutedEventArgs e)
        {
            RoleFilter.SelectedIndex = -1;
            UsersGrid.SelectedItem = null;
            selectedUser = null;
            ClearForm();
            LoadUsers();
            UpdateButtonsState();
            ClearValidationErrors();
        }

        /// Обработчик выбора пользователя в таблице
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                selectedUser = UsersGrid.SelectedItem as Users;

                if (selectedUser == null)
                {
                    ClearForm();
                    UpdateButtonsState();
                    return;
                }

                if (selectedUser.UserID == CurrentUser.UserID)
                {
                    MessageBox.Show("Вы не можете редактировать свою собственную учетную запись!",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    UsersGrid.SelectedItem = null;
                    selectedUser = null;
                    ClearForm();
                    UpdateButtonsState();
                    return;
                }

                var fullUser = db.Users.AsNoTracking().FirstOrDefault(u => u.UserID == selectedUser.UserID);
                if (fullUser == null)
                {
                    ClearForm();
                    UpdateButtonsState();
                    return;
                }

                SurnameBox.Text = fullUser.Surname;
                FirstNameBox.Text = fullUser.FirstName;
                LastNameBox.Text = fullUser.LastName;
                EmailBox.Text = fullUser.Email;
                LoginBox.Text = fullUser.Login;
                PasswordBox.Password = "";

                if (RoleBox.ItemsSource is System.Collections.IList roleList)
                {
                    foreach (var role in roleList)
                    {
                        if (role is UserRoles userRole && userRole.RoleID == fullUser.RoleID)
                        {
                            RoleBox.SelectedItem = role;
                            break;
                        }
                    }
                }

                UpdateButtonsState();
                ClearValidationErrors();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обновление состояния кнопок
        private void UpdateButtonsState()
        {
            if (selectedUser == null)
            {
                CreateButton.IsEnabled = true;
                SaveButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
            else
            {
                CreateButton.IsEnabled = false;
                SaveButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
        }

        /// Очистка формы
        private void ClearForm()
        {
            SurnameBox.Text = "";
            FirstNameBox.Text = "";
            LastNameBox.Text = "";
            EmailBox.Text = "";
            LoginBox.Text = "";
            PasswordBox.Password = "";
            RoleBox.SelectedItem = null;
        }

        /// Валидация полей формы
        private void ValidateFields(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isValid = true;
                string errorMessage = "";

                ClearValidationErrors();

                string surname = SurnameBox.Text?.Trim() ?? "";
                string firstName = FirstNameBox.Text?.Trim() ?? "";
                string lastName = LastNameBox.Text?.Trim() ?? "";
                string email = EmailBox.Text?.Trim() ?? "";
                string login = LoginBox.Text?.Trim() ?? "";
                string password = PasswordBox.Password;
                var role = RoleBox.SelectedItem as UserRoles;
                int? currentUserId = selectedUser?.UserID;

                if (string.IsNullOrWhiteSpace(surname))
                {
                    SurnameError.Text = "Фамилия обязательна для заполнения";
                    SurnameError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Фамилия обязательна для заполнения\n";
                }
                else if (surname.Length < 2)
                {
                    SurnameError.Text = "Фамилия должна содержать минимум 2 символа";
                    SurnameError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Фамилия должна содержать минимум 2 символа\n";
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    FirstNameError.Text = "Имя обязательно для заполнения";
                    FirstNameError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Имя обязательно для заполнения\n";
                }
                else if (firstName.Length < 2)
                {
                    FirstNameError.Text = "Имя должно содержать минимум 2 символа";
                    FirstNameError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Имя должно содержать минимум 2 символа\n";
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    EmailError.Text = "Email обязателен для заполнения";
                    EmailError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Email обязателен для заполнения\n";
                }
                else if (!IsValidEmail(email))
                {
                    EmailError.Text = "Введите корректный email адрес";
                    EmailError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Введите корректный email адрес\n";
                }
                else if (!IsEmailUnique(email, currentUserId))
                {
                    EmailError.Text = "Этот email уже используется другим пользователем";
                    EmailError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Этот email уже используется другим пользователем\n";
                }

                if (string.IsNullOrWhiteSpace(login))
                {
                    LoginError.Text = "Логин обязателен для заполнения";
                    LoginError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Логин обязателен для заполнения\n";
                }
                else if (login.Length < 3)
                {
                    LoginError.Text = "Логин должен содержать минимум 3 символа";
                    LoginError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Логин должен содержать минимум 3 символа\n";
                }
                else if (!IsLoginUnique(login, currentUserId))
                {
                    LoginError.Text = "Этот логин уже используется другим пользователем";
                    LoginError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Этот логин уже используется другим пользователем\n";
                }

                if (currentUserId == null)
                {
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        PasswordError.Text = "Пароль обязателен для заполнения";
                        PasswordError.Visibility = Visibility.Visible;
                        isValid = false;
                        errorMessage += "• Пароль обязателен для заполнения\n";
                    }
                    else if (password.Length < 6)
                    {
                        PasswordError.Text = "Пароль должен содержать минимум 6 символов";
                        PasswordError.Visibility = Visibility.Visible;
                        isValid = false;
                        errorMessage += "• Пароль должен содержать минимум 6 символов\n";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
                {
                    PasswordError.Text = "Пароль должен содержать минимум 6 символов";
                    PasswordError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Пароль должен содержать минимум 6 символов\n";
                }

                if (role == null)
                {
                    RoleError.Text = "Роль обязательна для выбора";
                    RoleError.Visibility = Visibility.Visible;
                    isValid = false;
                    errorMessage += "• Роль обязательна для выбора\n";
                }

                if (isValid && currentUserId == null)
                {
                    bool isDuplicate = CheckForDuplicateUser(surname, firstName, email);
                    if (isDuplicate)
                    {
                        isValid = false;
                        errorMessage += "• Пользователь с такой фамилией, именем и email уже существует\n";
                        ValidationSummary.Text = "Пользователь с такими данными уже существует!";
                        ValidationSummary.Visibility = Visibility.Visible;
                    }
                }

                if (!isValid)
                {
                    if (string.IsNullOrEmpty(ValidationSummary.Text))
                    {
                        ValidationSummary.Text = errorMessage.TrimEnd();
                        ValidationSummary.Visibility = Visibility.Visible;
                    }

                    if (currentUserId == null)
                    {
                        CreateButton.IsEnabled = false;
                    }
                    else
                    {
                        SaveButton.IsEnabled = false;
                    }
                }
                else
                {
                    ValidationSummary.Visibility = Visibility.Collapsed;

                    if (currentUserId == null)
                    {
                        CreateButton.IsEnabled = true;
                    }
                    else
                    {
                        SaveButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка валидации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Проверка на дублирование пользователя
        private bool CheckForDuplicateUser(string surname, string firstName, string email)
        {
            try
            {
                return db.Users.AsNoTracking()
                    .Any(u => u.Surname.Equals(surname, StringComparison.OrdinalIgnoreCase) &&
                             u.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase) &&
                             u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки дублирования пользователя: {ex.Message}");
                return false;
            }
        }

        /// Очистка сообщений об ошибках валидации
        private void ClearValidationErrors()
        {
            SurnameError.Visibility = Visibility.Collapsed;
            FirstNameError.Visibility = Visibility.Collapsed;
            EmailError.Visibility = Visibility.Collapsed;
            LoginError.Visibility = Visibility.Collapsed;
            PasswordError.Visibility = Visibility.Collapsed;
            RoleError.Visibility = Visibility.Collapsed;
            ValidationSummary.Visibility = Visibility.Collapsed;
            ValidationSummary.Text = "";
        }

        /// Проверка валидности email с помощью регулярного выражения
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// Проверка уникальности email
        private bool IsEmailUnique(string email, int? excludeUserId = null)
        {
            try
            {
                if (excludeUserId.HasValue)
                {
                    return !db.Users.AsNoTracking()
                        .Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                                 u.UserID != excludeUserId.Value);
                }
                else
                {
                    return !db.Users.AsNoTracking()
                        .Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки email: {ex.Message}");
                return true;
            }
        }

        /// Проверка уникальности логина
        private bool IsLoginUnique(string login, int? excludeUserId = null)
        {
            try
            {
                if (excludeUserId.HasValue)
                {
                    return !db.Users.AsNoTracking()
                        .Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase) &&
                                 u.UserID != excludeUserId.Value);
                }
                else
                {
                    return !db.Users.AsNoTracking()
                        .Any(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки логина: {ex.Message}");
                return true;
            }
        }

        /// Создание нового пользователя
        private void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CurrentUser.IsAdmin())
                {
                    MessageBox.Show("Только администратор может создавать новых пользователей!",
                        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ValidateFields(sender, e);

                if (ValidationSummary.Visibility == Visibility.Visible)
                {
                    MessageBox.Show("Исправьте ошибки в форме перед созданием пользователя",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var role = RoleBox.SelectedItem as UserRoles;

                if (role == null)
                {
                    MessageBox.Show("Выберите роль для пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string surname = SurnameBox.Text.Trim();
                string firstName = FirstNameBox.Text.Trim();
                string email = EmailBox.Text.Trim();

                if (CheckForDuplicateUser(surname, firstName, email))
                {
                    MessageBox.Show("Пользователь с такими данными уже существует!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = new Users
                {
                    Surname = surname,
                    FirstName = firstName,
                    LastName = LastNameBox.Text?.Trim(),
                    Email = email,
                    Login = LoginBox.Text.Trim(),
                    Password = PasswordBox.Password,
                    RoleID = role.RoleID
                };

                db.Users.Add(user);
                db.SaveChanges();

                LoadUsers();
                ClearForm();
                ClearValidationErrors();
                selectedUser = null;
                UpdateButtonsState();

                MessageBox.Show("Пользователь успешно создан!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Сохранение изменений пользователя
        private void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CurrentUser.IsAdmin())
                {
                    MessageBox.Show("Только администратор может редактировать пользователей!",
                        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedUser == null)
                {
                    MessageBox.Show("Выберите пользователя для редактирования!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ValidateFields(sender, e);

                if (ValidationSummary.Visibility == Visibility.Visible)
                {
                    MessageBox.Show("Исправьте ошибки в форме перед сохранением",
                        "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var role = RoleBox.SelectedItem as UserRoles;

                if (role == null)
                {
                    MessageBox.Show("Выберите роль для пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string surname = SurnameBox.Text.Trim();
                string firstName = FirstNameBox.Text.Trim();
                string email = EmailBox.Text.Trim();

                bool isDuplicate = db.Users.AsNoTracking()
                    .Any(u => u.Surname.Equals(surname, StringComparison.OrdinalIgnoreCase) &&
                             u.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase) &&
                             u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                             u.UserID != selectedUser.UserID);

                if (isDuplicate)
                {
                    MessageBox.Show("Пользователь с такими данными уже существует!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var userToUpdate = db.Users.FirstOrDefault(u => u.UserID == selectedUser.UserID);
                if (userToUpdate == null)
                {
                    MessageBox.Show("Пользователь не найден в базе данных",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                userToUpdate.Surname = surname;
                userToUpdate.FirstName = firstName;
                userToUpdate.LastName = LastNameBox.Text?.Trim();
                userToUpdate.Email = email;
                userToUpdate.Login = LoginBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    userToUpdate.Password = PasswordBox.Password;
                }

                userToUpdate.RoleID = role.RoleID;

                db.SaveChanges();
                LoadUsers();
                ClearValidationErrors();

                MessageBox.Show("Изменения успешно сохранены!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Удаление пользователя
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CurrentUser.IsAdmin())
                {
                    MessageBox.Show("Только администратор может удалять пользователей!",
                        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedUser == null)
                {
                    MessageBox.Show("Выберите пользователя для удаления!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int userId = selectedUser.UserID;

                if (userId == CurrentUser.UserID)
                {
                    MessageBox.Show("Вы не можете удалить свою собственную учетную запись!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool hasTasks = db.Tasks.Any(t => t.AssignedTo == userId);
                bool hasProjects = db.ProjectUsers.Any(pu => pu.UserID == userId);

                if (hasTasks || hasProjects)
                {
                    string message = "Невозможно удалить пользователя, так как он связан с:\n";
                    if (hasTasks) message += "• Задачами\n";
                    if (hasProjects) message += "• Проектами\n";
                    message += "\nСначала удалите связанные записи.";

                    MessageBox.Show(message,
                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Вы уверены, что хотите удалить пользователя {selectedUser.FirstName} {selectedUser.Surname}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var userToDelete = db.Users.FirstOrDefault(u => u.UserID == userId);
                    if (userToDelete != null)
                    {
                        db.Users.Remove(userToDelete);
                        db.SaveChanges();

                        LoadUsers();
                        ClearForm();
                        ClearValidationErrors();
                        selectedUser = null;
                        UpdateButtonsState();

                        MessageBox.Show("Пользователь успешно удалён!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Пользователь не найден в базе данных",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик загрузки строк таблицы
        private void UsersGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Height = double.NaN;
        }

        /// Обработчик изменения размера страницы
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRowHeights();
        }
    }
}