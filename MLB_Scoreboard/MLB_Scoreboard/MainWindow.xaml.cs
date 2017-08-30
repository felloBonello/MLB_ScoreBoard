using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLB_Scoreboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Rootobject> thisYearsDates;
        Year_Array year_a;
        Rootobject SelectedDate;
        int selectedYear = -1;

        public MainWindow()
        {
            InitializeComponent();           
            LoadData();           
            LoadComboBox();
            datesDisplay.DataContext = thisYearsDates;
        }

        private void LoadComboBox()
        {
            for (var i = 2000; i <= DateTime.Now.Year; ++i)
                year_cbo.Items.Add(i);
        }

        private void UpdateCache(Year y)
        {
            foreach(var date in y.data)
            {
                Games gameData = date.data.games;
                Games fetchedData = fetchDate(new DateTime(int.Parse(gameData.year), int.Parse(gameData.month), int.Parse(gameData.day))).data.games;
                if(fetchedData != null)
                    if (!fetchedData.modified_date.Equals(gameData.modified_date))
                        gameData = fetchedData;          
            }
        }

        private void LoadData()
        {
            year_a = new Year_Array()
            {
                years = new List<Year>()
            };
            int currentYear = DateTime.Now.Year;
            year_cbo.SelectedItem = currentYear;
            thisYearsDates = new ObservableCollection<Rootobject>();
            selectedYear = currentYear;
            dateSelect.DisplayDateStart = new DateTime(currentYear, 1, 1);
            dateSelect.DisplayDateEnd = new DateTime(currentYear, 12, 31);

            if (File.Exists("MLB.json"))
            {
                string json = System.IO.File.ReadAllText("MLB.json");
                year_a = JsonConvert.DeserializeObject<Year_Array>(json);

                if (year_a.years != null)
                    foreach (var y in year_a.years)
                    {
                        UpdateCache(y);
                        if (y.active)
                        {
                            year_cbo.SelectedItem = y.year;
                            thisYearsDates = y.data;
                            y.active = false;
                            selectedYear = y.year;
                            dateSelect.DisplayDateStart = new DateTime(y.year, 1, 1);
                            dateSelect.DisplayDateEnd = new DateTime(y.year, 12, 31);
                            //dateSelect.SetCurrentValue(Calendar.SelectedDateProperty, new DateTime(y.year, 1, 1));
                        }
                    }
            }
            else
            {
                StreamWriter output = new StreamWriter("MLB.json");
                output.Close();               
            }             
        }

        private Rootobject fetchDate(DateTime date)
        {
            try
            {
                string url = @"http://gd2.mlb.com/components/game/mlb/year_" + date.Year + "/month_" + date.Month.ToString("00") + "/day_" + date.Day.ToString("00") + "/master_scoreboard.json";
                WebClient client = new WebClient();
                string json = client.DownloadString(url);
                client.Dispose();
                Rootobject mlbData = JsonConvert.DeserializeObject<Rootobject>(json);
                if (mlbData.data.games.game == null)
                    return null;
                return mlbData;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;       
        }

        private void dateSelect_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if(dateSelect.SelectedDate != null)
            {
                foreach (var date in thisYearsDates)
                {
                    DateTime d = new DateTime(int.Parse(date.data.games.year), int.Parse(date.data.games.month), int.Parse(date.data.games.day));
                    if (d == dateSelect.SelectedDate)
                    {
                        SelectedDate = date;
                        return;
                    }
                }
                DateTime newDate = dateSelect.SelectedDate ?? default(DateTime);
                SelectedDate = fetchDate(newDate);
                if (SelectedDate != null)
                    thisYearsDates.Add(SelectedDate);
                else
                    MessageBox.Show("There is no game data for the that day.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void UpdateYear()
        {
            Year updatedYear = new Year()
            {
                year = selectedYear,
                active = false,
                data = thisYearsDates
            };
            year_a.years.Add(updatedYear);
        }

        private void CheckForCurrentYearChanges()
        {
            foreach (var y in year_a.years)
            {
                if (y.year == selectedYear)
                {                    
                    if (y.data.Count != thisYearsDates.Count)
                    {
                        year_a.years.Remove(y);
                        UpdateYear();
                    }
                    return;                       
                }               
            }
            if(thisYearsDates.Count > 0)
                UpdateYear();
            else
            {
                thisYearsDates = new ObservableCollection<Rootobject>();
                UpdateYear();
            }
        }
        
        private void findCurrentYear()
        {
            bool found = false;
            foreach (var y in year_a.years)
                if (y.year == selectedYear)
                {
                    thisYearsDates = y.data;
                    found = true;
                }
            if (!found)
                thisYearsDates = new ObservableCollection<Rootobject>();

            datesDisplay.DataContext = thisYearsDates;
            datesDisplay.Items.Refresh();
        }

        private void year_cbo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckForCurrentYearChanges();
            selectedYear = int.Parse(year_cbo.SelectedValue.ToString());
            findCurrentYear();

            dateSelect.SelectedDate = null;
            dateSelect.DisplayDateStart = new DateTime(selectedYear, 1, 1);
            dateSelect.DisplayDateEnd = new DateTime(selectedYear, 12, 31);
        }

        private void setCurrentYearToActive()
        {
            foreach(var y in year_a.years)
            {
                if (y.year == selectedYear)
                    y.active = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CheckForCurrentYearChanges();
            setCurrentYearToActive();
            try
            {
                string json = JsonConvert.SerializeObject(year_a);
                string path = AppDomain.CurrentDomain.BaseDirectory + "MLB.json";
                StreamWriter output = new StreamWriter(path);
                output.WriteLine(json);
                output.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

 
    }
}
