using System;
using System.Linq;
using System.Windows;

namespace AKurganov_2
{
    public partial class VisitorsRedactorWindow : Window
    {
        private int _visitorId;
        private bool _isEditMode;

        public VisitorsRedactorWindow(int visitorId = 0)
        {
            InitializeComponent();
            _visitorId = visitorId;
            _isEditMode = visitorId > 0;

            LoadVisitorData();
        }

        private void LoadVisitorData()
        {
            try
            {
                if (_isEditMode)
                {
                    var context = Entities.GetContext();
                    var visitor = context.Visitors.Find(_visitorId);

                    if (visitor != null)
                    {
                        txtFIO.Text = visitor.FIO;
                    }
                }
                else
                {
                    txtFIO.Text = "";
                    txtFIO.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetNextAvailableId()
        {
            try
            {
                var context = Entities.GetContext();

                var existingIds = context.Visitors
                    .Select(v => v.ID)
                    .OrderBy(id => id)
                    .ToList();

                int expectedId = 1;
                foreach (int existingId in existingIds)
                {
                    if (existingId > expectedId)
                    {
                        return expectedId;
                    }
                    expectedId = existingId + 1;
                }

                return expectedId;
            }
            catch
            {
                return 1;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData())
                return;

            try
            {
                var context = Entities.GetContext();

                if (_isEditMode)
                {
                    var visitor = context.Visitors.Find(_visitorId);
                    if (visitor != null)
                    {
                        visitor.FIO = txtFIO.Text.Trim();
                    }
                }
                else
                {
                    var newVisitor = new Visitors();

                    newVisitor.ID = GetNextAvailableId();
                    newVisitor.FIO = txtFIO.Text.Trim();

                    context.Visitors.Add(newVisitor);
                }

                context.SaveChanges();

                MessageBox.Show("Данные успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.InnerException?.Message ?? ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateData()
        {
            if (string.IsNullOrWhiteSpace(txtFIO.Text))
            {
                MessageBox.Show("Введите ФИО посетителя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFIO.Focus();
                return false;
            }

            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DialogResult == null)
            {
                var result = MessageBox.Show("Вы уверены, что хотите закрыть окно без сохранения?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    this.DialogResult = false;
                }
            }
        }
    }
}