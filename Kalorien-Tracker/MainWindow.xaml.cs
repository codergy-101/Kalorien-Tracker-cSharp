using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media; // Add this for Color and SolidColorBrush
using Newtonsoft.Json;
using LiveCharts;
using LiveCharts.Wpf;

namespace Kalorien_Tracker
{
    public partial class MainWindow : Window
    {
        private CalorieTracker tracker;
        private DateTime currentDate;
        private Dictionary<string, double> settings;

        // Add property for chart binding
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

            DataContext = this; // Set DataContext for binding
        }

        private void UpdateMacroPieChart(double protein, double carbs, double fat)
        {
            MacroSeries.Clear();

            // Define custom colors for each macro
            var fatColor = new SolidColorBrush(Color.FromRgb(231, 76, 60));   // Red fatColor
            var proteinColor = new SolidColorBrush(Color.FromRgb(46, 204, 113));    // Green proteinColor
            var carbsColor = new SolidColorBrush(Color.FromRgb(52, 152, 219));      // Blue carbsColor

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData();
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

        // Removed duplicate UpdateDailySummary method

        private void UpdateSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int newGoal = int.Parse(CalorieGoalTextBox.Text);
                double newProtein = double.Parse(ProteinTextBox.Text) / 100;
                double newCarbs = double.Parse(CarbsTextBox.Text) / 100;
                double newFat = double.Parse(FatTextBox.Text) / 100;

                if (Math.Abs(newProtein + newCarbs + newFat - 1.0) > 0.01)
                {
                    throw new ArgumentException("Makro-Verhältnis muss 100% ergeben");
                }

                settings["calorie_goal"] = newGoal;
                settings["protein_ratio"] = newProtein;
                settings["carbs_ratio"] = newCarbs;
                settings["fat_ratio"] = newFat;

                tracker.UpdateSettings(newGoal, newProtein, newCarbs, newFat);
                SaveSettings();
                MessageBox.Show("Einstellungen aktualisiert und gespeichert!", "Erfolg", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                UpdateDailySummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            tracker.SaveDailyLogToJson("daily_log.json");
            MessageBox.Show("Daten wurden gespeichert.", "Gespeichert", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void LoadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tracker.LoadFromJson("daily_log.json");
                MessageBox.Show("Daten wurden geladen.", "Geladen", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateDailySummary();
            }
            catch (Exception)
            {
                MessageBox.Show("Keine gespeicherten Daten gefunden.", "Fehler", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PrevDay_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddDays(-1);
            DateLabel.Content = currentDate.ToString("dd.MM.yyyy");
            UpdateDailySummary();
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddDays(1);
            DateLabel.Content = currentDate.ToString("dd.MM.yyyy");
            UpdateDailySummary();
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