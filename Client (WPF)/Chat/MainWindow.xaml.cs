using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Chat
{
    public partial class MainWindow : Window
    {
        Client network;
        IPAddress address;
        int port;

        bool debugMode;
        public bool DebugMode { get { return debugMode; } }

        public MainWindow()
        {
            InitializeComponent();
            address = IPAddress.Parse(Properties.Settings.Default.last_IP_Entry);
            port = Properties.Settings.Default.last_Port_Entry;
            debugMode = Properties.Settings.Default.debug_mode;
        }

        private void TnT_Chat_Loaded(object sender, RoutedEventArgs e)
        {
            Server_IP.Text = address.ToString();
            Server_Port.Text = port.ToString();
            DebugModeTrigger(debugMode);

            InitClient();
        }
        private void SendButton_Clicked(object sender, RoutedEventArgs e)
        {                       SendMessage();                          }
        private void MessageField_KeyDown(object sender, KeyEventArgs a)
        {
            if (a.Key == System.Windows.Input.Key.Enter)
                SendMessage();
        }
        private void SendMessage()
        {
            string text = RegexFormat(MessageField.Text);

            if (text.Length < 3)
                return;

            var paragraph = new Paragraph();
            paragraph.Background = Brushes.Gray;
            paragraph.LineHeight = 1;
            ChatBox.Document.Blocks.Add(paragraph);
            ChatBox.AppendText(" You: > " + text  + '\r');
            ChatBox.ScrollToEnd();

            network.SendAMessage(MessageField.Text);
            MessageField.Text = "";
        }
        private void ServerIP_Port_Updated(object sender, RoutedEventArgs e)
        {
            if (IPAddress.TryParse(Server_IP.Text, out address) &&
                int.TryParse(Server_Port.Text, out port))
            {
                network.Reset();                
                InitClient();
            }
            else
                MessageBox.Show("Wrong IP Address, try again.");
        }
        private void InitClient()
        {
            network = new Client(address, port);
            network.ReceiveChatBox(ChatBox);
            network.SetWindowReference(this);
        }
        private void DebugModeButton_Clicked(object sender, RoutedEventArgs e)
        { DebugModeTrigger(debugMode = !debugMode); }
        private void DebugModeTrigger(bool t)
        {
            Debug_Mode.Content = (t) ? "Debug Mode is [ON]" : "Debug Mode is [OFF]";
            Debug_Mode.BorderBrush = (t) ? Brushes.Red : Brushes.DarkGray;
        }
        public void ToggleMessageField()
        { SendButton.IsEnabled = MessageField.IsEnabled = network.ReadyToSend; }
        private static IPAddress FindLocalIP()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            return IPAddress.Parse("127.0.0.1");
        }
        ~MainWindow()
        {
            Properties.Settings.Default.last_IP_Entry = address.ToString();
            Properties.Settings.Default.last_Port_Entry = port;
            Properties.Settings.Default.debug_mode = debugMode;
            Properties.Settings.Default.Save();
        }
        private string RegexFormat(string text)
        { return Regex.Replace(text, @"^(([a-zA-Z0-9]|up|down|left|right|space|enter|tab|escape|nop|at|dot|slash|backslash|backspace|[\\\|\/_=><-])$|rm\s-|ctrl-[cdz])", ""); }
    }
}
