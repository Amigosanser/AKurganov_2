using System;
using System.Linq;
using System.Windows;

namespace AKurganov_2
{
    public partial class ApartmentsRedactorWindow : Window
    {
        private int _apartmentId;
        private bool _isEditMode;

        public ApartmentsRedactorWindow(int apartmentId = 0)
        {
            InitializeComponent();
            _apartmentId = apartmentId;
            _isEditMode = apartmentId > 0;

            LoadComboBoxData();
            LoadApartmentData();
        }

        private void LoadComboBoxData()
        {
            try
            {
                var context = Entities.GetContext();

                cbType.ItemsSource = context.Types.ToList();
                cbCondition.ItemsSource = context.Conditions.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadApartmentData()
        {
            try
            {
                if (_isEditMode)
                {
                    var context = Entities.GetContext();
                    var apartment = context.Apartments.Find(_apartmentId);

                    if (apartment != null)
                    {
                        cbType.SelectedItem = context.Types
                            .FirstOrDefault(t => t.ID == apartment.TypeID);

                        cbCondition.SelectedItem = context.Conditions
                            .FirstOrDefault(c => c.ID == apartment.ConditionID);
                    }
                }
                else
                {
                    if (cbType.Items.Count > 0)
                        cbType.SelectedIndex = 0;

                    if (cbCondition.Items.Count > 0)
                        cbCondition.SelectedIndex = 0;
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

                var existingIds = context.Apartments
                    .Select(a => a.ID)
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
                    var apartment = context.Apartments.Find(_apartmentId);
                    if (apartment != null)
                    {
                        var selectedType = cbType.SelectedItem as Types;
                        var selectedCondition = cbCondition.SelectedItem as Conditions;

                        apartment.TypeID = selectedType.ID;
                        apartment.ConditionID = selectedCondition.ID;
                    }
                }
                else
                {
                    var newApartment = new Apartments();

                    var selectedType = cbType.SelectedItem as Types;
                    var selectedCondition = cbCondition.SelectedItem as Conditions;

                    newApartment.ID = GetNextAvailableId();
                    newApartment.TypeID = selectedType.ID;
                    newApartment.ConditionID = selectedCondition.ID;

                    context.Apartments.Add(newApartment);
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
            if (cbType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип комнаты", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbType.Focus();
                return false;
            }

            if (cbCondition.SelectedItem == null)
            {
                MessageBox.Show("Выберите состояние комнаты", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbCondition.Focus();
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
                var result = MessageBox.Show(
                    "Вы уверены, что хотите закрыть окно без сохранения?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

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