using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace AKurganov_2
{
    public partial class ApartmentsControlWindow : Window
    {
        private dynamic _selectedApartment;

        public ApartmentsControlWindow()
        {
            InitializeComponent();
            LoadApartments();
            icApartments.MouseDown += IcApartments_MouseDown;
        }

        private void LoadApartments()
        {
            try
            {
                var context = Entities.GetContext();

                var apartments = context.Apartments
                    .Join(context.Types,
                          a => a.TypeID,
                          t => t.ID,
                          (a, t) => new { Apartment = a, Type = t })
                    .Join(context.Conditions,
                          at => at.Apartment.ConditionID,
                          c => c.ID,
                          (at, c) => new
                          {
                              ID = at.Apartment.ID,
                              TypeID = at.Type.ID,
                              ConditionID = c.ID,
                              TypeName = at.Type.TypeName,
                              ConditionName = c.ConditionName
                          })
                    .OrderBy(a => a.ID)
                    .ToList();

                icApartments.ItemsSource = apartments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IcApartments_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) return;

            var border = FindParent<Border>(source);
            if (border != null && border.DataContext != null)
            {
                ClearAllSelections();

                border.Background = new SolidColorBrush(Colors.LightBlue);
                _selectedApartment = border.DataContext;
                btnDelete.IsEnabled = true;
            }
        }

        private void ClearAllSelections()
        {
            var container = icApartments.ItemContainerGenerator;
            for (int i = 0; i < icApartments.Items.Count; i++)
            {
                var containerFromIndex = icApartments.ItemContainerGenerator.ContainerFromIndex(i);
                if (containerFromIndex != null)
                {
                    var border = FindVisualChild<Border>(containerFromIndex);
                    if (border != null)
                    {
                        border.Background = Brushes.White;
                    }
                }
            }

            _selectedApartment = null;
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

        private void icApartments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) return;

            var border = FindParent<Border>(source);
            if (border != null && border.DataContext != null)
            {
                dynamic selectedApartment = border.DataContext;
                OpenEditorWindow(selectedApartment.ID);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenEditorWindow();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedApartment == null)
            {
                MessageBox.Show("Выберите комнату для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить комнату ID: {_selectedApartment.ID}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteSelectedApartment();
            }
        }

        private void DeleteSelectedApartment()
        {
            try
            {
                var context = Entities.GetContext();

                var apartmentToDelete = context.Apartments.Find(_selectedApartment.ID);
                if (apartmentToDelete != null)
                {
                    context.Apartments.Remove(apartmentToDelete);
                    context.SaveChanges();

                    MessageBox.Show("Комната успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadApartments();
                    ClearAllSelections();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEditorWindow(int apartmentId = 0)
        {
            try
            {
                var editorWindow = new ApartmentsRedactorWindow(apartmentId);
                editorWindow.Owner = this;
                editorWindow.Closed += (s, args) =>
                {
                    LoadApartments();
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