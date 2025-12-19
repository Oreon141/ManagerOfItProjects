using ManagerOfItProjects.DataBase;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManagerOfItProjects.Pages
{

    public partial class AddEditStagePage : Page
    {
        private ITProjectsManagerEntities db = ITProjectsManagerEntities.GetContext();
        private ProjectStages stage;
        private int projectId;

        private bool isEdit = false;

        public AddEditStagePage(int projectId)
        {
            InitializeComponent();
            this.projectId = projectId;
            isEdit = false;
        }


        public AddEditStagePage(ProjectStages stage)
        {
            InitializeComponent();
            this.stage = stage;
            projectId = stage.ProjectID;
            isEdit = true;
        }

        /// Обработчик загрузки страницы
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StatusBox.ItemsSource = db.Statuses.ToList();

            if (isEdit)
            {
                HeaderText.Text = "Редактирование этапа";

                StageNumberBox.Text = stage.StageNumber.ToString();
                StageNameBox.Text = stage.StageName;
                StageDescriptionBox.Text = stage.StageDescription;

                PlannedStartBox.SelectedDate = stage.PlannedStartDate;
                PlannedEndBox.SelectedDate = stage.PlannedEndDate;

                StatusBox.SelectedItem = db.Statuses.FirstOrDefault(s => s.StatusID == stage.StatusID);

                DeleteButton.Visibility = Visibility.Visible;
            }
            else
            {
                HeaderText.Text = "Создание нового этапа";
            }
        }
        /// Обработчик сохранения этапа
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEdit)
                stage = new ProjectStages { ProjectID = projectId };
            if (!int.TryParse(StageNumberBox.Text, out int num))
            {
                MessageBox.Show("Номер этапа должен быть числом!");
                return;
            }

            if (string.IsNullOrWhiteSpace(StageNameBox.Text))
            {
                MessageBox.Show("Название этапа не может быть пустым!");
                return;
            }

            if (PlannedStartBox.SelectedDate == null)
            {
                MessageBox.Show("Укажите планируемую дату начала!");
                return;
            }

            if (PlannedEndBox.SelectedDate == null)
            {
                MessageBox.Show("Укажите планируемую дату окончания!");
                return;
            }

            if (PlannedEndBox.SelectedDate < PlannedStartBox.SelectedDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала!");
                return;
            }

            stage.StageNumber = num;
            stage.StageName = StageNameBox.Text;
            stage.StageDescription = StageDescriptionBox.Text;
            stage.PlannedStartDate = PlannedStartBox.SelectedDate;
            stage.PlannedEndDate = PlannedEndBox.SelectedDate;

            if (StatusBox.SelectedItem is Statuses s)
                stage.StatusID = s.StatusID;
            else
            {
                MessageBox.Show("Выберите статус!");
                return;
            }

            try
            {
                if (!isEdit)
                    db.ProjectStages.Add(stage);

                db.SaveChanges();
                MessageBox.Show("Этап сохранён!");

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        /// Обработчик удаления этапа
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEdit) return;

            if (MessageBox.Show("Удалить этап?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    db.ProjectStages.Remove(stage);
                    db.SaveChanges();

                    MessageBox.Show("Этап удалён!");
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}