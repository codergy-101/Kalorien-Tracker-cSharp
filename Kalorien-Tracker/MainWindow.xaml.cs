using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using LiveCharts;
using LiveCharts.Wpf;

namespace Kalorien_Tracker
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, double> weightData = new Dictionary<string, double>();
        private const string WEIGHT_FILE = "weight_data.json";
        private SeriesCollection weightChartSeries;
        private CalorieTracker tracker;
        private DateTime currentDate;
        private Dictionary<string, double> settings;

        public SeriesCollection MacroSeries { get; set; }
        public Func<ChartPoint, string> LabelFormatter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Initialize chart
            MacroSeries = new SeriesCollection();
            LabelFormatter = point => $"{point.Y:N1}g ({point.Participation:P1})";

            Loaded += LoadFoodData;
            LoadSettings();
            LoadFoodData(null, null);
            tracker = new CalorieTracker((int)settings["calorie_goal"], settings["protein_ratio"],
                settings["carbs_ratio"], settings["fat_ratio"]);
            currentDate = DateTime.Today;
            tracker.LoadFoodData("food_data.json");
            UpdateDailySummary();
            DisplaySettings();
            DateLabel.Content = currentDate.ToString("dd.MM.yyyy");

            DataContext = this;
        }

        private void UpdateWeightChart()
        {
            try
            {
                if (weightData == null || weightData.Count == 0)
                    return;

                // Sort the weight data by date
                var sortedData = weightData
                    .OrderBy(x => DateTime.ParseExact(x.Key, "yyyy-MM-dd", null))
                    .ToList();

                if (sortedData.Count == 0)
                    return;

                // Filter by selected time range
                DateTime cutoffDate = DateTime.Now;

                if (LastMonthRadio != null && LastMonthRadio.IsChecked == true)
                {
                    cutoffDate = DateTime.Now.AddMonths(-1);
                }
                else if (Last3MonthsRadio != null && Last3MonthsRadio.IsChecked == true)
                {
                    cutoffDate = DateTime.Now.AddMonths(-3);
                }
                else if (Last6MonthsRadio != null && Last6MonthsRadio.IsChecked == true)
                {
                    cutoffDate = DateTime.Now.AddMonths(-6);
                }
                else // Last year or default
                {
                    cutoffDate = DateTime.Now.AddYears(-1);
                }

                var filteredData = sortedData
                    .Where(x => DateTime.ParseExact(x.Key, "yyyy-MM-dd", null) >= cutoffDate)
                    .ToList();

                // Initialize chart properties
                weightChartSeries = new SeriesCollection();

                if (filteredData.Count == 0)
                {
                    WeightChart.Series = weightChartSeries;
                    return;
                }

                // Prepare data points
                var weightValues = new ChartValues<double>();
                var dateLabels = new List<string>();

                // Add data points to the chart
                foreach (var entry in filteredData)
                {
                    // Parse the date into a readable format for the axis
                    DateTime entryDate = DateTime.ParseExact(entry.Key, "yyyy-MM-dd", null);
                    dateLabels.Add(entryDate.ToString("dd.MM"));
                    weightValues.Add(entry.Value);
                }

                // Add line series with data points
                weightChartSeries.Add(new LineSeries
                {
                    Title = "Gewicht (kg)",
                    Values = weightValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    LineSmoothness = 0,
                    Stroke = System.Windows.Media.Brushes.DodgerBlue,
                    Fill = System.Windows.Media.Brushes.Transparent
                });

                // Update the chart
                WeightChart.Series = weightChartSeries;

                // Set X-axis labels
                WeightChart.AxisX[0].Labels = dateLabels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren des Gewichtsverlaufs: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void UpdateMacroPieChart(double protein, double carbs, double fat)
        {
            MacroSeries.Clear();

            // Define custom colors for each macro
            var fatColor = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red fatColor
            var proteinColor = new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Green proteinColor
            var carbsColor = new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue carbsColor

            MacroSeries.Add(new PieSeries
            {
                Title = "Protein",
                Values = new ChartValues<double> { protein },
                DataLabels = true,
                LabelPoint = LabelFormatter,
                Fill = proteinColor
            });

            MacroSeries.Add(new PieSeries
            {
                Title = "Kohlenhydrate",
                Values = new ChartValues<double> { carbs },
                DataLabels = true,
                LabelPoint = LabelFormatter,
                Fill = carbsColor
            });

            MacroSeries.Add(new PieSeries
            {
                Title = "Fett",
                Values = new ChartValues<double> { fat },
                DataLabels = true,
                LabelPoint = LabelFormatter,
                Fill = fatColor
            });
        }


        private void LoadWeightData()
        {
            try
            {
                if (File.Exists(WEIGHT_FILE))
                {
                    weightData = JsonConvert.DeserializeObject<Dictionary<string, double>>(
                        File.ReadAllText(WEIGHT_FILE));

                    // Update weight chart with loaded data
                    UpdateWeightChart();
                }
                else
                {
                    weightData = new Dictionary<string, double>();
                }

                // Display today's weight if available
                UpdateWeightDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Gewichtsdaten: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                weightData = new Dictionary<string, double>();
            }
        }

        private void TimeRangeRadio_Checked(object sender, RoutedEventArgs e)
        {
            UpdateWeightChart();
        }


        private void SaveWeight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (double.TryParse(WeightTextBox.Text, out double weight))
                {
                    string currentDateStr = currentDate.ToString("yyyy-MM-dd");
                    weightData[currentDateStr] = weight;
                    SaveWeightData();
                    UpdateWeightChart(); // Update the chart after saving
                    MessageBox.Show($"Gewicht für {currentDate.ToString("dd.MM.yyyy")} gespeichert.",
                        "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Bitte geben Sie ein gültiges Gewicht ein.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern des Gewichts: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveWeightData()
        {
            try
            {
                File.WriteAllText(WEIGHT_FILE, JsonConvert.SerializeObject(weightData, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Gewichtsdaten: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void UpdateDailySummary()
        {
            var totals = tracker.GetDailyTotals(currentDate.ToString("yyyy-MM-dd"));
            DailySummaryListBox.Items.Clear();

            foreach (var food in tracker.DailyLog.GetValueOrDefault(currentDate.ToString("yyyy-MM-dd"),
                         new List<FoodItem>()))
            {
                DailySummaryListBox.Items.Add(
                    $"{food.Name}:\n {food.Calories:F1} kcal (P:{food.Protein:F1}g, K:{food.Carbs:F1}g, F:{food.Fat:F1}g)");
            }

            double progress = (totals["calories"] / tracker.CalorieGoal) * 100;
            CalorieProgressBar.Value = Math.Min(progress, 100);
            ProgressLabel.Content = $"{totals["calories"]:F1} / {tracker.CalorieGoal} kcal ({progress:F1}%)";

            double proteinGoal = tracker.CalorieGoal * tracker.MacroRatios["protein"] / 4;
            double carbsGoal = tracker.CalorieGoal * tracker.MacroRatios["carbs"] / 4;
            double fatGoal = tracker.CalorieGoal * tracker.MacroRatios["fat"] / 9;
            double proteinProgress = (totals["protein"] / proteinGoal) * 100;
            double carbsProgress = (totals["carbs"] / carbsGoal) * 100;
            double fatProgress = (totals["fat"] / fatGoal) * 100;

            MacroTextBox.Text =
                $"Makronährstoffe:\nProtein: {totals["protein"]:F1}g / {proteinGoal:F1}g ({proteinProgress:F1}%)\nKohlenhydrate: {totals["carbs"]:F1}g / {carbsGoal:F1}g ({carbsProgress:F1}%)\nFett: {totals["fat"]:F1}g / {fatGoal:F1}g ({fatProgress:F1}%)";

            // Update pie chart
            UpdateMacroPieChart(totals["protein"], totals["carbs"], totals["fat"]);
        }

        private void LoadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSettings();
                LoadData();
                UpdateDailySummary();

                MessageBox.Show("Daten erfolgreich geladen.", "Erfolg", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Daten: {ex.Message}", "Fehler", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadWeightData();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData();
            SaveWeightData();
        }

        private void LoadData()
        {
            try
            {
                tracker.LoadFromJson("daily_log.json");
                UpdateDailySummary();
            }
            catch (Exception)
            {
                MessageBox.Show("Keine gespeicherten Daten gefunden.", "Fehler", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadFoodData(object sender, RoutedEventArgs e)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "food_data.json");

            if (!File.Exists(filePath))
            {
                string defaultContent = "[]";
                File.WriteAllText(filePath, defaultContent, Encoding.UTF8);
            }

            string jsonData = File.ReadAllText(filePath, Encoding.UTF8);
        }

        private void SaveData()
        {
            tracker.SaveDailyLogToJson("daily_log.json");
        }

        private void LoadSettings()
        {
            try
            {
                settings = JsonConvert.DeserializeObject<Dictionary<string, double>>(File.ReadAllText("settings.json"));
            }
            catch (Exception)
            {
                settings = new Dictionary<string, double>
                {
                    { "calorie_goal", 2000 },
                    { "protein_ratio", 0.3 },
                    { "carbs_ratio", 0.4 },
                    { "fat_ratio", 0.3 }
                };
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(settings));
        }

        private void DisplaySettings()
        {
            CalorieGoalTextBox.Text = settings["calorie_goal"].ToString(CultureInfo.CurrentCulture);
            ProteinTextBox.Text = (settings["protein_ratio"] * 100).ToString(CultureInfo.CurrentCulture);
            CarbsTextBox.Text = (settings["carbs_ratio"] * 100).ToString(CultureInfo.CurrentCulture);
            FatTextBox.Text = (settings["fat_ratio"] * 100).ToString(CultureInfo.CurrentCulture);
        }

        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (double.TryParse(CalorieGoalTextBox.Text, out double newGoal))
                {
                    settings["calorie_goal"] = newGoal;
                }

                if (double.TryParse(ProteinTextBox.Text, out double proteinPercentage))
                {
                    settings["protein_ratio"] = proteinPercentage / 100;
                }

                if (double.TryParse(CarbsTextBox.Text, out double carbsPercentage))
                {
                    settings["carbs_ratio"] = carbsPercentage / 100;
                }

                if (double.TryParse(FatTextBox.Text, out double fatPercentage))
                {
                    settings["fat_ratio"] = fatPercentage / 100;
                }

                double totalPercentage =
                    (settings["protein_ratio"] + settings["carbs_ratio"] + settings["fat_ratio"]) * 100;
                if (Math.Abs(totalPercentage - 100) > 1)
                {
                    MessageBox.Show("Die Makronährstoffe müssen in Summe 100% ergeben.", "Fehler", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                tracker.UpdateSettings((int)settings["calorie_goal"], settings["protein_ratio"],
                    settings["carbs_ratio"], settings["fat_ratio"]);

                UpdateDailySummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren der Einstellungen: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                SaveSettings();
                SaveData();

                MessageBox.Show("Daten erfolgreich gespeichert.", "Erfolg", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Daten: {ex.Message}", "Fehler", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PrevDay_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddDays(-1);
            DateLabel.Content = currentDate.ToString("dd.MM.yyyy");
            UpdateDailySummary();
            UpdateWeightDisplay();
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddDays(1);
            DateLabel.Content = currentDate.ToString("dd.MM.yyyy");
            UpdateDailySummary();
            UpdateWeightDisplay();
        }

        private void UpdateWeightDisplay()
        {
            string dateStr = currentDate.ToString("yyyy-MM-dd");
            if (weightData.ContainsKey(dateStr))
            {
                WeightTextBox.Text = weightData[dateStr].ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                WeightTextBox.Text = string.Empty;
            }
        }

        private void RemoveFood_Click(object sender, RoutedEventArgs e)
        {
            if (DailySummaryListBox.SelectedItem != null)
            {
                string selectedFood = DailySummaryListBox.SelectedItem.ToString();
                string foodName = selectedFood.Split(':')[0]; // Extract the food name

                string date = currentDate.ToString("yyyy-MM-dd");
                tracker.RemoveFood(date, foodName);
                tracker.SaveDailyLogToJson("daily_log.json");
                UpdateDailySummary();
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Nahrungsmittel aus der Liste aus.", "Fehler", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenAddMealWindow_Click(object sender, RoutedEventArgs e)
        {
            var addMealWindow = new AddMealWindow();
            var suggestions = JsonConvert.DeserializeObject<List<FoodItem>>(File.ReadAllText("food_data.json"));
            addMealWindow.SetFoodSuggestions(suggestions);
            if (addMealWindow.ShowDialog() == true)
            {
                tracker.AddFood(addMealWindow.MealName, addMealWindow.MealCalories, addMealWindow.MealProtein,
                    addMealWindow.MealCarbs, addMealWindow.MealFat, addMealWindow.MealAmount);
                UpdateDailySummary();
            }
        }
    }
}