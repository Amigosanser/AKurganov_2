using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace AKurganov_2
{
    public partial class VisitorsControlWindow : Window
    {
        private dynamic _selectedVisitor;

        public VisitorsControlWindow()
        {
            InitializeComponent();
            LoadVisitors();
            icVisitors.MouseDown += IcVisitors_MouseDown;
        }

        private void LoadVisitors()
        {
            try
            {
                var context = Entities.GetContext();

                var visitors = context.Visitors
                    .OrderBy(v => v.ID)
                    .Select(v => new
                    {
                        v.ID,
                        v.FIO
                    })
                    .ToList();

                icVisitors.ItemsSource = visitors;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IcVisitors_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) return;

            var border = FindParent<Border>(source);
            if (border != null && border.DataContext != null)
            {
                ClearAllSelections();

                border.Background = new SolidColorBrush(Colors.LightBlue);
                _selectedVisitor = border.DataContext;
                btnDelete.IsEnabled = true;
            }
        }

        private void ClearAllSelections()
        {
            var container = icVisitors.ItemContainerGenerator;
            for (int i = 0; i < icVisitors.Items.Count; i++)
            {
                var containerFromIndex = icVisitors.ItemContainerGenerator.ContainerFromIndex(i);
                if (containerFromIndex != null)
                {
                    var border = FindVisualChild<Border>(containerFromIndex);
                    if (border != null)
                    {
                        border.Background = Brushes.White;
                    }
                }
            }

            _selectedVisitor = null;
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

        private void icVisitors_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) return;

            var border = FindParent<Border>(source);
            if (border != null && border.DataContext != null)
            {
                dynamic selectedVisitor = border.DataContext;
                OpenEditorWindow(selectedVisitor.ID);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenEditorWindow();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVisitor == null)
            {
                MessageBox.Show("Выберите посетителя для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить посетителя: {_selectedVisitor.FIO}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteSelectedVisitor();
            }
        }

        private void DeleteSelectedVisitor()
        {
            try
            {
                var context = Entities.GetContext();

                var visitorToDelete = context.Visitors.Find(_selectedVisitor.ID);
                if (visitorToDelete != null)
                {
                    context.Visitors.Remove(visitorToDelete);
                    context.SaveChanges();

                    MessageBox.Show("Посетитель успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadVisitors();
                    ClearAllSelections();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenEditorWindow(int visitorId = 0)
        {
            try
            {
                var editorWindow = new VisitorsRedactorWindow(visitorId);
                editorWindow.Owner = this;
                editorWindow.Closed += (s, args) =>
                {
                    LoadVisitors();
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