using Lure.Net;
using Lure.Net.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Pegi.Client.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetClient client;

        public MainWindow()
        {
            InitializeComponent();

            Console.SetOut(new ControlWriter(LogTextBox));
            PegiLogging.Configure("Client GUI");

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            client = new NetClient("localhost", 45685);
            client.Start();
            client.SendMessage(new TestMessage
            {
                Integer = 10,
                Float = 20,
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            client?.Stop();
        }
    }
}
