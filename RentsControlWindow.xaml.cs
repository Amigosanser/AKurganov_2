using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace AKurganov_2
{
    public partial class RentsControlWindow : Window
    {
        private dynamic _selectedRent;
        private List<dynamic> _allRents = new List<dynamic>();
        private List<dynamic> _filteredRents = new List<dynamic>();

        public RentsControlWindow()
        {
            InitializeComponent();
            LoadRents();
            LoadFilterData();
            icRents.MouseDown += IcRents_MouseDown;
        }

        private void LoadRents()
        {
            try
            {
                var context = Entities.GetContext();

                _allRents = (from r in context.PaymentApartments
                             join v in context.Visitors on r.VisitorID equals v.ID
                             join s in context.Staffs on r.StaffID equals s.ID
                             orderby r.ID
                             select new
                             {
                                 r.ID,
                                 r.ApartmentID,
                                 r.VisitorID,
                                 r.QuantityDays,
                                 r.Payment,
                                 r.StaffID,
                                 r.Date,
                                 VisitorFIO = v.FIO,
                                 StaffFIO = s.FIO
                             }).ToList<dynamic>();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFilterData()
        {
            try
            {
                var context = Entities.GetContext();

                var visitors = context.Visitors
                    .OrderBy(v => v.FIO)
                    .Select(v => new { v.ID, v.FIO })
                    .ToList();

                foreach (var visitor in visitors)
                {
                    cmbVisitorFilter.Items.Add(new ComboBoxItem
                    {
                        Content = visitor.FIO,
                        Tag = visitor.ID
                    });
                }

                var staff = context.Staffs
                    .OrderBy(s => s.FIO)
                    .Select(s => new { s.ID, s.FIO })
                    .ToList();

                foreach (var staffMember in staff)
                {
                    cmbStaffFilter.Items.Add(new ComboBoxItem
                    {
                        Content = staffMember.FIO,
                        Tag = staffMember.ID
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            _filteredRents = _allRents.ToList();

            string searchText = txtSearch.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                _filteredRents = _filteredRents.Where(r =>
                    (r.VisitorFIO?.ToString()?.ToLower() ?? "").Contains(searchText) ||
                    (r.StaffFIO?.ToString()?.ToLower() ?? "").Contains(searchText) ||
                    (r.ID.ToString()?.ToLower() ?? "").Contains(searchText) ||
                    (r.ApartmentID.ToString()?.ToLower() ?? "").Contains(searchText) ||
                    (r.Payment.ToString()?.ToLower() ?? "").Contains(searchText)
                ).ToList();
            }

            if (cmbVisitorFilter.SelectedItem is ComboBoxItem visitorItem && visitorItem.Tag != null)
            {
                int visitorId = (int)visitorItem.Tag;
                _filteredRents = _filteredRents.Where(r => r.VisitorID == visitorId).ToList();
            }

            if (cmbStaffFilter.SelectedItem is ComboBoxItem staffItem && staffItem.Tag != null)
            {
                int staffId = (int)staffItem.Tag;
                _filteredRents = _filteredRents.Where(r => r.StaffID == staffId).ToList();
            }

            icRents.ItemsSource = _filteredRents;
            ClearAllSelections();
        }

        private void IcRents_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) return;

            var border = FindParent<Border>(source);
            if (border != null && border.DataContext != null)
            {
                ClearAllSelections();

                border.Background = new SolidColorBrush(Colors.LightBlue);
                _selectedRent = border.DataContext;
                btnDelete.IsEnabled = true;
            }
        }

        private void ClearAllSelections()
        {
            var container = icRents.ItemContainerGenerator;
            for (int i = 0; i < icRents.Items.Count; i++)
            {
                var containerFromIndex = icRents.ItemContainerGenerator.ContainerFromIndex(i);
                if (containerFromIndex != null)
                {
                    var border = FindVisualChild<Border>(containerFromIndex);
                    if (border != null)
                    {
                        border.Background = Brushes.White;
                    }
                }
            }

            _selectedRent = null;
            btnDelete.IsEnabled = false;
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }

        private void icRents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) return;

            var border = FindParent<Border>(source);
            if (border != null && border.DataContext != null)
            {
                dynamic selectedRent = border.DataContext;
                OpenEditorWindow(selectedRent.ID);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbVisitorFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbStaffFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbVisitorFilter.SelectedIndex = 0;
            cmbStaffFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenEditorWindow();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRent == null)
            {
                MessageBox.Show("Выберите аренду для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить аренду ID: {_selectedRent.ID}?\nПосетитель: {_selectedRent.VisitorFIO}\nСумма: {_selectedRent.Payment}", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteSelectedRent();
            }
        }

        private void DeleteSelectedRent()
        {
            try
            {
                var context = Entities.GetContext();

                var rentToDelete = context.PaymentApartments.Find(_selectedRent.ID);
                if (rentToDelete != null)
                {
                    context.PaymentApartments.Remove(rentToDelete);
                    context.SaveChanges();

                    MessageBox.Show("Аренда успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadRents();
                    ClearAllSelections();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEditorWindow(int rentId = 0)
        {
            try
            {
                var editorWindow = new RentsRedactorWindow(rentId);
                editorWindow.Owner = this;
                editorWindow.Closed += (s, args) =>
                {
                    LoadRents();
                    LoadFilterData();
                    ClearAllSelections();
                };

                editorWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия редактора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool _isClosingFromBackButton = false;

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            _isClosingFromBackButton = true;
            ReturnToMainWindow();
        }

        private void ReturnToMainWindow()
        {
            _isClosingFromBackButton = true;
            if (Owner is MainWindow mainWindow)
            {
                mainWindow.Show();
            }
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosingFromBackButton)
            {
                e.Cancel = false;
                return;
            }

            if (Owner != null)
            {
                Owner.Show();
                e.Cancel = true;
                this.Hide();
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите закрыть программу?", "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}