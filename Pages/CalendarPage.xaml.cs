using ManagerOfItProjects.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ManagerOfItProjects.Pages
{
    public partial class CalendarPage : Page
    {
        private readonly ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private DateTime currentDate;
        private List<Projects> allProjects;

        // Модель для отображения дня в календаре
        public class CalendarDay
        {
            public int DayNumber { get; set; }
            public DateTime Date { get; set; }
            public bool IsCurrentMonth { get; set; }
            public bool IsToday { get; set; }
            public bool HasDeadline { get; set; }
            public Brush DayBackground { get; set; } = Brushes.Transparent;
            public Brush TextColor { get; set; } = Brushes.Black;
            public FontWeight FontWeight { get; set; } = FontWeights.Normal;
        }

        // Модель для отображения проекта с дедлайном
        public class ProjectDeadlineView
        {
            public string ProjectName { get; set; }
            public DateTime Deadline { get; set; }
            public string Description { get; set; }
            public string Priority { get; set; }
            public string FormattedDeadline => Deadline.ToString("dd MMMM yyyy", new CultureInfo("ru-RU"));
        }

        public CalendarPage()
        {
            InitializeComponent();
            currentDate = DateTime.Today;
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProjects();
            GenerateCalendar();
            SelectDay(DateTime.Today);
        }

        /// Загрузка всех проектов из базы данных
        private void LoadProjects()
        {
            allProjects = db.Projects.ToList();
        }

        /// Генерация календаря на текущий месяц
        private void GenerateCalendar()
        {
            var days = new ObservableCollection<CalendarDay>();

            var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            int firstDayWeekOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            for (int i = 0; i < firstDayWeekOffset; i++)
            {
                var date = firstDayOfMonth.AddDays(-(firstDayWeekOffset - i));
                var hasDeadline = HasDeadlineOnDate(date);

                var day = new CalendarDay
                {
                    DayNumber = date.Day,
                    Date = date,
                    IsCurrentMonth = false,
                    IsToday = date.Date == DateTime.Today.Date,
                    HasDeadline = hasDeadline
                };

                if (hasDeadline)
                {
                    day.DayBackground = new SolidColorBrush(Color.FromArgb(50, 255, 235, 238));
                    day.TextColor = Brushes.DarkRed;
                    day.FontWeight = FontWeights.Bold;
                }
                else
                {
                    day.TextColor = Brushes.Gray;
                }

                days.Add(day);
            }

            int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
            for (int i = 1; i <= daysInMonth; i++)
            {
                var date = new DateTime(currentDate.Year, currentDate.Month, i);
                var isToday = date.Date == DateTime.Today.Date;
                var hasDeadline = HasDeadlineOnDate(date);

                var day = new CalendarDay
                {
                    DayNumber = date.Day,
                    Date = date,
                    IsCurrentMonth = true,
                    IsToday = isToday,
                    HasDeadline = hasDeadline
                };

                if (hasDeadline)
                {
                    day.DayBackground = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54));
                    day.TextColor = Brushes.White;
                    day.FontWeight = FontWeights.Bold;
                }
                else if (isToday)
                {
                    day.DayBackground = new SolidColorBrush(Color.FromArgb(50, 33, 150, 243));
                    day.TextColor = Brushes.White;
                    day.FontWeight = FontWeights.Bold;
                }
                else if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    day.TextColor = Brushes.Blue;
                }
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    day.TextColor = Brushes.Red;
                }

                days.Add(day);
            }

            int totalCells = 42;
            int remainingCells = totalCells - days.Count;

            for (int i = 1; i <= remainingCells; i++)
            {
                var date = new DateTime(currentDate.Year, currentDate.Month, daysInMonth).AddDays(i);
                var hasDeadline = HasDeadlineOnDate(date);

                var day = new CalendarDay
                {
                    DayNumber = date.Day,
                    Date = date,
                    IsCurrentMonth = false,
                    IsToday = date.Date == DateTime.Today.Date,
                    HasDeadline = hasDeadline
                };

                if (hasDeadline)
                {
                    day.DayBackground = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    day.TextColor = Brushes.DarkRed;
                    day.FontWeight = FontWeights.Bold;
                }
                else
                {
                    day.TextColor = Brushes.Gray;
                }

                days.Add(day);
            }

            CalendarDaysControl.ItemsSource = days;
            UpdateMonthText();
        }

        /// Проверка наличия дедлайнов на указанную дату
        private bool HasDeadlineOnDate(DateTime date)
        {
            return allProjects?.Any(p => p.Deadline.Date == date.Date) ?? false;
        }

        /// Выбор дня и отображение дедлайнов на эту дату
        private void SelectDay(DateTime date)
        {
            currentDate = date;
            SelectedDateText.Text = date.ToString("dddd, d MMMM yyyy", new CultureInfo("ru-RU"));

            var projectsOnDate = allProjects
                .Where(p => p.Deadline.Date == date.Date)
                .Select(p => new ProjectDeadlineView
                {
                    ProjectName = p.ProjectName,
                    Deadline = p.Deadline,
                    Description = p.Description ?? "Описание отсутствует",
                    Priority = p.Priority ?? "Не указан"
                })
                .ToList();

            EventsList.ItemsSource = projectsOnDate.Count > 0
                ? projectsOnDate
                : new List<ProjectDeadlineView>
                {
                    new ProjectDeadlineView
                    {
                        ProjectName = "Нет дедлайнов",
                        Description = "На эту дату не запланировано дедлайнов проектов",
                        Priority = "",
                        Deadline = date
                    }
                };
        }

        /// Обработчик клика по дню календаря
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DateTime date)
            {
                SelectDay(date);
            }
        }

        /// Переход к предыдущему месяцу
        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddMonths(-1);
            GenerateCalendar();
        }

        /// Переход к следующему месяцу
        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddMonths(1);
            GenerateCalendar();
        }

        /// Переход к предыдущему году
        private void BtnPrevYear_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddYears(-1);
            GenerateCalendar();
        }

        /// Переход к следующему году
        private void BtnNextYear_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddYears(1);
            GenerateCalendar();
        }

        /// Обновление текста с названием текущего месяца
        private void UpdateMonthText()
        {
            var culture = new CultureInfo("ru-RU");
            CurrentMonthText.Text = currentDate.ToString("MMMM yyyy", culture).ToUpper();
        }

        /// Обработчик клика по проекту для перехода к деталям
        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProjectDeadlineView projectView)
            {
                var project = db.Projects.FirstOrDefault(p => p.ProjectName == projectView.ProjectName &&
                                                               p.Deadline == projectView.Deadline);

                if (project != null)
                {
                    var projectDetailsPage = new ProjectDetailsPage(project.ProjectID);
                    NavigationService.Navigate(projectDetailsPage);
                }
            }
        }
    }
}