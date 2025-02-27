﻿using System.Windows;
using System.Windows.Controls;

namespace Kalorien_Tracker
{
    public partial class AddMealWindow : Window
    {
        public string MealName { get; private set; }
        public double MealCalories { get; private set; }
        public double MealProtein { get; private set; }
        public double MealCarbs { get; private set; }
        public double MealFat { get; private set; }
        public double MealAmount { get; private set; }

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
                // Filtere die Vorschläge basierend auf der Eingabe
                var closestMatches = foodSuggestions
                    .Where(f => f.Name.StartsWith(MealNameTextBox.Text, StringComparison.OrdinalIgnoreCase))
                    .Take(3) // Nimm die ersten 3 Übereinstimmungen
                    .Select(f => f.Name)
                    .ToList();

                SuggestionsListBox.ItemsSource = closestMatches;
                SuggestionsPopup.IsOpen = closestMatches.Any(); // Öffne das Popup, wenn es Vorschläge gibt
            }
            else
            {
                SuggestionsPopup.IsOpen = false; // Schließe das Popup, wenn keine Vorschläge vorhanden sind
            }
        }

        private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem != null)
            {
                var selectedFood = foodSuggestions.FirstOrDefault(f => f.Name == SuggestionsListBox.SelectedItem.ToString());
                if (selectedFood != null)
                {
                    MealNameTextBox.Text = selectedFood.Name;
                    MealCaloriesTextBox.Text = selectedFood.Calories.ToString();
                    MealProteinTextBox.Text = selectedFood.Protein.ToString();
                    MealCarbsTextBox.Text = selectedFood.Carbs.ToString();
                    MealFatTextBox.Text = selectedFood.Fat.ToString();
                }

                SuggestionsPopup.IsOpen = false; // Schließe das Popup nach der Auswahl
            }
        }

        private void AddMealButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MealName = MealNameTextBox.Text;
                MealCalories = double.Parse(MealCaloriesTextBox.Text);
                MealProtein = double.Parse(MealProteinTextBox.Text);
                MealCarbs = double.Parse(MealCarbsTextBox.Text);
                MealFat = double.Parse(MealFatTextBox.Text);
                MealAmount = double.Parse(MealAmountTextBox.Text);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Eingabe: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}