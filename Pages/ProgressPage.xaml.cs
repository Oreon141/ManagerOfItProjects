using LiveCharts;
using LiveCharts.Wpf;
using ManagerOfItProjects.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ManagerOfItProjects.Pages
{
    public partial class ProgressPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private int currentChart = 0;
        private List<Tasks> tasks;
        private bool hasDataForCurrentChart = false;

        public ProgressPage()
        {
            InitializeComponent();
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ProjectFilter.ItemsSource = db.Projects.ToList();

                if (ProjectFilter.Items.Count > 0)
                    ProjectFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик изменения выбора проекта
        private void ProjectFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectFilter.SelectedItem is Projects selectedProject)
            {
                try
                {
                    tasks = db.Tasks
                              .Where(t => t.ProjectID == selectedProject.ProjectID)
                              .ToList();

                    DrawAll();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке задач проекта: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ClearCharts();
                ShowNoDataMessage();
            }
        }

        /// Очистка всех диаграмм
        private void ClearCharts()
        {
            PieChart.Series?.Clear();
            BarChart.Series?.Clear();
            GanttChart.Series?.Clear();

            if (BarChart.AxisX.Count > 0)
                BarChart.AxisX[0].Labels = null;

            if (GanttChart.AxisY.Count > 0)
                GanttChart.AxisY[0].Labels = null;
        }

        /// Показать сообщение "Нет данных"
        private void ShowNoDataMessage()
        {
            PieChart.Visibility = Visibility.Collapsed;
            BarChart.Visibility = Visibility.Collapsed;
            GanttChart.Visibility = Visibility.Collapsed;

            NoDataMessage.Visibility = Visibility.Visible;
            hasDataForCurrentChart = false;
        }

        /// Скрыть сообщение "Нет данных"
        private void HideNoDataMessage()
        {
            NoDataMessage.Visibility = Visibility.Collapsed;
            hasDataForCurrentChart = true;
        }

        /// Обработчик кнопки "Предыдущая диаграмма"
        private void PrevChart_Click(object sender, RoutedEventArgs e)
        {
            currentChart = (currentChart + 2) % 3;
            ShowChart(false);
        }

        /// Обработчик кнопки "Следующая диаграмма"
        private void NextChart_Click(object sender, RoutedEventArgs e)
        {
            currentChart = (currentChart + 1) % 3;
            ShowChart(true);
        }

        /// Анимированное переключение между диаграммами
        void ShowChart(bool forward = true)
        {
            CheckDataForCurrentChart();

            if (!hasDataForCurrentChart)
            {
                ShowNoDataMessage();
                return;
            }

            UIElement[] charts = { PieChart, BarChart, GanttChart };

            foreach (var chart in charts)
                chart.Visibility = Visibility.Collapsed;

            UIElement activeChart;
            string chartTitle;

            switch (currentChart)
            {
                case 0:
                    activeChart = PieChart;
                    chartTitle = "Задачи по статусам";
                    break;
                case 1:
                    activeChart = BarChart;
                    chartTitle = "Загрузка пользователей";
                    break;
                default:
                    activeChart = GanttChart;
                    chartTitle = "Этапы проекта (Gantt)";
                    break;
            }

            ChartTitle.Text = chartTitle;
            activeChart.Visibility = Visibility.Visible;
            HideNoDataMessage();

            var transform = activeChart.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                activeChart.RenderTransform = transform;
            }

            double from = forward ? 400 : -400;
            transform.X = from;

            var animation = new DoubleAnimation
            {
                From = from,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        /// Проверка наличия данных для текущей диаграммы
        private void CheckDataForCurrentChart()
        {
            if (tasks == null || tasks.Count == 0)
            {
                hasDataForCurrentChart = false;
                return;
            }

            switch (currentChart)
            {
                case 0:
                    var statusGroups = tasks
                        .Where(t => t.Statuses != null)
                        .GroupBy(t => t.Statuses.StatusName)
                        .ToList();
                    hasDataForCurrentChart = statusGroups.Count > 0;
                    break;

                case 1:
                    var userGroups = tasks
                        .Where(t => t.Users != null)
                        .GroupBy(t => t.Users.Login)
                        .ToList();
                    hasDataForCurrentChart = userGroups.Count > 0;
                    break;

                case 2:
                    var selectedProject = ProjectFilter.SelectedItem as Projects;
                    if (selectedProject == null)
                    {
                        hasDataForCurrentChart = false;
                        return;
                    }

                    var stages = db.ProjectStages
                        .Where(s => s.ProjectID == selectedProject.ProjectID &&
                                   s.PlannedStartDate.HasValue &&
                                   s.PlannedEndDate.HasValue)
                        .ToList();
                    hasDataForCurrentChart = stages.Count > 0;
                    break;
            }
        }

        /// Отрисовка всех диаграмм
        void DrawAll()
        {
            DrawPie();
            DrawBar();
            DrawGantt();
            ShowChart();
        }

        /// Получение цвета для статуса задачи
        SolidColorBrush ColorForStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return Brushes.Gray;

            string normalizedStatus = status.ToLower();

            if (normalizedStatus.Contains("в процессе"))
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#60a5fa");

            if (normalizedStatus.Contains("не нач"))
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#f87171");

            if (normalizedStatus.Contains("выполн"))
                return (SolidColorBrush)new BrushConverter().ConvertFrom("#34d399");

            return Brushes.Gray;
        }

        /// Отрисовка круговой диаграммы
        void DrawPie()
        {
            PieChart.Series = new SeriesCollection();

            var groups = tasks
                .Where(t => t.Statuses != null)
                .GroupBy(t => t.Statuses.StatusName)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            if (groups.Count == 0)
            {
                return;
            }

            foreach (var group in groups)
            {
                PieChart.Series.Add(new PieSeries
                {
                    Title = group.Status,
                    Values = new ChartValues<int> { group.Count },
                    DataLabels = true,
                    FontSize = 18,
                    Fill = ColorForStatus(group.Status)
                });
            }
        }

        /// Отрисовка столбчатой диаграммы
        void DrawBar()
        {
            BarChart.Series = new SeriesCollection();

            var data = tasks
                .Where(t => t.Users != null)
                .GroupBy(t => t.Users.Login)
                .Select(g => new { User = g.Key, Count = g.Count() })
                .ToList();

            if (data.Count == 0)
            {
                return;
            }

            BarChart.AxisX[0].Labels = data.Select(d => d.User).ToArray();

            BarChart.Series.Add(new ColumnSeries
            {
                Title = "Задачи",
                Values = new ChartValues<int>(data.Select(d => d.Count)),
                DataLabels = true,
                FontSize = 18,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#3b82f6")
            });
        }

        /// Отрисовка диаграммы Ганта
        void DrawGantt()
        {
            GanttChart.Series = new SeriesCollection();

            var selectedProject = ProjectFilter.SelectedItem as Projects;
            if (selectedProject == null)
                return;

            var stages = db.ProjectStages
                .Where(s => s.ProjectID == selectedProject.ProjectID &&
                           s.PlannedStartDate.HasValue &&
                           s.PlannedEndDate.HasValue)
                .OrderBy(s => s.StageNumber)
                .ToList();

            if (stages.Count == 0)
            {
                return;
            }

            GanttChart.AxisY[0].Labels = stages.Select(s => s.StageName).ToArray();

            var values = new ChartValues<double>();
            foreach (var stage in stages)
            {
                double days = (stage.PlannedEndDate.Value - stage.PlannedStartDate.Value).TotalDays;
                values.Add(days <= 0 ? 1 : days);
            }

            GanttChart.Series.Add(new RowSeries
            {
                Title = "Этапы проекта",
                Values = values,
                DataLabels = true,
                FontSize = 16,
                Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#60a5fa")
            });
        }
    }
}