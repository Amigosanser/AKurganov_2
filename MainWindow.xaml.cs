using System;
using System.Windows;

namespace AKurganov_2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnApartments_Click(object sender, RoutedEventArgs e)
        {
            ApartmentsControlWindow apartmentsWindow = new ApartmentsControlWindow();
            apartmentsWindow.Closed += (s, args) => this.Show();
            apartmentsWindow.Show();
            this.Hide();
        }

        private void btnVisitors_Click(object sender, RoutedEventArgs e)
        {
            VisitorsControlWindow visitorsWindow = new VisitorsControlWindow();
            visitorsWindow.Closed += (s, args) => this.Show();
            visitorsWindow.Show();
            this.Hide();
        }

        private void btnRents_Click(object sender, RoutedEventArgs e)
        {
            RentsControlWindow rentsWindow = new RentsControlWindow();
            rentsWindow.Closed += (s, args) => this.Show();
            rentsWindow.Show();
            this.Hide();
        }

        private void btnADR_Click(object sender, RoutedEventArgs e)
        {
            ADRWindow adrWindow = new ADRWindow();
            adrWindow.Owner = this;
            adrWindow.Closed += (s, args) => this.Show();
            adrWindow.Show();
            this.Hide();
        }

        private void btnRevPAR_Click(object sender, RoutedEventArgs e)
        {
            RevPARWindow revparWindow = new RevPARWindow();
            revparWindow.Owner = this;
            revparWindow.Closed += (s, args) => this.Show();
            revparWindow.Show();
            this.Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите закрыть приложение?", "Подтверждение закрытия", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (ActualWidth < MinWidth || ActualHeight < MinHeight)
            {
                Width = Math.Max(Width, MinWidth);
                Height = Math.Max(Height, MinHeight);
            }
        }
    }
}