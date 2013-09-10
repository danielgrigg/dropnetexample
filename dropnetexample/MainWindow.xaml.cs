using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using DropNet;

namespace dropnetexample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string appKey = "TODO_YOUR_CLIENT_ID";
        private const string appSecret = "TODO_YOUR_CLIENT_SECRET";

        private DropNet.DropNetClient _client;

        private System.Windows.Forms.WebBrowser browser1 = new System.Windows.Forms.WebBrowser();

        public MainWindow()
        {
            InitializeComponent();
            getButton.IsEnabled = false;
            putButton.IsEnabled = false;
            Task.Run(() => microServer(8000));
        }
        void microServer(int port)
        {
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var stream = client.GetStream();
                var data = new byte[client.ReceiveBufferSize];
                stream.Read(data, 0, Convert.ToInt32(client.ReceiveBufferSize));
                var msg = System.Text.Encoding.ASCII.GetBytes("HTTP/1.0 200 OK\r\n\r\nok\r\n");
                stream.Write(msg, 0, msg.Length);
                client.Close();
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                var tokenCachePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "droptoken");
                var secretCachePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dropsecret");
                if (File.Exists(tokenCachePath) && File.Exists(secretCachePath))
                {
                    var cachedUserToken = File.ReadAllText(tokenCachePath);
                    var cachedUserSecret = File.ReadAllText(secretCachePath);
                    _client = new DropNetClient(appKey, appSecret, cachedUserToken, cachedUserSecret);
                }
                else
                {
                    _client = new DropNetClient(appKey, appSecret);
                    var userToken = _client.GetToken();

                    var tokenUrl = _client.BuildAuthorizeUrl("http://localhost:8000/token");

                    browser1.DocumentCompleted += Browser1OnDocumentCompleted;
                    browser1.Navigated += Browser1OnNavigated;
                    browser1.ScriptErrorsSuppressed = true;
                    browser1.Navigate(new Uri(tokenUrl));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        private void Browser1OnNavigated(object sender, WebBrowserNavigatedEventArgs webBrowserNavigatedEventArgs)
        {
            throw new NotImplementedException();
        }

        private void Browser1OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (e.Url.AbsolutePath == "/token")
            {
                var response = _client.GetAccessToken();
                Debug.WriteLine("token {0}, secret {1}", response.Token, response.Secret);
                var tokenCachePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "droptoken");
                File.WriteAllText(tokenCachePath, response.Token);

                var secretCachePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dropsecret");
                File.WriteAllText(secretCachePath, response.Secret);
            }
        }

        private void putButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(putText.Text))
            {
                System.Windows.MessageBox.Show("Can't upload " + putText.Text);
                return;
            }
            var uploaded = _client.UploadFile("/", System.IO.Path.GetFileName(putText.Text), File.OpenRead(putText.Text));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var host = new System.Windows.Forms.Integration.WindowsFormsHost();

            host.Child = browser1;
            this.grid1.Children.Add(host);
        }

        private void getButton_Click(object sender, RoutedEventArgs e)
        {
            var d = new FolderBrowserDialog();
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                byte[] data = _client.GetFile(getText.Text);
                File.WriteAllBytes(System.IO.Path.Combine(d.SelectedPath, getText.Text), data);
            }
        }

        private void putText_TextChanged(object sender, TextChangedEventArgs e)
        {
            putButton.IsEnabled = !string.IsNullOrEmpty(putText.Text);
        }

        private void getText_TextChanged(object sender, TextChangedEventArgs e)
        {
            getButton.IsEnabled = !string.IsNullOrEmpty(getText.Text);
        }
    }
}
