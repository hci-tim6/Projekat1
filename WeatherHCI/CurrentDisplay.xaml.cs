using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace WeatherHCI
{
    /// <summary>
    /// Interaction logic for CurrentDisplay.xaml
    /// </summary>
    public partial class CurrentDisplay : UserControl
    {
        public CurrentDisplay()
        {
            InitializeComponent();
        }

        public void Favorite_Click(object sender, RoutedEventArgs e)
        {
            Favorite.Focusable = false;

            var window = FindParent<MainWindow>(this);
            var brush = new ImageBrush();

            if (window != null)
            {
                if (window.favoriteSet.Contains(Place.Content.ToString()))
                {
                    var children = window.Favorites.Children;
                    string text;

                    for (int i=1; i<children.Count; ++i)
                    {
                        if ((children[i] as TextBlock).Text.Equals(Place.Content.ToString()))
                        {
                            text = (children[i] as TextBlock).Text;
                            children.RemoveAt(i);
                            window.favoriteSet.Remove(text);
                            using (StreamWriter file =
                                new StreamWriter("../../favorites.txt", false))
                            {
                                foreach (var item in window.favoriteSet)
                                {
                                    file.WriteLine(item);
                                }
                            }
                            brush.ImageSource = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\favorite.png"));
                            Favorite.Background = brush;
                            return;
                        }
                    }
                    return;
                }

                brush.ImageSource = new BitmapImage(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location + "\\..\\..\\..\\resources\\icon\\favorited.png"));
                Favorite.Background = brush;

                TextBlock block = new TextBlock();
                block.FontSize = 24;

                block.Text = Place.Content.ToString();

                block.Foreground = Brushes.White;
                block.Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
                block.Padding = new Thickness(5);

                Thickness margin = block.Margin;
                margin.Bottom = 4;
                block.Margin = margin;

                block.Cursor = Cursors.Hand;

                block.MouseLeftButtonDown += (senderLocal, eLocal) =>
                {
                    window.WeatherLookup(block.Text);
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

                window.favoriteSet.Add(block.Text);
                window.Favorites.Children.Add(block);

                using (StreamWriter file =
                    new StreamWriter("../../favorites.txt", false))
                {
                    foreach (var item in window.favoriteSet)
                    {
                        file.WriteLine(item);
                    }
                }

            }

        }

        

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
} 
