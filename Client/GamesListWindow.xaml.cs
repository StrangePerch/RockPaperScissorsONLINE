using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using NetLib;

namespace Client
{
    /// <summary>
    /// Interaction logic for ServerList.xaml
    /// </summary>
    public partial class ServerList : Window
    {
        public ServerList()
        {
            InitializeComponent();
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            GamePanel.ItemsSource = ((GamesPacket)ConnectionManager.SendWithResult(new BasePacket(Commands.RequestGames))).Games;
        }

        private void HostButton_OnClick(object sender, RoutedEventArgs e)
        {
            new HostWindow().ShowDialog();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var packet = new ConnectPacket((GamePanel.SelectedValue as GamePacket)?.Name);
            var game = (GamePacket)ConnectionManager.SendWithResult(packet);
            new GameWindow(false, game.Name, game.Host, game.MaxScore).ShowDialog();
        }
    }
}
