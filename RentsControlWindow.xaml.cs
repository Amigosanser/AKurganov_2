using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace AKurganov_2
{
    public partial class RentsControlWindow : Window
    {
        private dynamic _selectedRent;

        public RentsControlWindow()
        {
            InitializeComponent();
            LoadRents();
            icRents.MouseDown += IcRents_MouseDown;
        }

        private void LoadRents()
        {
            try
            {
                var context = Entities.GetContext();

                var rents = (from r in context.PaymentApartments
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
                             }).ToList();

                icRents.ItemsSource = rents;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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