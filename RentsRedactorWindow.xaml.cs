using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AKurganov_2
{
    public partial class RentsRedactorWindow : Window
    {
        private int _rentId;
        private bool _isEditMode;
        private int _selectedApartmentTypeCost = 0;
        private int _currentApartmentId = 0;

        public RentsRedactorWindow(int rentId = 0)
        {
            InitializeComponent();
            _rentId = rentId;
            _isEditMode = rentId > 0;

            LoadComboBoxData();
            LoadRentData();
            CalculateCost();
        }

        private void LoadComboBoxData()
        {
            try
            {
                var context = Entities.GetContext();

                var apartments = (from a in context.Apartments
                                  join t in context.Types on a.TypeID equals t.ID
                                  join c in context.Conditions on a.ConditionID equals c.ID
                                  where c.ConditionName == "Чистый"
                                  select new
                                  {
                                      ApartmentID = a.ID,
                                      TypeCost = t.Cost,
                                      TypeName = t.TypeName,
                                      RoomInfo = "ID: " + a.ID + ", Тип: " + t.TypeName
                                  }).ToList();

                cbApartment.ItemsSource = apartments;

                cbVisitor.ItemsSource = context.Visitors.ToList();

                var staff = (from s in context.Staffs
                             join r in context.Roles on s.RoleID equals r.ID
                             where r.RoleName == "Администратор"
                             select new
                             {
                                 StaffID = s.ID,
                                 StaffFIO = s.FIO
                             }).ToList();

                cbStaff.ItemsSource = staff;
                cbStaff.DisplayMemberPath = "StaffFIO";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRentData()
        {
            try
            {
                if (_isEditMode)
                {
                    var context = Entities.GetContext();
                    var rent = context.PaymentApartments.Find(_rentId);

                    if (rent != null)
                    {
                        _currentApartmentId = rent.ApartmentID;

                        var apartment = (from a in context.Apartments
                                         join t in context.Types on a.TypeID equals t.ID
                                         join c in context.Conditions on a.ConditionID equals c.ID
                                         where a.ID == rent.ApartmentID
                                         select new
                                         {
                                             ApartmentID = a.ID,
                                             TypeCost = t.Cost,
                                             TypeName = t.TypeName,
                                             RoomInfo = "ID: " + a.ID + ", Тип: " + t.TypeName
                                         }).FirstOrDefault();

                        if (apartment != null)
                        {
                            cbApartment.SelectedItem = apartment;
                            _selectedApartmentTypeCost = apartment.TypeCost;
                        }

                        cbVisitor.SelectedItem = context.Visitors.Find(rent.VisitorID);

                        var staff = (from s in context.Staffs
                                     join r in context.Roles on s.RoleID equals r.ID
                                     where s.ID == rent.StaffID && r.RoleName == "Администратор"
                                     select new
                                     {
                                         StaffID = s.ID,
                                         StaffFIO = s.FIO
                                     }).FirstOrDefault();

                        cbStaff.SelectedItem = staff;

                        txtDays.Text = rent.QuantityDays.ToString();
                    }
                }
                else
                {
                    if (cbApartment.Items.Count > 0)
                        cbApartment.SelectedIndex = 0;

                    if (cbVisitor.Items.Count > 0)
                        cbVisitor.SelectedIndex = 0;

                    if (cbStaff.Items.Count > 0)
                        cbStaff.SelectedIndex = 0;

                    txtDays.Text = "1";
                    txtDays.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtDays_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private int GetNextAvailableId()
        {
            try
            {
                var context = Entities.GetContext();

                var existingIds = context.PaymentApartments
                    .Select(r => r.ID)
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

        private void CalculateCost()
        {
            if (txtCost != null && int.TryParse(txtDays.Text, out int days) && days > 0)
            {
                int cost = days * _selectedApartmentTypeCost;
                txtCost.Text = cost.ToString();
            }
            else if (txtCost != null)
            {
                txtCost.Text = "0";
            }
        }

        private void cbApartment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbApartment.SelectedItem != null)
            {
                dynamic apartment = cbApartment.SelectedItem;
                _selectedApartmentTypeCost = apartment.TypeCost;
                CalculateCost();
            }
        }

        private void txtDays_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateCost();
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
                    var rent = context.PaymentApartments.Find(_rentId);
                    if (rent != null)
                    {
                        dynamic apartment = cbApartment.SelectedItem;
                        dynamic visitor = cbVisitor.SelectedItem;
                        dynamic staff = cbStaff.SelectedItem;
                        int days = int.Parse(txtDays.Text);
                        int payment = days * _selectedApartmentTypeCost;

                        rent.ApartmentID = apartment.ApartmentID;
                        rent.VisitorID = visitor.ID;
                        rent.StaffID = staff.StaffID;
                        rent.QuantityDays = days;
                        rent.Payment = payment;
                        rent.Date = DateTime.Now;

                        if (_currentApartmentId != apartment.ApartmentID)
                        {
                            UpdateApartmentStatus(_currentApartmentId, "Грязный");
                            UpdateApartmentStatus(apartment.ApartmentID, "Занят");
                        }
                    }
                }
                else
                {
                    var newRent = new PaymentApartments();

                    dynamic apartment = cbApartment.SelectedItem;
                    dynamic visitor = cbVisitor.SelectedItem;
                    dynamic staff = cbStaff.SelectedItem;
                    int days = int.Parse(txtDays.Text);
                    int payment = days * _selectedApartmentTypeCost;

                    newRent.ID = GetNextAvailableId();
                    newRent.ApartmentID = apartment.ApartmentID;
                    newRent.VisitorID = visitor.ID;
                    newRent.StaffID = staff.StaffID;
                    newRent.QuantityDays = days;
                    newRent.Payment = payment;
                    newRent.Date = DateTime.Now;

                    context.PaymentApartments.Add(newRent);

                    UpdateApartmentStatus(apartment.ApartmentID, "Занят");
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

        private void UpdateApartmentStatus(int apartmentId, string status)
        {
            try
            {
                var context = Entities.GetContext();
                var apartment = context.Apartments.Find(apartmentId);
                if (apartment != null)
                {
                    var condition = context.Conditions.FirstOrDefault(c => c.ConditionName == status);
                    if (condition != null)
                    {
                        apartment.ConditionID = condition.ID;
                        context.SaveChanges();
                    }
                }
            }
            catch
            {
            }
        }

        private bool ValidateData()
        {
            if (cbApartment.SelectedItem == null)
            {
                MessageBox.Show("Выберите комнату", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbApartment.Focus();
                return false;
            }

            if (cbVisitor.SelectedItem == null)
            {
                MessageBox.Show("Выберите посетителя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbVisitor.Focus();
                return false;
            }

            if (cbStaff.SelectedItem == null)
            {
                MessageBox.Show("Выберите сотрудника", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbStaff.Focus();
                return false;
            }

            if (!int.TryParse(txtDays.Text, out int days) || days <= 0)
            {
                MessageBox.Show("Введите корректное количество дней", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDays.Focus();
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