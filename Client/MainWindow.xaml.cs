using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using NetLib;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogInButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (UsernameBox.Text.Length <= 2)
            {
                Console.WriteLine("Username length <= 2 isn't allowed");
                return;
            }
            try
            {
                ConnectionManager.SendWithErrorAnswer(new UserDataPacket(UsernameBox.Text));
                ConnectionManager.ClientName = UsernameBox.Text;
                new ServerList().Show();
                Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            LogInButton.IsEnabled = false;
            ConnectionManager.Connect();
            LogInButton.IsEnabled = true;
        }
    }
}
