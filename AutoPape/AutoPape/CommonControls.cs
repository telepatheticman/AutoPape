using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutoPape
{
    public class ThreadButton
    {
        public Button button;
        public StackPanel content;
        public TextBlock text;
        public Image threadImage;
        public ThreadButton()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                button = new Button();
                content = new StackPanel();
                text = new TextBlock();
                threadImage = new Image();
            

            button.Content = content;
            button.VerticalContentAlignment = VerticalAlignment.Top;
            button.Width = 200;
            button.Height = Double.NaN;
            button.Margin = new Thickness(10);
            
            content.VerticalAlignment = VerticalAlignment.Top;

            text.Text = "";
            text.TextWrapping = TextWrapping.Wrap;

            content.Children.Add(threadImage);
            content.Children.Add(text);
            });
        }

        public void addTextLine(string toAdd)
        {
            text.Text += toAdd + "\n";
        }

        public void addText(string toAdd)
        {
            text.Text += toAdd;
        }

        public void setClick(RoutedEventHandler handler)
        {
            button.Click += handler;
        }

        public void removeClick(RoutedEventHandler handler)
        {
            button.Click -= handler;
        }
    }
}
