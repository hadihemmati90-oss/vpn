using System;
using System.Windows;
using System.Windows.Media;
using XrayClient.Core;

namespace XrayClient
{
    public partial class MainWindow : Window
    {
        private ServiceController _service;
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            _service = new ServiceController();
            _service.OnLog += Log;
        }

        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            ConnectBtn.IsEnabled = false;
            try
            {
                if (!_isConnected)
                {
                    Log("Connecting...");
                    bool useTun = ModeToggle.IsChecked == true;
                    await _service.StartConnection(useTun);
                    
                    _isConnected = true;
                    ConnectBtn.Content = "DISCONNECT";
                    ConnectBtn.Background = Brushes.Red;
                    StatusText.Text = "Connected";
                    StatusText.Foreground = Brushes.LightGreen;
                }
                else
                {
                     Log("Disconnecting...");
                    _service.StopConnection();
                    _isConnected = false;
                     ConnectBtn.Content = "CONNECT";
                     ConnectBtn.Background = (Brush)FindResource("PrimaryHueMidBrush"); // Default Material Color
                     StatusText.Text = "Disconnected";
                     StatusText.Foreground = Brushes.Gray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log($"Error: {ex.Message}");
                // Reset state if failed inside start
                if (_isConnected) // If we thought we were connecting but failed
                {
                     _isConnected = false;
                     ConnectBtn.Content = "CONNECT";
                }
            }
            finally
            {
                ConnectBtn.IsEnabled = true;
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() => 
            {
                LogBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                LogScroll.ScrollToEnd();
            });
        }
    }
}
