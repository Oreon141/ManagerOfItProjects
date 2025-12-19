using ManagerOfItProjects.DataBase;
using ManagerOfItProjects.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace ManagerOfItProjects.Pages
{
    public partial class ReportsPage : Page
    {
        ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();

        public ReportsPage()
        {
            InitializeComponent();
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadReportTypes();
                LoadProjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик автоматического создания колонок DataGrid
        private void ReportGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is DataGridTextColumn textColumn)
            {
                var elementStyle = new Style(typeof(TextBlock));
                elementStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                elementStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
                elementStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                elementStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(2)));

                textColumn.ElementStyle = elementStyle;
                textColumn.MinWidth = 100;

                string header = e.Column.Header.ToString();
                if (header.Contains("Название") || header.Contains("Описание"))
                {
                    textColumn.Width = new DataGridLength(2, DataGridLengthUnitType.Star);
                }
                else if (header.Contains("Пользователь") || header.Contains("Исполнитель"))
                {
                    textColumn.Width = new DataGridLength(1.5, DataGridLengthUnitType.Star);
                }
                else
                {
                    textColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                }

                if (textColumn.Binding is Binding binding)
                {
                    binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                }
            }
            else if (e.Column is DataGridTemplateColumn templateColumn)
            {
                templateColumn.CellStyle = FindResource("ReportDataGridCellStyle") as Style;
            }

            e.Column.HeaderStyle = FindResource("ReportDataGridColumnHeaderStyle") as Style;
        }

        /// Обработчик загрузки строк DataGrid
        private void ReportGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Height = Double.NaN;
            e.Row.Loaded += Row_Loaded;
        }

        /// Обработчик загрузки строки для вычисления высоты
        private void Row_Loaded(object sender, RoutedEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row == null) return;

            var dataGrid = FindVisualParent<DataGrid>(row);
            if (dataGrid == null) return;

            var cellsPresenter = GetVisualChild<DataGridCellsPresenter>(row);
            if (cellsPresenter == null) return;

            double maxHeight = 0;

            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                var cell = cellsPresenter.ItemContainerGenerator.ContainerFromIndex(i) as DataGridCell;
                if (cell != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(cell);
                    if (textBlock != null)
                    {
                        textBlock.Measure(new Size(cell.ActualWidth, double.PositiveInfinity));
                        maxHeight = Math.Max(maxHeight, textBlock.DesiredSize.Height);
                    }
                }
            }

            if (maxHeight > 0)
            {
                row.Height = maxHeight + 12;
            }
        }

        /// Вспомогательный метод для поиска визуального родителя
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        /// Вспомогательный метод для поиска визуального дочернего элемента
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        /// Вспомогательный метод для получения визуального дочернего элемента определенного типа
        private T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
            }
            return null;
        }

        /// Загрузка типов отчётов
        private void LoadReportTypes()
        {
            ReportTypeCombo.ItemsSource = new[]
            {
                "Все задачи проекта",
                "Этапы проекта",
                "Пользователи и их загрузка"
            };
            ReportTypeCombo.SelectedIndex = 0;
        }

        /// Загрузка списка проектов
        private void LoadProjects()
        {
            var projectsList = db.Projects.ToList();
            ProjectFilter.ItemsSource = projectsList;
            if (projectsList.Any())
                ProjectFilter.SelectedItem = projectsList.Last();
        }

        /// Обработчик изменения типа отчёта
        private void ReportTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedType = ReportTypeCombo.SelectedItem as string;

            ProjectFilterContainer.Visibility =
                selectedType == "Все задачи проекта" ||
                selectedType == "Этапы проекта"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// Генерация отчёта на основе выбранного типа
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            string selectedType = ReportTypeCombo.SelectedItem as string;

            try
            {
                switch (selectedType)
                {
                    case "Все задачи проекта":
                        Generate_TasksByProject();
                        break;
                    case "Этапы проекта":
                        Generate_StagesByProject();
                        break;
                    case "Пользователи и их загрузка":
                        Generate_UserLoad();
                        break;
                    default:
                        MessageBox.Show("Выберите тип отчёта.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации отчёта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Генерация отчёта по всем задачам проекта
        private void Generate_TasksByProject()
        {
            if (!(ProjectFilter.SelectedItem is Projects selectedProject))
            {
                MessageBox.Show("Выберите проект.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = db.Tasks
                .Where(t => t.ProjectID == selectedProject.ProjectID)
                .Select(t => new
                {
                    Номер = t.TaskID,
                    Название = t.TaskName,
                    Описание = t.TaskDescription,
                    Статус = t.Statuses.StatusName,
                    Исполнитель = t.Users != null ? t.Users.Login : "",
                    Приоритет = t.Priority
                })
                .ToList();

            ReportGrid.ItemsSource = data;

            if (data.Count == 0)
            {
                MessageBox.Show("В выбранном проекте нет задач.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// Генерация отчёта по этапам проекта
        private void Generate_StagesByProject()
        {
            if (!(ProjectFilter.SelectedItem is Projects selectedProject))
            {
                MessageBox.Show("Выберите проект.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rawData = db.ProjectStages
                .Where(s => s.ProjectID == selectedProject.ProjectID)
                .OrderBy(s => s.StageNumber)
                .Select(s => new
                {
                    s.StageNumber,
                    s.StageName,
                    s.StageDescription,
                    s.PlannedStartDate,
                    s.PlannedEndDate,
                    StatusName = s.Statuses.StatusName
                })
                .ToList();

            var data = rawData.Select(s => new
            {
                Номер = s.StageNumber,
                Название = s.StageName,
                Описание = s.StageDescription,
                ПланируемоеНачало = s.PlannedStartDate.HasValue
                    ? s.PlannedStartDate.Value.ToString("dd.MM.yyyy")
                    : "",
                ПланируемыйКонец = s.PlannedEndDate.HasValue
                    ? s.PlannedEndDate.Value.ToString("dd.MM.yyyy")
                    : "",
                Статус = s.StatusName
            }).ToList();

            ReportGrid.ItemsSource = data;

            if (data.Count == 0)
            {
                MessageBox.Show("В выбранном проекте нет этапов.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// Генерация отчёта по загрузке пользователей
        private void Generate_UserLoad()
        {
            if (CurrentUser.IsAdmin() || CurrentUser.IsProjectManager())
            {
                var userData = db.Tasks
                    .Where(t => t.Users != null)
                    .GroupBy(t => t.Users.Login)
                    .Select(g => new
                    {
                        Пользователь = g.Key,
                        КоличествоЗадач = g.Count(),
                        Выполненных = g.Count(t => t.Statuses.StatusName.ToLower().Contains("выполн")),
                        ВПроцессе = g.Count(t => t.Statuses.StatusName.ToLower().Contains("процессе")),
                        НеНачатоЗадач = g.Count(t => t.Statuses.StatusName.ToLower().Contains("не нач"))
                    })
                    .ToList();

                ReportGrid.ItemsSource = userData;
            }
            else
            {
                var generalData = new[]
                {
                    new
                    {
                        ВсегоЗадачВСистеме = db.Tasks.Count(),
                        ВыполненныхЗадач = db.Tasks.Count(t => t.Statuses.StatusName.ToLower().Contains("выполн")),
                        ЗадачВПроцессе = db.Tasks.Count(t => t.Statuses.StatusName.ToLower().Contains("процессе")),
                        НеНачатыхЗадач = db.Tasks.Count(t => t.Statuses.StatusName.ToLower().Contains("не нач"))
                    }
                };

                ReportGrid.ItemsSource = generalData;
            }
        }

        /// Метод для сохранения в Excel
        private void SaveAsExcel_Click(object sender, RoutedEventArgs e)
        {
            if (ReportGrid.ItemsSource == null)
            {
                MessageBox.Show("Сначала сгенерируйте отчёт.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel файл (*.xlsx)|*.xlsx",
                FileName = $"Отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            string filePath = saveDialog.FileName;

            try
            {
                var items = ReportGrid.ItemsSource as IEnumerable;
                var firstItem = items.Cast<object>().FirstOrDefault();

                if (firstItem == null)
                {
                    MessageBox.Show("Нет данных для экспорта.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Отчёт");

                    worksheet.Cells[1, 1].Value = $"Отчёт: {ReportTypeCombo.SelectedItem}";
                    worksheet.Cells[1, 1].Style.Font.Size = 16;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1, 1, 10].Merge = true;
                    worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Row(1).Height = 30;

                    worksheet.Cells[2, 1].Value = $"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    worksheet.Cells[2, 1, 2, 10].Merge = true;
                    worksheet.Cells[2, 1].Style.Font.Italic = true;
                    worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Row(2).Height = 20;

                    worksheet.Cells[3, 1].Value = $"Сгенерировал: {CurrentUser.Login} ({CurrentUser.Role})";
                    worksheet.Cells[3, 1, 3, 10].Merge = true;
                    worksheet.Cells[3, 1].Style.Font.Italic = true;
                    worksheet.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Row(3).Height = 20;

                    worksheet.Cells[5, 1].Value = "";
                    worksheet.Row(5).Height = 10;

                    var properties = firstItem.GetType().GetProperties();
                    int startRow = 6;
                    int col = 1;

                    foreach (var property in properties)
                    {
                        worksheet.Cells[startRow, col].Value = property.Name;
                        worksheet.Cells[startRow, col].Style.Font.Bold = true;
                        worksheet.Cells[startRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[startRow, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        worksheet.Cells[startRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[startRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[startRow, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        col++;
                    }
                    worksheet.Row(startRow).Height = 30;

                    var rowHeights = new List<double>();

                    int row = startRow + 1;
                    int itemCount = 0;

                    foreach (var item in items)
                    {
                        double maxLineHeight = 0;
                        col = 1;

                        foreach (var property in properties)
                        {
                            object value = property.GetValue(item);
                            var cell = worksheet.Cells[row, col];
                            cell.Value = value;

                            if (value is DateTime)
                            {
                                cell.Style.Numberformat.Format = "dd.MM.yyyy";
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }
                            else if (value is int || value is decimal || value is double || value is float)
                            {
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            }
                            else if (value is string strValue)
                            {
                                cell.Style.WrapText = true;
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                                if (!string.IsNullOrEmpty(strValue))
                                {
                                    int lineCount = (strValue.Length / 50) + 2;
                                    double estimatedHeight = Math.Max(20, lineCount * 15);
                                    maxLineHeight = Math.Max(maxLineHeight, estimatedHeight);
                                }
                            }
                            else
                            {
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }

                            if (row % 2 == 0)
                            {
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                            }

                            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            col++;
                        }

                        rowHeights.Add(Math.Max(20, maxLineHeight));
                        row++;
                        itemCount++;
                    }

                    for (int i = 1; i <= properties.Length; i++)
                    {
                        worksheet.Column(i).AutoFit();
                        if (worksheet.Column(i).Width < 15)
                            worksheet.Column(i).Width = 15;
                        if (worksheet.Column(i).Width > 50)
                            worksheet.Column(i).Width = 100;
                    }

                    for (int i = 0; i < itemCount; i++)
                    {
                        worksheet.Row(startRow + 1 + i).Height = rowHeights[i];
                    }

                    if (row > startRow + 1)
                    {
                        worksheet.Cells[startRow, 1, row - 1, properties.Length].AutoFilter = true;
                    }

                    package.SaveAs(new System.IO.FileInfo(filePath));
                }

                MessageBox.Show($"Отчёт успешно сохранён в Excel!\nФайл: {filePath}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении Excel файла: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Сохранение отчёта в формате DOCX
        private void SaveAsDocx_Click(object sender, RoutedEventArgs e)
        {
            if (ReportGrid.ItemsSource == null)
            {
                MessageBox.Show("Сначала сгенерируйте отчёт.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Word документ (*.docx)|*.docx",
                FileName = $"Отчёт_{DateTime.Now:yyyyMMdd_HHmmss}.docx",
                DefaultExt = ".docx"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            string filePath = saveDialog.FileName;

            try
            {
                var items = ReportGrid.ItemsSource as IEnumerable;

                using (var document = DocX.Create(filePath))
                {
                    document.InsertParagraph($"Отчёт: {ReportTypeCombo.SelectedItem}")
                        .FontSize(20)
                        .Bold()
                        .Alignment = Alignment.center;

                    document.InsertParagraph($"Дата генерации: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(12)
                        .Italic()
                        .Alignment = Alignment.center;

                    document.InsertParagraph($"Сгенерировал: {CurrentUser.Login} ({CurrentUser.Role})")
                        .FontSize(12)
                        .Italic()
                        .Alignment = Alignment.center;

                    document.InsertParagraph("");

                    var firstItem = items.Cast<object>().FirstOrDefault();
                    if (firstItem == null)
                    {
                        document.InsertParagraph("Нет данных для отображения.")
                            .FontSize(14)
                            .Alignment = Alignment.center;
                        document.Save();
                        MessageBox.Show("Документ сохранён.",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var properties = firstItem.GetType().GetProperties();
                    var table = document.AddTable(1, properties.Length);

                    for (int i = 0; i < properties.Length; i++)
                    {
                        table.Rows[0].Cells[i].Paragraphs[0]
                            .Append(properties[i].Name)
                            .Bold()
                            .Alignment = Alignment.center;
                    }

                    foreach (var item in items)
                    {
                        var row = table.InsertRow();
                        for (int i = 0; i < properties.Length; i++)
                        {
                            object value = properties[i].GetValue(item);
                            row.Cells[i].Paragraphs[0]
                                .Append(value?.ToString() ?? "")
                                .Alignment = Alignment.center;
                        }
                    }

                    table.AutoFit = AutoFit.Window;
                    table.Design = TableDesign.LightGrid;
                    document.InsertTable(table);
                    document.Save();
                }

                MessageBox.Show($"Отчёт успешно сохранён!\nФайл: {filePath}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении документа: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}