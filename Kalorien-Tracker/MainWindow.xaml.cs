using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace Kalorien_Tracker
{
    public partial class MainWindow : Window
    {
        private CalorieTracker tracker;
        private DateTime currentDate;
        private Dictionary<string, double> settings;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += LoadFoodData;
            LoadSettings();
            LoadFoodData(null, null); // Call the method when the program starts
            tracker = new CalorieTracker((int)settings["calorie_goal"], settings["protein_ratio"],
                settings["carbs_ratio"], settings["fat_ratio"]);
            currentDate = DateTime.Today;
            tracker.LoadFoodData("food_data.json");
            UpdateDailySummary();
            DisplaySettings();
            DateLabel.Content = currentDate.ToString("dd.MM.yyyy");
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
        }

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

        public static int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            }

            if (string.IsNullOrEmpty(target))
            {
                return source.Length;
            }

            int[,] d = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= target.Length; d[0, j] = j++)
            {
            }

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[source.Length, target.Length];
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