using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagerOfItProjects.Models
{
    public static class CurrentUser
    {
        public static int UserID { get; set; }
        public static string Login { get; set; }
        public static string Role { get; set; }
        public static int RoleID { get; set; }

        public static bool CanManageUsers => Role == "Администратор";
        public static bool CanManageProjects => Role == "Администратор" || Role == "Менеджер проектов";
        public static bool CanManageTasks => Role == "Администратор" || Role == "Менеджер проектов";
        public static bool CanViewProjectsAndTasks => true;
        public static bool CanViewOnly => Role == "Разработчик" || Role == "Тестировщик" || Role == "Аналитик";
        public static bool CanGenerateReports => true;

        /// Инициализация данных текущего пользователя
        public static void Initialize(int userId, string login, string role, int roleId)
        {
            UserID = userId;
            Login = login;
            Role = role;
            RoleID = roleId;
        }

        /// Очистка данных пользователя (при выходе из системы)
        public static void Clear()
        {
            UserID = 0;
            Login = string.Empty;
            Role = string.Empty;
            RoleID = 0;
        }

        /// Проверка, является ли пользователь администратором
        public static bool IsAdmin() => Role == "Администратор";

        /// Проверка, является ли пользователь менеджером проектов
        public static bool IsProjectManager() => Role == "Менеджер проектов";

        /// Проверка, имеет ли пользователь права только на просмотр
        public static bool HasViewOnlyRights() => CanViewOnly;
    }
}