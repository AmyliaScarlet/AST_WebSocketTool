using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AST_WebSocketTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TextBox tbWSAdress;
        TextBox tbOutput;
        TextBox tbMsg;
        TextBox tbIp;
        TextBox tbPort;

        Button btnSvr;
        Button btnSvrClose;

        String sWSAdress = "";


        private ClientWebSocket cWebSocket;
        private WebSocket sWebSocket;
        private HttpListener listener;
        private bool bLocalSvr = false;

        public MainWindow()
        {
            InitializeComponent();

            Init();
        }

        private void Init()
        {
            tbWSAdress = (TextBox)FindName("TB_WS_Adress");
            tbOutput = (TextBox)FindName("TB_Output");
            tbMsg = (TextBox)FindName("TB_WS_Msg");

            tbIp = (TextBox)FindName("TB_IP");
            tbPort = (TextBox)FindName("TB_PORT");

            btnSvr = (Button)FindName("Btn_Svr");
            btnSvrClose = (Button)FindName("Btn_SvrClose");

            sWSAdress = "http://" + tbIp.Text + ":" + tbPort.Text + "/";
        }
        private async void Btn_SvrClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sWebSocket != null) 
                {
                    await sWebSocket.SendAsync(new ArraySegment<byte>(), WebSocketMessageType.Close, true, CancellationToken.None);
                }
                if (cWebSocket != null)
                {
                    await cWebSocket.SendAsync(new ArraySegment<byte>(), WebSocketMessageType.Close, true, CancellationToken.None);
                }

                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }

               
                bLocalSvr = false;
                tbIp.IsEnabled = !bLocalSvr;
                tbPort.IsEnabled = !bLocalSvr;
                tbOutput.Text += "本地WebSocket已停止" + "\n";
                btnSvr.IsEnabled = true;
                btnSvrClose.IsEnabled = false;
            }
            catch (Exception ex) 
            {
                tbOutput.Text += ex.Message + "\n";
            }

        }
        private async void Btn_Svr_Click(object sender, RoutedEventArgs e) 
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(sWSAdress);
                listener.Start();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    tbWSAdress.Text = sWSAdress.Replace("http://", "ws://");
                });

                bLocalSvr = true;
                tbIp.IsEnabled = !bLocalSvr;
                tbPort.IsEnabled = !bLocalSvr;
                tbOutput.Text += "本地WebSocket已启动" + "\n";
                btnSvr.IsEnabled = false;
                btnSvrClose.IsEnabled = true;
                while (true)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);

                        sWebSocket = webSocketContext.WebSocket;

                        byte[] buffer = new byte[1024];

                        var receivedDataBuffer = new ArraySegment<byte>(buffer);
                        while (sWebSocket != null && sWebSocket.State == WebSocketState.Open)
                        {

                            WebSocketReceiveResult result = await sWebSocket.ReceiveAsync(receivedDataBuffer, CancellationToken.None);
                            
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await sWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                                sWebSocket = null;
                            }
                            else
                            {
                                

                                // 处理接收到的数据
                                byte[] payloadData = receivedDataBuffer.Array.Where(b => b != 0).ToArray();
                                string receiveString = Encoding.UTF8.GetString(payloadData, 0, payloadData.Length);
                   
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    tbOutput.Text += "[服务端]" + DateTime.Now.ToString() + " 收到消息: " + receiveString + "\n";
                                });
                                // 发送响应数据
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(receiveString);


                                await sWebSocket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, responseBuffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    tbOutput.Text += "[服务端]" + DateTime.Now.ToString() + " 响应消息: 已收到" + receiveString + "\n";
                                });
                                

                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("由于线程退出或应用程序请求，已中止 I/O 操作。")) 
                {
                    bLocalSvr = false;
                    tbIp.IsEnabled = !bLocalSvr;
                    tbPort.IsEnabled = !bLocalSvr;      
                }
                else
                {
                    tbOutput.Text += ex.Message + "\n";
                }
                
            }
        }


        private async void Btn_Con_Click(object sender, RoutedEventArgs e)
        {
            if (cWebSocket != null && cWebSocket.State == WebSocketState.Open)
            {
                tbOutput.Text += "已连接Websocket" + "\n";
            }
            else
            {
                try
                {
                    cWebSocket = new ClientWebSocket();

                    await cWebSocket.ConnectAsync(new Uri(tbWSAdress.Text), CancellationToken.None);
                    tbOutput.Text += "连接成功" + "\n";

                    btnSvrClose.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    tbOutput.Text += ex.Message + "\n";
                }
            }
            
        }

        private void Btn_Claer_Click(object sender, RoutedEventArgs e)
        {
            tbOutput.Text = "";
        }

        private async void Btn_Uncon_Click(object sender, RoutedEventArgs e)
        {
            if (cWebSocket != null && cWebSocket.State == WebSocketState.Open)
            {
                await cWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "uncon", CancellationToken.None);
                cWebSocket = null;
                tbOutput.Text += "断开连接" + "\n";

                btnSvrClose.IsEnabled = true;

            }
            else 
            {
                tbOutput.Text += "未连接Websocket" + "\n";
            }

        }

        private async void Btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (cWebSocket != null && cWebSocket.State == WebSocketState.Open)
            {
                string msg = tbMsg.Text;
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                await cWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
               

                tbOutput.Text += "[客户端]" + DateTime.Now.ToString() + " 发送消息:" + msg + "\n";

                if (!bLocalSvr) 
                {
                    while (true)
                    {
                        var result = new byte[1024];

                        WebSocketReceiveResult cresult = await cWebSocket.ReceiveAsync(new ArraySegment<byte>(result), new CancellationToken());//接受数据
                        if (cresult.MessageType == WebSocketMessageType.Close && cresult.CloseStatusDescription == "uncon")
                        {
                            await cWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            cWebSocket = null;
                        }

                        var lastbyte = ByteCut(result, 0x00);

                        var str = Encoding.UTF8.GetString(lastbyte, 0, lastbyte.Length);

                        tbOutput.Text += "[客户端]" + DateTime.Now.ToString() + " 收到服务端消息: " + str + "\n";
                    }
                }


            }
            else
            {
                tbOutput.Text += "未连接Websocket" + "\n";
            }

        }

        public static byte[] ByteCut(byte[] b, byte cut)
        {
            var list = new List<byte>();
            list.AddRange(b);
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == cut)
                    list.RemoveAt(i);
            }
            var lastbyte = new byte[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                lastbyte[i] = list[i];
            }
            return lastbyte;
        }

        private void Btn_About_Click(object sender, RoutedEventArgs e)
        {
            String about = "本工具可用于测试你的Websocket连接与通讯，你也可以使用本工具建立一个本地Websocket连接，尝试IP及端口的可用性。\n" +
                "当使用本工具建立的本地Websocket连接时，客户端发送消息到服务端会输出消息的接收信息及响应信息。\n" +
                "当使用非本工具建立的Websocket连接时，客户端可以接收来自服务端发送的消息。\n\n" +
                "Designed and Developed By AmyliaScarlet .AST For WebSocketTool ©2016-"+ DateTime.Now.Year + "\n";
            MessageBox.Show(about, "关于", MessageBoxButton.OK, MessageBoxImage.None);
        }
    }
}
