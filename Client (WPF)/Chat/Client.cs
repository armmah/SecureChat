using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Chat
{
    #region Client
    class Client
    {
        #region vars
        bool haveBeenWiped = false;
        bool communicating = false;
        public bool ReadyToSend { get { return communicating; } }

        IPEndPoint serverEP;
        NetworkStream connection;
        TcpClient client;
        Crypter encryption;
        Dispatcher dispatcher = Application.Current.Dispatcher;

        RichTextBox chatBox;
        MainWindow window;
        #endregion

        #region Constructor / Destructor
        public Client(IPEndPoint serverEP) { ConnectToServer(serverEP); }
        public Client(IPAddress adress, int port) { ConnectToServer(adress, port); }
        public void ReceiveChatBox(RichTextBox chatBox) { this.chatBox = chatBox; }
        ~Client() { Dispose(); }
        #endregion
        #region Connection
        public void ConnectToServer(IPAddress adress, int port)
        {
            if (haveBeenWiped)
                return;

            serverEP = new IPEndPoint(adress, port);
            ConnectToServer(serverEP);
        }
        public void ConnectToServer(IPEndPoint serverEP)
        {
            if (haveBeenWiped)
                return;

            Dispose();
            encryption = new Crypter();

            client = new TcpClient();
            this.serverEP = serverEP;

            ChatBoxShowDebugging("Attempting to connect to server.\r");
            client.BeginConnect(serverEP.Address, serverEP.Port, 
                new AsyncCallback(ConnectionWithServerEstablished), client);
        }
        public void SendAMessage(string text)
        {  WriteAsync(encryption.Do(text)); }
        #endregion
        #region Read From Server
        byte[] buf;
        private void ReadFromServer()
        {
            if (haveBeenWiped)
                return;
            try
            {
                buf = new byte[1500];
                //ChatBoxShow("Attempting to get server's response.\r");
                if(connection.CanRead)
                    connection.BeginRead(buf, 0, buf.Length, new AsyncCallback(ReceivedMessageFromServer), connection);
            }
            catch(SocketException e) { MessageBox.Show(e.ToString()); }
        }
        private void ConnectionWithServerEstablished(IAsyncResult ar)
        {
            if (haveBeenWiped)
                return;
            if (client == null)
            {
                ConnectToServer(serverEP);
                return;
            }

            if (!client.Connected)
            {
                Thread.Sleep(1000);
                ConnectToServer(serverEP);
                return;
            }
            client.EndConnect(ar);
            TcpClient cl = ar.AsyncState as TcpClient;
            if(cl != null && cl.Connected)
            {
                connection = cl.GetStream();
                ReadFromServer();
            }
            else
                ReadFromServer();
        }
        private void ReceivedMessageFromServer(IAsyncResult ar)
        {
            if (haveBeenWiped)
                return;
            try
            {
                connection.EndRead(ar);
                byte[] data = buf;

                //ChatBoxShow("Received a communication from server.\r");
                short state;
                string text;
                JsonMessage.GetStateAndText(data, out state, out text);

                switch (state)
                {
                    case (short)JsonMessage.ConnectionState.Closed:
                        {
                            communicating = false;
                            ChatBoxShow(" The second party have closed the channel, disconnecting...");
                            Dispose();
                            break;
                        }
                    case (short)JsonMessage.ConnectionState.Null:
                        {
                            ChatBoxShow(" Waiting for the other party to connect, please wait...\r");
                            communicating = false;
                            break;
                        }
                    case (short)JsonMessage.ConnectionState.Established:
                        {
                            ChatBoxShowDebugging(" Server is requesting a public key exchange.\r");
                            communicating = false;
                     
                            WriteAsync(new JsonMessage((short)JsonMessage.ConnectionState.Exchanged,
                                Convert.ToBase64String(encryption.PublicKey)));

                            break;
                        }
                    case (short)JsonMessage.ConnectionState.Exchanged:
                        {
                            encryption.ReceivePublicKey(Convert.FromBase64String(text));
                            ChatBoxShowDebugging(" Received a public key.\r");
                            ChatBoxShow(" A secure communication channel is established, you are welcome to send a message.\r");
                            communicating = true;
                            WriteAsync(new JsonMessage((short)JsonMessage.ConnectionState.Communicating, ""));
                            break;
                        }
                    case (short)JsonMessage.ConnectionState.Communicating:
                        {
                            communicating = true;
                            if (text != "")
                                ChatBoxShow(" Friend: > " + encryption.Undo(text) + '\r');
                            break;
                        }
                }

                ToggleWindow();
                ReadFromServer();
            } catch (SocketException e) { MessageBox.Show(e.ToString()); }
        }
        #endregion
        #region Write To Server
        private void WriteAsync(JsonMessage msg)
        {
            if (haveBeenWiped)
                return;
            WriteAsync(msg.State, msg.Message); }
        private async void WriteAsync(short state, string text)
        {
            if (haveBeenWiped)
                return;
            try
            {
                byte[] buf;
                ChatBoxShowDebugging(" Sent: " + "  " + text);
                buf = JsonMessage.GetBytes(state, text);
                await connection.WriteAsync(buf, 0, buf.Length);
            }
            catch (SocketException e)
            { Console.WriteLine("SocketException: {0}", e); }
        }
        private void WriteAsync(string text)
        {
            if (haveBeenWiped)
                return;
            if (!communicating)
                return;

            WriteAsync(new JsonMessage((short)JsonMessage.ConnectionState.Communicating, text));
        }
        #endregion
        #region other
        private void ToggleWindow()
        {
            if (haveBeenWiped)
                return;
            dispatcher.Invoke(new Action(() =>
            { window.ToggleMessageField(); }), DispatcherPriority.ContextIdle);
        }
        private void ChatBoxShowDebugging(string text)
        {
            if (haveBeenWiped)
                return;
            if (window != null && window.DebugMode) ChatBoxShow(" [Debug Mode] " + text); }
        private void ChatBoxShow(string text)
        {
            if (haveBeenWiped)
                return;

            dispatcher.Invoke(new Action(() =>
            {
                if(chatBox != null)
                {
                    var paragraph = new Paragraph();
                    paragraph.Background = (communicating) ? Brushes.Green : Brushes.Coral;
                    paragraph.LineHeight = 1;
                    chatBox.Document.Blocks.Add(paragraph);
                    chatBox.AppendText(text);
                    chatBox.ScrollToEnd();
                }
            }), DispatcherPriority.ContextIdle);
        }
        public void Dispose()
        {
            if (haveBeenWiped)
                return;

            if (client == null || !client.Connected)
                return;

            encryption = null;

            client.Client?.Shutdown(SocketShutdown.Both);
            client.Client?.Close();
            client.Client?.Dispose();

            connection?.Close();
            connection?.Dispose();
            
            client.Close();
            client.Dispose();
        }
        public void Reset()
        {
            Dispose();
            haveBeenWiped = true;

            buf = null;
            chatBox = null;
            client = null;
            communicating = false;
            connection = null;
            dispatcher = null;
            encryption = null;
            serverEP = null;
            window = null;
        }
        public void SetWindowReference(MainWindow window)
        {
            if (haveBeenWiped)
                return;
            this.window = window;
        }
        #endregion
    }
    #endregion
    #region Serializable JsonMessage
    public class JsonMessage
    {
        #region JsonProperties
        [JsonProperty]
        short state;
        [JsonProperty]
        string text;
        #endregion

        #region enum
        public enum ConnectionState : short
        {
            Null = 0,
            Established = 1,
            Exchanged = 2,
            Communicating = 3,
            Closed = 4
        };
        #endregion

        public short State { get { return state; } }
        public string Message { get { return text; } }
        public JsonMessage(short state, string text) { this.state = state; this.text = text; }


        #region Static Funcs for conversion
        public static byte[] GetBytes(short state, string text)
        { return GetBytes(new JsonMessage(state, text)); }
        public static byte[] GetBytes(JsonMessage jsonMessage)
        { return Encoding.UTF8.GetBytes((JsonConvert.SerializeObject(jsonMessage))); }
        public static JsonMessage GetJsonMessage(byte[] bytes)
        { return JsonConvert.DeserializeObject<JsonMessage>(Encoding.UTF8.GetString(bytes)); }
        public static void GetStateAndText(byte[] bytes, out short state, out string text)
        {
            JsonMessage jm = GetJsonMessage(bytes);
            if (jm == null) { state = (short)ConnectionState.Closed; text = ""; }
            else { state = jm.state; text = jm.text; }
        }
        #endregion
    }
#endregion
}