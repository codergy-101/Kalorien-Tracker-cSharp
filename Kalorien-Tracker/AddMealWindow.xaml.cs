// In AddMealWindow.xaml.cs:

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace Kalorien_Tracker
{
    public partial class AddMealWindow : Window
    {
        public string MealName => MealNameTextBox.Text?.Trim() ?? "";
        public double MealCalories => double.TryParse(MealCaloriesTextBox.Text, out double calories) ? calories : 0;
        public double MealProtein => double.TryParse(MealProteinTextBox.Text, out double protein) ? protein : 0;
        public double MealCarbs => double.TryParse(MealCarbsTextBox.Text, out double carbs) ? carbs : 0;
        public double MealFat => double.TryParse(MealFatTextBox.Text, out double fat) ? fat : 0;
        public double MealAmount => double.TryParse(MealAmountTextBox.Text, out double amount) ? amount : 0;
        public string MealEAN => EANTextBox.Text?.Trim() ?? "";
        
        private List<FoodItem> foodSuggestions;

        public AddMealWindow()
        {
            InitializeComponent();

        }

        public void SetFoodSuggestions(List<FoodItem> suggestions)
        {
            foodSuggestions = suggestions;
        }

        private void MealNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (foodSuggestions != null && !string.IsNullOrWhiteSpace(MealNameTextBox.Text))
            {
                var closestMatches = foodSuggestions
                    .Where(f => f.Name.StartsWith(MealNameTextBox.Text, StringComparison.OrdinalIgnoreCase))
                    .Take(3)
                    .Select(f => f.Name)
                    .ToList();

                SuggestionsListBox.ItemsSource = closestMatches;
                SuggestionsPopup.IsOpen = closestMatches.Any();
            }
            else
            {
                SuggestionsPopup.IsOpen = false;
            }
        }
        
        private void EANTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (foodSuggestions != null && !string.IsNullOrWhiteSpace(EANTextBox.Text))
            {
                // Check if input is numeric
                if (!EANTextBox.Text.All(char.IsDigit))
                {
                    // Optional: Clear non-numeric characters
                    string numericOnly = new string(EANTextBox.Text.Where(char.IsDigit).ToArray());
                    if (EANTextBox.Text != numericOnly)
                    {
                        EANTextBox.Text = numericOnly;
                        EANTextBox.CaretIndex = numericOnly.Length;
                        return;
                    }
                }
        
                var eanQuery = EANTextBox.Text;
                var eanMatches = foodSuggestions
                    .Where(f => !string.IsNullOrEmpty(f.EAN) && f.EAN.Contains(eanQuery))
                    .Take(3)
                    .ToList();

                if (eanMatches.Any())
                {
                    // If we have an exact match, auto-fill the form
                    var exactMatch = eanMatches.FirstOrDefault(f => f.EAN == eanQuery);
                    if (exactMatch != null)
                    {
                        FillFormWithFoodItem(exactMatch);
                        return;
                    }

                    // Otherwise show suggestions
                    SuggestionsListBox.ItemsSource = eanMatches.Select(f => f.Name);
                    SuggestionsPopup.IsOpen = true;
                }
                else
                {
                    SuggestionsPopup.IsOpen = false;
                }
            }
            else
            {
                SuggestionsPopup.IsOpen = false;
            }
        }
        
        private void FillFormWithFoodItem(FoodItem food)
        {
            MealNameTextBox.Text = food.Name;
            MealCaloriesTextBox.Text = food.Calories.ToString();
            MealProteinTextBox.Text = food.Protein.ToString();
            MealCarbsTextBox.Text = food.Carbs.ToString();
            MealFatTextBox.Text = food.Fat.ToString();
            EANTextBox.Text = food.EAN ?? "";
        }

        private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem != null)
            {
                var selectedFood = foodSuggestions.FirstOrDefault(f => f.Name == SuggestionsListBox.SelectedItem.ToString());
                if (selectedFood != null)
                {
                    FillFormWithFoodItem(selectedFood);
                }

                SuggestionsPopup.IsOpen = false;
            }
        }

        private void AddMealButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateMealData())
            {
                if (ShouldSaveSuggestion.IsChecked == true)
                {
                    // Use constructor instead of object initializer
                    var newFoodItem = new FoodItem(
                        MealName,
                        MealCalories,
                        MealProtein,
                        MealCarbs,
                        MealFat,
                        100, // Base amount for suggestions
                        MealEAN);

                    foodSuggestions.Add(newFoodItem);
                    File.WriteAllText("food_data.json", JsonConvert.SerializeObject(foodSuggestions));
                }

                DialogResult = true;
            }
        }

        private bool ValidateMealData()
        {
            if (string.IsNullOrWhiteSpace(MealName))
            {
                MessageBox.Show("Bitte geben Sie einen Namen ein.", "Fehlerhafte Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (MealCalories <= 0)
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Wert für Kalorien ein.", "Fehlerhafte Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (MealAmount <= 0)
            {
                MessageBox.Show("Bitte geben Sie eine gültige Menge ein.", "Fehlerhafte Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}