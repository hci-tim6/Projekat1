using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using LiveCharts;
using LiveCharts.Wpf;

namespace WeatherHCI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public static string API_KEY = "d1a2e13daa7c30b697d45aa1d9a64c37";
        public static string GEOLOCATION_KEY = "861649dd3a7d02dca5f059367b80692e2e98da013c804a5e41d176ef";

        public string GeolocationUrl = "https://api.ipdata.co/?api-key=861649dd3a7d02dca5f059367b80692e2e98da013c804a5e41d176ef";
        public string CurrentUrl = "http://api.openweathermap.org/data/2.5/weather?q=" +
            "@LOC@&mode=xml&units=metric&APPID=" + API_KEY;
        public string ForecastUrl = "http://api.openweathermap.org/data/2.5/forecast?q=" +
            "@LOC@&mode=xml&units=metric&APPID=" + API_KEY;

        private List<List<XmlNode>> days = new List<List<XmlNode>>
            {
                new List<XmlNode>(),
                new List<XmlNode>(),
                new List<XmlNode>(),
                new List<XmlNode>(),
                new List<XmlNode>()
            };

        public List<DayDisplay> dayDisplays;
        public List<HourlyDisplay> hrDisplays;
        private List<Label> hrLabelDisplays;
        public List<Place> places;
        public List<string> placesStr = new List<string>();

        public HashSet<string> favoriteSet = new HashSet<string>();

        private bool hideAC = true;
        private int scrollIndex = -1;
        private int dailyIndex = 0;

        public SeriesCollection SeriesCollection { get; set; }
        public List<string> GraphLabels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("../../favorites.txt"))
            {
                using (StreamReader file = new StreamReader("../../favorites.txt", false))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        favoriteSet.Add(line);
                        addFavBlock(line);
                    }

                }
            }

            dayDisplays = new List<DayDisplay>
            {
                Daily1,
                Daily2,
                Daily3,
                Daily4,
                Daily5
            };

            hrDisplays = new List<HourlyDisplay>
            {
                Hr1,
                Hr2,
                Hr3,
                Hr4,
                Hr5,
                Hr6,
                Hr7,
                Hr8
            };

            hrLabelDisplays = new List<Label>
            {
                Hr1Label,
                Hr2Label,
                Hr3Label,
                Hr4Label,
                Hr5Label,
                Hr6Label,
                Hr7Label,
                Hr8Label
            };

            string bigJson;
            using (StreamReader sr = new StreamReader("../../resources/json/cities.json"))
            {
                bigJson = sr.ReadToEnd();
            }
            places = JsonConvert.DeserializeObject<List<Place>>(bigJson);

            foreach (Place p in places)
            {
                placesStr.Add(p.name + ", " + p.country);
            }

            using (WebClient client = new WebClient())
            {
                try
                {
                    string city = client.DownloadString("https://api.ipdata.co/city?api-key=" + GEOLOCATION_KEY);
                    string country = client.DownloadString("https://api.ipdata.co/country_code?api-key=" + GEOLOCATION_KEY);
                    WeatherLookup(city + ", " + country);

                }
                catch (WebException)
                {
                    MessageBox.Show("Could not find any weather information");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unknown error\n" + ex.Message);
                }
            }

        }

        public void SetGraph(int index)
        {
            double[] temperatureValues = new double[8];
            for (int i = 0; i < days[index].Count; i++)
                temperatureValues[i] = Math.Round(double.Parse(days[index][i].SelectSingleNode("temperature").Attributes["value"].Value));
            if (days[index].Count < 8)
            {
                for (int i = 0; i < 8 - days[index].Count; i++)
                    temperatureValues[i + days[index].Count] = Math.Round(double.Parse(days[index + 1][i].SelectSingleNode("temperature").Attributes["value"].Value));
            }

            string[] hourValues = new string[8];
            for (int i = 0; i < days[index].Count; i++)
                hourValues[i] = DateTime.ParseExact(days[index][i].Attributes["from"].Value.Substring(11, 5), "HH:mm", new CultureInfo("en-GB", false)).ToString("hh:mm tt");
            if (days[index].Count < 8)
            {
                for (int i = 0; i < 8 - days[index].Count; i++)
                    hourValues[i + days[index].Count] = DateTime.ParseExact(days[index + 1][i].Attributes["from"].Value.Substring(11, 5), "HH:mm", new CultureInfo("en-GB", false)).ToString("hh:mm tt");
            }

            if (SeriesCollection != null && SeriesCollection.Count != 0)
            {
                SeriesCollection.Clear();

                var lineSeries = new LineSeries
                {
                    Title = DateTime.ParseExact(days[index][0].Attributes["from"].Value.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture).DayOfWeek.ToString(),
                    Values = new ChartValues<double> {
                        temperatureValues[0], temperatureValues[1], temperatureValues[2], temperatureValues[3], temperatureValues[4],
                        temperatureValues[5], temperatureValues[6], temperatureValues[7]
                    }
                };
                lineSeries.Foreground = Brushes.White;
                lineSeries.Stroke = Brushes.White;
                SeriesCollection.Add(lineSeries);
            } else
            {
                var lineSeries = new LineSeries
                {
                    Title = DateTime.ParseExact(days[index][0].Attributes["from"].Value.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture).DayOfWeek.ToString(),
                    Values = new ChartValues<double> {
                        temperatureValues[0], temperatureValues[1], temperatureValues[2], temperatureValues[3], temperatureValues[4],
                        temperatureValues[5], temperatureValues[6], temperatureValues[7]
                    }
                };
                lineSeries.Foreground = Brushes.White;
                lineSeries.Stroke = Brushes.White;
                SeriesCollection = new SeriesCollection { lineSeries };
            }

            if (GraphLabels != null && GraphLabels.Count != 0)
            {
                GraphLabels.Clear();
                foreach (var hour in hourValues)
                {
                    GraphLabels.Add(hour);
                }
            } else
            {
                GraphLabels = new List<string> {
                        hourValues[0], hourValues[1], hourValues[2], hourValues[3], hourValues[4],
                        hourValues[5], hourValues[6], hourValues[7]
                };
            }


            YFormatter = value => value.ToString("N") + "℃";
            DataContext = this;

        }


        public void addFavBlock(string text)
        {
            TextBlock block = new TextBlock();
            block.FontSize = 24;

            block.Text = text;

            block.Cursor = Cursors.Hand;
            block.Foreground = Brushes.White;
            block.Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
            block.Padding = new Thickness(5);


            Thickness margin = block.Margin;
            margin.Bottom = 4;
            block.Margin = margin;

            block.MouseLeftButtonDown += (senderLocal, eLocal) =>
            {
                WeatherLookup(block.Text);
            };

            block.MouseEnter += (senderLocal, eLocal) =>
            {
                TextBlock b = senderLocal as TextBlock;
                b.Background = new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
            };

            block.MouseLeave += (senderLocal, eLocal) =>
            {
                TextBlock b = senderLocal as TextBlock;
                b.Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
            };

            Favorites.Children.Add(block);
        }

        private void FormatCurrent(string xml)
        {
            XmlDocument xml_doc = new XmlDocument();
            xml_doc.LoadXml(xml);

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            Current.Place.Content = xml_doc.SelectSingleNode("current/city").Attributes["name"].InnerText + ", " + xml_doc.SelectSingleNode("current/city/country").InnerText;
            Current.Image.Source = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\" + xml_doc.SelectSingleNode("current/weather").Attributes["icon"].InnerText + "_w.png"));
            Current.Temperature.Content = Math.Round(double.Parse(xml_doc.SelectSingleNode("current/temperature").Attributes["value"].InnerText)).ToString();
            Current.Prop1.Content = "Wind " + xml_doc.SelectSingleNode("current/wind/speed").Attributes["value"].InnerText + " km/h";
            Current.Prop2.Content = "Humidity " + xml_doc.SelectSingleNode("current/humidity").Attributes["value"].InnerText + "%";
            Current.Prop3.Content = "Pressure " + Math.Round(double.Parse(xml_doc.SelectSingleNode("current/pressure").Attributes["value"].InnerText)).ToString() + "hPa";
            Current.Description.Content = textInfo.ToTitleCase(xml_doc.SelectSingleNode("current/weather").Attributes["value"].InnerText);
        }

        private void FormatForecast(string xml)
        {
            for (int i = 0; i < days.Count; i++)
            {
                days[i].Clear();
            }
            // Load the response into an XML document.
            XmlDocument xml_doc = new XmlDocument();
            xml_doc.LoadXml(xml);

            // Load every time object into a list
            XmlNodeList allTimes = xml_doc.SelectNodes("weatherdata/forecast/time[@*]");

            // Divide hourly forecast by day
            int k = 0;
            string firstDay = allTimes[0].Attributes["from"].Value.Substring(8, 2);
            for (int i = 0; i < allTimes.Count; i++)
            {
                if (allTimes[i].Attributes["from"].Value.Substring(8, 2) != firstDay)
                {
                    firstDay = allTimes[i].Attributes["from"].Value.Substring(8, 2);
                    k++;
                    if (k == 5)
                    {
                        break;
                    }
                    this.days[k].Add(allTimes[i]);
                }
                else
                {
                    this.days[k].Add(allTimes[i]);
                }
            }
        }

        private void DisplayHourly(int index)
        {
            var day = days[index];

            foreach (var dayDisp in dayDisplays)
            {
                dayDisp.BorderThickness = new Thickness(0);
            }

            dayDisplays[index].BorderThickness = new Thickness(2);
            dayDisplays[index].BorderBrush = Brushes.White;

            foreach (Label lab in hrLabelDisplays)
            {
                lab.Content = "";
            }

            DateTime dt = DateTime.ParseExact(days[index][0].Attributes["from"].Value.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Hr1Label.Content = dt.DayOfWeek.ToString();

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            for (int i = 0; i < day.Count; i++)
            {
                hrDisplays[i].Icon.Source = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\" + day[i].SelectSingleNode("symbol").Attributes["var"].InnerText + "_w.png"));
                hrDisplays[i].Value.Content = Math.Round(double.Parse(day[i].SelectSingleNode("temperature").Attributes["value"].Value)).ToString() + "℃";
                hrDisplays[i].Desc.Content = textInfo.ToTitleCase(day[i].SelectSingleNode("symbol").Attributes["name"].Value);
                try
                {
                    hrDisplays[i].Perc.Content = Math.Round(double.Parse(day[i].SelectSingleNode("precipitation").Attributes["value"].Value) * 100).ToString() + "%";
                }
                catch (Exception)
                {
                    hrDisplays[i].Perc.Content = "0%";
                }
                double mps = double.Parse((day[i].SelectSingleNode("windSpeed ").Attributes["mps"].Value));
                double kmh = mps * 3.6;
                hrDisplays[i].Wind.Content = Math.Round(kmh) + "km/h";
                hrDisplays[i].Time.Content = DateTime.ParseExact(day[i].Attributes["from"].Value.Substring(11, 5), "HH:mm", new CultureInfo("en-GB", false)).ToString("hh:mm tt");
            }

            if (day.Count < 8)
            {
                DateTime dt2 = DateTime.ParseExact(days[index + 1][0].Attributes["from"].Value.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                hrLabelDisplays[day.Count].Content = dt2.DayOfWeek.ToString();
            }

            for (int i = 0; i < 8 - day.Count; i++)
            {
                hrDisplays[day.Count + i].Icon.Source = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\" + days[index + 1][i].SelectSingleNode("symbol").Attributes["var"].InnerText + "_w.png"));
                hrDisplays[day.Count + i].Value.Content = Math.Round(double.Parse(days[index + 1][i].SelectSingleNode("temperature").Attributes["value"].Value)).ToString() + "℃";
                hrDisplays[day.Count + i].Desc.Content = textInfo.ToTitleCase(days[index + 1][i].SelectSingleNode("symbol").Attributes["name"].Value);
                try
                {
                    hrDisplays[day.Count + i].Perc.Content = Math.Round(double.Parse(days[index + 1][i].SelectSingleNode("precipitation").Attributes["value"].Value) * 100).ToString() + "%";
                }
                catch (Exception)
                {
                    hrDisplays[day.Count + i].Perc.Content = "0%";
                }
                double mps = double.Parse((days[index + 1][i].SelectSingleNode("windSpeed ").Attributes["mps"].Value));
                double kmh = mps * 3.6;
                hrDisplays[day.Count + i].Wind.Content = Math.Round(kmh) + "km/h";
                hrDisplays[day.Count + i].Time.Content = DateTime.ParseExact(days[index + 1][i].Attributes["from"].Value.Substring(11, 5), "HH:mm", new CultureInfo("en-GB", false)).ToString("hh:mm tt");
            }
        }

        private void DisplayForecast()
        {
            double[] min = { double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue };
            double[] max = { double.MinValue, double.MinValue, double.MinValue, double.MinValue, double.MinValue };
            string[] description = { "", "", "", "", "" };
            string[] date = { "", "", "", "", "" };
            string[] icon = { "", "", "", "", "" };

            for (int i = 0; i < 5; i++)
            {
                min[i] = days[i].Min(element => Math.Round(double.Parse(element.SelectSingleNode("temperature").Attributes["min"].Value)));
                max[i] = days[i].Max(element => Math.Round(double.Parse(element.SelectSingleNode("temperature").Attributes["max"].Value)));

                DateTime dt = DateTime.ParseExact(days[i][0].Attributes["from"].Value.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                date[i] = dt.DayOfWeek.ToString() + ", " + dt.ToString("dd");

                var iconDict = new Dictionary<string, int>();
                var descDict = new Dictionary<string, int>();

                for (int j = 0; j < days[i].Count; j++)
                {

                    string desc = days[i][j].SelectSingleNode("symbol").Attributes["name"].Value;
                    if (descDict.ContainsKey(desc))
                    {
                        descDict[desc]++;
                    }
                    else
                    {
                        descDict[desc] = 1;
                    }

                    string iconName = days[i][j].SelectSingleNode("symbol ").Attributes["var"].Value;
                    if (iconDict.ContainsKey(iconName))
                    {
                        iconDict[iconName]++;
                    }
                    else
                    {
                        iconDict[iconName] = 1;
                    }
                }

                description[i] = descDict.Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
                icon[i] = iconDict.Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
            }

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            for (int i = 0; i < dayDisplays.Count; i++)
            {
                dayDisplays[i].Date.Content = date[i];
                dayDisplays[i].Min.Content = min[i] + "°";
                dayDisplays[i].Max.Content = max[i] + "°";
                dayDisplays[i].Desc.Content = textInfo.ToTitleCase(description[i]);
                dayDisplays[i].Icon.Source = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\" + icon[i] + "_w.png"));
            }

            DisplayHourly(0);
            SetGraph(0);
        }

        public void WeatherLookup(string city)
        {
            using (WebClient client = new WebClient())
            {
                // Get the response string from the URL.
                try
                {
                    FormatForecast(client.DownloadString(ForecastUrl.Replace("@LOC@", city)));
                    DisplayForecast();
                    FormatCurrent(client.DownloadString(CurrentUrl.Replace("@LOC@", city)));

                    var brush = new ImageBrush();

                    if (favoriteSet.Contains(Current.Place.Content.ToString()))
                    {

                        brush.ImageSource = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\favorited.png"));
                    }
                    else
                    {
                        brush.ImageSource = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\favorite.png"));
                    }
                    Current.Favorite.Background = brush;
                }
                catch (WebException)
                {
                    try {
                        FormatForecast(client.DownloadString(ForecastUrl.Replace("@LOC@", city.Split(',')[0])));
                        DisplayForecast();
                        FormatCurrent(client.DownloadString(CurrentUrl.Replace("@LOC@", city.Split(',')[0])));
                    } catch (WebException)
                    {
                        MessageBox.Show("Could not find any weather information");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unknown error\n" + ex.Message);
                }
            }
        }

        public void Btn_Get_Forecast(object sender, RoutedEventArgs e)
        {
            if (scrollIndex == -1 && resultStack.Children.Count > 0)
            {
                textBox.Text = (resultStack.Children[0] as TextBlock).Text;
            }

            if (scrollIndex != -1 && resultStack.Children.Count > 0)
            {
                textBox.Text = (resultStack.Children[scrollIndex] as TextBlock).Text;
            }

            WeatherLookup(textBox.Text);
            textBox.Clear();

            e.Handled = true;
            MainScroll.Focus();            
            dailyIndex = 0;
        }

        private void Favorites_Click(object sender, RoutedEventArgs e)
        {
            FavoritesBtn.Focusable = false;

            if (Favorites.Visibility == Visibility.Collapsed)
            {
                Favorites.Visibility = Visibility.Visible;
            }
            else
            {
                Favorites.Visibility = Visibility.Collapsed;
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Down) || e.Key.Equals(Key.Up))
            {
                return;
            }

            if(e.Key == Key.Escape)
            {
                e.Handled = true;
                MainScroll.Focus();
                return;
            }

            bool found = false;
            var border = (resultStack.Parent as ScrollViewer).Parent as Border;
            var data = placesStr;
            scrollIndex = -1;

            string query = (sender as TextBox).Text;

            if (query.Length == 0)
            {
                // Clear   
                resultStack.Children.Clear();
                border.Visibility = System.Windows.Visibility.Hidden;
                border.Opacity = 0;
            }
            else
            {
                border.Visibility = System.Windows.Visibility.Visible;
                border.Opacity = 1;
            }

            // Clear the list   
            resultStack.Children.Clear();

            if (query.Length > 0)
            {
                // Add the result   
                foreach (var obj in data)
                {
                    if (query.Length < 3)
                    {
                        if (obj.ToLower().StartsWith(query.ToLower()) && obj.Length <= textBox.Text.Length + 7)
                        {
                            addItem(obj);
                            found = true;
                        }
                    }
                    else
                    {
                        if (obj.ToLower().StartsWith(query.ToLower()))
                        {
                            addItem(obj);
                            found = true;
                        }
                    }
                }
            }

            if (!found && query.Length > 0)
            {
                resultStack.Children.Add(new TextBlock() { Text = "No results found", FontSize = 14 });
            }
        }

        private void addItem(string text)
        {
            TextBlock block = new TextBlock();
            block.FontSize = 12;

            block.Text = text;

            block.Cursor = Cursors.Hand;

            block.MouseLeftButtonDown += (sender, e) =>
            {
                textBox.Text = (sender as TextBlock).Text;
                WeatherLookup(textBox.Text);
                textBox.Clear();
            };

            block.MouseEnter += (sender, e) =>
            {
                TextBlock b = sender as TextBlock;
                b.Background = Brushes.LightBlue;
            };

            block.MouseLeave += (sender, e) =>
            {
                TextBlock b = sender as TextBlock;
                b.Background = Brushes.Transparent;
            };

            resultStack.Children.Add(block);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!hideAC)
            {
                hideAC = true;
                return;
            }

            var border = (resultStack.Parent as ScrollViewer).Parent as Border;
            border.Visibility = System.Windows.Visibility.Hidden;
            border.Opacity = 0;
            scrollIndex = -1;
        }

        private void ResultStack_LostFocus(object sender, RoutedEventArgs e)
        {
            var border = (resultStack.Parent as ScrollViewer).Parent as Border;
            border.Visibility = System.Windows.Visibility.Hidden;
            border.Opacity = 0;
            scrollIndex = -1;
        }

        private void ResultStack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            hideAC = true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    if (scrollIndex == resultStack.Children.Count - 1)
                    {
                        return;
                    }
                    scrollIndex++;
                    for (int i = 0; i < resultStack.Children.Count; i++)
                    {
                        (resultStack.Children[i] as TextBlock).Background = Brushes.Transparent;
                    }
                    (resultStack.Children[scrollIndex] as TextBlock).Background = Brushes.LightBlue;
                    (resultStack.Children[scrollIndex] as TextBlock).BringIntoView();
                    break;
                case Key.Up:
                    if (scrollIndex <= 0)
                    {
                        return;
                    }
                    scrollIndex--;
                    for (int i = 0; i < resultStack.Children.Count; i++)
                    {
                        (resultStack.Children[i] as TextBlock).Background = Brushes.Transparent;
                    }
                    (resultStack.Children[scrollIndex] as TextBlock).Background = Brushes.LightBlue;
                    (resultStack.Children[scrollIndex] as TextBlock).BringIntoView();
                    break;
                case Key.Enter:
                    Btn_Get_Forecast(sender, e);
                    break;
            }
        }

        private void ResultStack_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox_KeyDown(sender, e);
        }

        private void JeffTheBorder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox_KeyDown(sender, e);
        }

        private void JeffTheScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox_KeyDown(sender, e);
        }

        private void JeffTheScrollViewer_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox_KeyDown(sender, e);
        }

        private void ResultStack_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox_KeyDown(sender, e);
        }

        private void Daily1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DisplayHourly(0);
            SetGraph(0);
        }

        private void Daily2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DisplayHourly(1);
            SetGraph(1);
        }

        private void Daily3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DisplayHourly(2);
            SetGraph(2);
        }

        private void Daily4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DisplayHourly(3);
            SetGraph(3);
        }

        private void Daily5_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DisplayHourly(4);
            SetGraph(4);
        }

        private void Main_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (textBox.IsFocused)
            {
                return;
            }

            if(e.Key == Key.Tab)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && Favorites.Visibility == Visibility.Visible)
            {
                Favorites.Visibility = Visibility.Collapsed;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down)
            {
                MainScroll.ScrollToBottom();
                return;
            }

            if(e.Key == Key.Up)
            {
                MainScroll.ScrollToTop();
                return;
            }

            if (e.Key == Key.Right)
            {
                ++dailyIndex;
                dailyIndex %= 5;
                DisplayHourly(dailyIndex);
                SetGraph(dailyIndex);
            }
            else if (e.Key == Key.Left)
            {
                dailyIndex += 4;
                dailyIndex %= 5;
                DisplayHourly(dailyIndex);
                SetGraph(dailyIndex);
            }
        }

        private void DisplaySwitch_Click(object sender, RoutedEventArgs e)
        {
            ColumnPanel.Visibility = Visibility.Collapsed;
            GraphPanel.Visibility = Visibility.Visible;
        }

        private void DisplayCards_Click(object sender, RoutedEventArgs e)
        {
            ColumnPanel.Visibility = Visibility.Visible;
            GraphPanel.Visibility = Visibility.Collapsed;
        } 

        private void ShowFavorites_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Favorites_Click(sender, e);
        }

        private void ToggleFavorite_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Current.Favorite_Click(sender, e);
        }

        private void Search_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            textBox.Focus();
        }

        private void Display_Hourly_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ColumnPanel.Visibility = Visibility.Visible;
            GraphPanel.Visibility = Visibility.Collapsed;
        }

        private void Display_Graph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ColumnPanel.Visibility = Visibility.Collapsed;
            GraphPanel.Visibility = Visibility.Visible;
        }

        private void TextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool found = false;
            var border = (resultStack.Parent as ScrollViewer).Parent as Border;
            var data = placesStr;
            scrollIndex = -1;

            string query = (sender as TextBox).Text;

            if (query.Length == 0)
            {
                // Clear   
                resultStack.Children.Clear();
                border.Visibility = System.Windows.Visibility.Hidden;
                border.Opacity = 0;
            }
            else
            {
                border.Visibility = System.Windows.Visibility.Visible;
                border.Opacity = 1;
            }

            // Clear the list   
            resultStack.Children.Clear();

            if (query.Length > 0)
            {
                // Add the result   
                foreach (var obj in data)
                {
                    if (query.Length < 3)
                    {
                        if (obj.ToLower().StartsWith(query.ToLower()) && obj.Length <= textBox.Text.Length + 7)
                        {
                            addItem(obj);
                            found = true;
                        }
                    } else
                    {
                        if (obj.ToLower().StartsWith(query.ToLower()))
                        {
                            addItem(obj);
                            found = true;
                        }
                    }
                }
            }

            if (!found && query.Length > 0)
            {
                resultStack.Children.Add(new TextBlock() { Text = "No results found", FontSize = 14 });
            }
        }
    }
    public static class MyCommand
    {

        public static readonly RoutedUICommand ShowFavorites = new RoutedUICommand(
            "Show Favorites",
            "ShowFavorites",
            typeof(MyCommand),
            new InputGestureCollection()
            {
                new KeyGesture(Key.X, ModifierKeys.Control),
            }
        );

        public static readonly RoutedUICommand ToggleFavorite = new RoutedUICommand(
            "Toggle Favorites",
            "ToggleFavorites",
            typeof(MyCommand),
            new InputGestureCollection()
            {
                new KeyGesture(Key.D, ModifierKeys.Control),
            }
        );

        public static readonly RoutedUICommand Search = new RoutedUICommand(
            "Search",
            "Search",
            typeof(MyCommand),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F, ModifierKeys.Control),
            }
        );

        public static readonly RoutedUICommand DisplayHourly = new RoutedUICommand(
            "Display Hourly",
            "DisplayHourly",
            typeof(MyCommand),
            new InputGestureCollection()
            {
                new KeyGesture(Key.H, ModifierKeys.Control),
            }
        );

        public static readonly RoutedUICommand DisplayGraph = new RoutedUICommand(
            "Display Graph",
            "DisplayGraph",
            typeof(MyCommand),
            new InputGestureCollection()
            {
                new KeyGesture(Key.G, ModifierKeys.Control),
            }
        );
    }
}
