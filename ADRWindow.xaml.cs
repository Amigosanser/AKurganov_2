using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;

namespace AKurganov_2
{
    public partial class ADRWindow : Window
    {
        public ADRWindow()
        {
            InitializeComponent();

            dpStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            dpEndDate.SelectedDate = DateTime.Now;

            CalculateADR();
        }

        private void CalculateADR()
        {
            try
            {
                if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
                {
                    MessageBox.Show("Выберите даты", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = dpStartDate.SelectedDate.Value.Date;
                DateTime endDate = dpEndDate.SelectedDate.Value.Date;

                if (startDate > endDate)
                {
                    MessageBox.Show("Начальная дата не может быть больше конечной", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var context = Entities.GetContext();

                var rents = context.PaymentApartments
                    .Where(r => r.Date >= startDate && r.Date <= endDate)
                    .ToList();

                var dailyData = new List<ADRData>();

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayRents = rents.Where(r => r.Date.Value.Date == date).ToList();

                    decimal dailyRevenue = dayRents.Sum(r => (decimal)r.Payment);
                    int rentCount = dayRents.Count;
                    decimal adr = rentCount > 0 ? dailyRevenue / rentCount : 0;

                    dailyData.Add(new ADRData
                    {
                        Date = date,
                        DailyRevenue = dailyRevenue,
                        RentCount = rentCount,
                        ADR = Math.Round(adr, 2)
                    });
                }

                var totalRevenue = dailyData.Sum(d => d.DailyRevenue);
                var totalRents = dailyData.Sum(d => d.RentCount);
                var averageADR = totalRents > 0 ? Math.Round(totalRevenue / totalRents, 2) : 0;

                var summaryData = new ADRData
                {
                    Date = null,
                    DailyRevenue = totalRevenue,
                    RentCount = totalRents,
                    ADR = averageADR
                };

                var displayData = dailyData.Cast<object>().ToList();
                displayData.Add(summaryData);

                dgADRData.ItemsSource = displayData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета ADR: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateADR();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgADRData.Items.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                saveDialog.FileName = $"ADR_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Дата;Выручка за день;Количество аренд;ADR");

                        foreach (var item in dgADRData.Items)
                        {
                            if (item is ADRData data)
                            {
                                string dateStr = data.Date.HasValue ? data.Date.Value.ToString("dd.MM.yyyy") : "Итого";
                                writer.WriteLine($"{dateStr};{data.DailyRevenue};{data.RentCount};{data.ADR}");
                            }
                        }
                    }

                    MessageBox.Show("Данные успешно экспортированы", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Owner != null)
            {
                Owner.Show();
            }
        }
    }

    public class ADRData
    {
        public DateTime? Date { get; set; }
        public decimal DailyRevenue { get; set; }
        public int RentCount { get; set; }
        public decimal ADR { get; set; }
    }
}