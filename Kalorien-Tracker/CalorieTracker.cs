using System.IO;
using Newtonsoft.Json;

namespace Kalorien_Tracker;

public class CalorieTracker
{
    public int CalorieGoal { get; set; }
    public Dictionary<string, double> MacroRatios { get; set; }
    public Dictionary<string, List<FoodItem>>? DailyLog { get; set; }
    public List<FoodItem>? FoodData { get; set; }

    public CalorieTracker(int calorieGoal, double proteinRatio, double carbRatio, double fatRatio)
    {
        CalorieGoal = calorieGoal;
        MacroRatios = new Dictionary<string, double>
        {
            { "protein", proteinRatio },
            { "carbs", carbRatio },
            { "fat", fatRatio }
        };
        DailyLog = new Dictionary<string, List<FoodItem>>();
        FoodData = new List<FoodItem>();
    }

    public void AddFood(string name, double calories, double protein, double carbs, double fat, double amount)
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        if (DailyLog != null && !DailyLog.ContainsKey(today))
        {
            DailyLog[today] = new List<FoodItem>();
        }

        double factor = amount / 100.0;
        var foodItem = new FoodItem
        {
            Name = name,
            Calories = calories * factor,
            Protein = protein * factor,
            Carbs = carbs * factor,
            Fat = fat * factor
        };

        DailyLog?[today].Add(foodItem);
        SaveDailyLogToJson("daily_log.json");

        // Check if the food item already exists in FoodData
        if (FoodData != null && !FoodData.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            FoodData.Add(new FoodItem
            {
                Name = name,
                Calories = calories,
                Protein = protein,
                Carbs = carbs,
                Fat = fat
            });
            SaveFoodDataToJson("food_data.json");
        }
    }

    public Dictionary<string, double> GetDailyTotals(string day = null)
    {
        if (day == null)
        {
            day = DateTime.Today.ToString("yyyy-MM-dd");
        }

        if (DailyLog != null && !DailyLog.ContainsKey(day))
        {
            return new Dictionary<string, double>
            {
                { "calories", 0 },
                { "protein", 0 },
                { "carbs", 0 },
                { "fat", 0 }
            };
        }

        var totals = new Dictionary<string, double>
        {
            { "calories", 0 },
            { "protein", 0 },
            { "carbs", 0 },
            { "fat", 0 }
        };

        foreach (var food in DailyLog[day])
        {
            totals["calories"] += food.Calories;
            totals["protein"] += food.Protein;
            totals["carbs"] += food.Carbs;
            totals["fat"] += food.Fat;
        }

        return totals;
    }

    public void SaveDailyLogToJson(string filename)
    {
        File.WriteAllText(filename, JsonConvert.SerializeObject(DailyLog, Formatting.Indented));
    }

    public void RemoveFood(string date, string foodName)
    {
        if (DailyLog.ContainsKey(date))
        {
            var foodItem = DailyLog[date].FirstOrDefault(f => f.Name == foodName);
            if (foodItem != null)
            {
                DailyLog[date].Remove(foodItem);
            }
        }
    }

    public void SaveFoodDataToJson(string filename)
    {
        File.WriteAllText(filename, JsonConvert.SerializeObject(FoodData, Formatting.Indented));
    }

    public void LoadFromJson(string filename)
    {
        DailyLog = JsonConvert.DeserializeObject<Dictionary<string, List<FoodItem>>>(File.ReadAllText(filename));
    }

    public void LoadFoodData(string filename)
    {
        try
        {
            FoodData = JsonConvert.DeserializeObject<List<FoodItem>>(File.ReadAllText(filename));
        }
        catch (IOException)
        {
            FoodData = new List<FoodItem>();
        }
    }

    public void UpdateSettings(int calorieGoal, double proteinRatio, double carbRatio, double fatRatio)
    {
        if (Math.Abs(proteinRatio + carbRatio + fatRatio - 1.0) > 0.01)
        {
            throw new ArgumentException("Makro-Verhältnis muss 100% ergeben");
        }

        CalorieGoal = calorieGoal;
        MacroRatios = new Dictionary<string, double>
        {
            { "protein", proteinRatio },
            { "carbs", carbRatio },
            { "fat", fatRatio }
        };
    }
}

public class FoodItem
{
    public string Name { get; set; }
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
}