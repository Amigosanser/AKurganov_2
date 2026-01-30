using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;

namespace AKurganov_2
{
    public partial class RevPARWindow : Window
    {
        public RevPARWindow()
        {
            InitializeComponent();

            dpStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            dpEndDate.SelectedDate = DateTime.Now;

            CalculateRevPAR();
        }

        private void CalculateRevPAR()
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

                var totalRooms = context.Apartments.Count();

                var rents = context.PaymentApartments
                    .Where(r => r.Date >= startDate && r.Date <= endDate)
                    .ToList();

                var dailyData = new List<RevPARData>();

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayRents = rents.Where(r => r.Date.Value.Date == date).ToList();

                    decimal dailyRevenue = dayRents.Sum(r => (decimal)r.Payment);
                    int occupiedRooms = dayRents.Count;

                    decimal occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;
                    decimal adr = occupiedRooms > 0 ? dailyRevenue / occupiedRooms : 0;
                    decimal revpar = totalRooms > 0 ? dailyRevenue / totalRooms : 0;

                    dailyData.Add(new RevPARData
                    {
                        Date = date,
                        DailyRevenue = dailyRevenue,
                        OccupiedRooms = occupiedRooms,
                        TotalRooms = totalRooms,
                        OccupancyRate = Math.Round(occupancyRate, 2),
                        ADR = Math.Round(adr, 2),
                        RevPAR = Math.Round(revpar, 2)
                    });
                }

                var totalRevenue = dailyData.Sum(d => d.DailyRevenue);
                var totalOccupied = dailyData.Sum(d => d.OccupiedRooms);
                var totalDays = (endDate - startDate).Days + 1;

                decimal avgOccupancyRate = totalDays > 0 && totalRooms > 0 ?
                    (decimal)totalOccupied / (totalDays * totalRooms) * 100 : 0;

                decimal avgADR = totalOccupied > 0 ? totalRevenue / totalOccupied : 0;
                decimal avgRevPAR = totalDays > 0 && totalRooms > 0 ?
                    totalRevenue / (totalDays * totalRooms) : 0;

                var summaryData = new RevPARData
                {
                    Date = null,
                    DailyRevenue = totalRevenue,
                    OccupiedRooms = totalOccupied,
                    TotalRooms = totalRooms * totalDays,
                    OccupancyRate = Math.Round(avgOccupancyRate, 2),
                    ADR = Math.Round(avgADR, 2),
                    RevPAR = Math.Round(avgRevPAR, 2)
                };

                var displayData = dailyData.Cast<object>().ToList();
                displayData.Add(summaryData);

                dgRevPARData.ItemsSource = displayData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета RevPAR: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateRevPAR();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgRevPARData.Items.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
                saveDialog.FileName = $"RevPAR_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Дата;Выручка за день;Занято комнат;Всего комнат;Загрузка %;ADR;RevPAR");

                        foreach (var item in dgRevPARData.Items)
                        {
                            if (item is RevPARData data)
                            {
                                string dateStr = data.Date.HasValue ? data.Date.Value.ToString("dd.MM.yyyy") : "Итого";
                                writer.WriteLine($"{dateStr};{data.DailyRevenue};{data.OccupiedRooms};{data.TotalRooms};{data.OccupancyRate:F2}%;{data.ADR:F2};{data.RevPAR:F2}");
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

    public class RevPARData
    {
        public DateTime? Date { get; set; }
        public decimal DailyRevenue { get; set; }
        public int OccupiedRooms { get; set; }
        public int TotalRooms { get; set; }
        public decimal OccupancyRate { get; set; }
        public decimal ADR { get; set; }
        public decimal RevPAR { get; set; }
    }
}