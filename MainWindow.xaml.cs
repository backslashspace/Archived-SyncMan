using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SyncMan
{
    public partial class MainWindow : Window
    {
        //##################### Meth-odes #######################################################################

        private void LogBoxAdd(String Text, SolidColorBrush Foreground, FontWeight FontWeight, Boolean ScrollToEnd = true, SolidColorBrush Background = null)
        {
            TextRange TeggsdRayndsch = new(LogTextBox.Document.ContentEnd, LogTextBox.Document.ContentEnd)
            {
                Text = Text
            };

            TeggsdRayndsch.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeight);

            if (Foreground != null)
            {
                TeggsdRayndsch.ApplyPropertyValue(TextElement.ForegroundProperty, Foreground);
            }

            if (Background != null)
            {
                TeggsdRayndsch.ApplyPropertyValue(TextElement.BackgroundProperty, Background);
            }

            if (ScrollToEnd == true)
            {
                LogBox.ScrollToEnd();
            }
        }

        private void LogBoxRemoveLine(UInt32 Lines = 1)
        {
            for (UInt32 I = 0; I < Lines; I++)
            {
                LogTextBox.Document.Blocks.Remove(LogTextBox.Document.Blocks.LastBlock);
            }
        }

        //############### Main ###############################

        public MainWindow()
        {
            InitializeComponent();

            LogTextBox.Document.PageWidth = 375;

            GetConfig();
        }

        //############# Buttons ##########################

        private void Debug(Object sender, RoutedEventArgs e)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);

            LogBoxAdd("\n" + finalString, Brushes.LightGray, FontWeights.Normal);
            //LogBoxAdd("\n#################################################iiiiii", Brushes.LightGray, FontWeights.Normal);
        }

        //###############################################################################

        //Window UI buttons

        //minimize
        private void Button_Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MinimizeButtonMouseIsOver(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MinimizeButtonColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d2d2d"));
        }

        private void MinimizeButtonMouseIsNotOver(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MinimizeButtonColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202020"));
        }

        private void MinimizeButtonMouseClick(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MinimizeButtonColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a2a2a"));
        }

        //close button

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CloseButtonColorMouseIsOver(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButtonColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c42b1c"));
        }

        private void CloseButtonColorMouseIsNotOver(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButtonColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202020"));
        }

        private void CloseButtonColorMouseClick(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButtonColor.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b22a1b"));
        }
    }
}
