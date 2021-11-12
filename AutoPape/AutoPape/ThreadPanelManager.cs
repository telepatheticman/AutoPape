using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoPape
{
    public class ThreadPanelManager
    {
        ProgressBar progressBar;
        Button saveButton;
        StackPanel threadPanel;

        int images;
        bool isProcessing;
        bool activeThread;

        public ThreadPanelManager(ProgressBar progress, Button button, StackPanel panel)
        {
            progressBar = progress;
            saveButton = button;
            threadPanel = panel;

            images = 0;
            isProcessing = false;
            activeThread = false;
            saveButton.IsEnabled = false;
        }

        public bool startProc(int images, bool clearList = true)
        {
            if(!isProcessing)
            {
                this.images = images;
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (clearList) threadPanel.Children.Clear();
                    saveButton.IsEnabled = false;
                    progressBar.Maximum = this.images;
                });
                isProcessing = true;
                return true;
            }
            return false;
        }

        public void endProc()
        {
            isProcessing = false;
            images = 0;
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                saveButton.IsEnabled = true;
                progressBar.Value = 0;
                progressBar.Maximum = 1;
            });
        }

        public void Add(Image toAdd, int image)
        {
            if (isProcessing)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    threadPanel.Children.Add(toAdd);
                    progressBar.Value = image + 1;
                });
            }
        }

        public void Add(int image)
        {
            if (isProcessing)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    progressBar.Value = image + 1;
                });
            }
        }
    }
}
