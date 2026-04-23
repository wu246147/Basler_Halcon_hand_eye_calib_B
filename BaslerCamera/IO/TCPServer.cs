using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPIP
{
    public class DataExchange : EventArgs
    {
        public EndPoint ip { get; set; }
        public Socket tmpSkt { get; set; }
        public string data { get; set; }
    }

    public class TCP_Server
    {
        public TCP_Server(string Ip, int Port)
        {
            this.ipAndPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
            this.mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        // 【核心修改1】使用字典来管理所有连接的客户端
        // Key: 客户端的IP端口字符串, Value: Socket对象
        // 使用 ConcurrentDictionary 保证多线程下的增删安全
        private ConcurrentDictionary<string, Socket> _clientSockets = new ConcurrentDictionary<string, Socket>();

        public delegate void DelDataArrived(DataExchange tmp);
        public event DelDataArrived OnDataArrivedEvent;
        public event DelDataArrived OnDiscoveredDeviceEvent;

        private readonly object _lock = new object();
        private bool _isListening = false;

        private IPEndPoint _IpAndPoint;
        public IPEndPoint ipAndPoint
        {
            get { return _IpAndPoint; }
            set { _IpAndPoint = value; }
        }

        private Socket _MySocket;
        public Socket mySocket
        {
            get { return _MySocket; }
            set { _MySocket = value; }
        }

        // 移除了原来的 ConnectSocket 属性，因为现在有多个客户端，不再需要单一变量

        private void StartAcceptLoop()
        {
            try
            {
                if (mySocket != null)
                {
                    mySocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("监听已停止。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"开始监听时发生错误: {ex.Message}");
                StartAcceptLoop();
            }
        }

        // 接受操作的回调函数
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = mySocket.EndAccept(ar);
                string clientKey = clientSocket.RemoteEndPoint.ToString();

                Console.WriteLine($"新客户端已连接: {clientKey}，当前连接数: {_clientSockets.Count + 1}");

                // 【核心修改2】将新客户端加入字典
                _clientSockets.TryAdd(clientKey, clientSocket);

                // 触发发现设备事件
                if (OnDiscoveredDeviceEvent != null)
                {
                    // 每次创建一个新的对象，避免多线程冲突
                    var args = new DataExchange
                    {
                        ip = clientSocket.RemoteEndPoint,
                        tmpSkt = clientSocket,
                        data = "Connected"
                    };
                    OnDiscoveredDeviceEvent(args);
                }

                // 为这个新客户端启动专门的接收任务
                _ = Task.Run(() => ReceiveDataFromClient(clientSocket, clientKey));

                // 继续监听下一个连接
                StartAcceptLoop();
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("监听Socket已关闭。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受客户端时发生错误: {ex.Message}");
                StartAcceptLoop();
            }
        }

        /// <summary>
        /// 接收信号的委托
        /// string 接收到信号的客户端ip:端口
        /// string 接收到的信息
        /// </summary>
        public event Action<string, string> reserveInfoSignal;

        public void StartListen()
        {
            lock (_lock)
            {
                if (_isListening) return;

                _isListening = true;
                // 重新创建 Socket 防止重启报错
                if (mySocket != null) mySocket.Close();
                mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mySocket.Bind(ipAndPoint);
                mySocket.Listen(100); // 增加挂起连接队列长度

                StartAcceptLoop();
            }
        }

        public void StopListen()
        {
            lock (_lock)
            {
                if (_isListening)
                {
                    _isListening = false;

                    // 停止监听
                    if (mySocket != null)
                    {
                        try { mySocket.Shutdown(SocketShutdown.Both); } catch { }
                        mySocket.Close();
                        mySocket = null;
                    }

                    // 【核心修改3】停止服务时，断开所有客户端
                    foreach (var kvp in _clientSockets)
                    {
                        try
                        {
                            if (kvp.Value.Connected) kvp.Value.Shutdown(SocketShutdown.Both);
                            kvp.Value.Close();
                        }
                        catch { }
                    }
                    _clientSockets.Clear();
                }
            }
        }

        // 【核心修改4】接收方法增加参数 clientKey，以便在断开时从字典中移除
        public Task ReceiveDataFromClient(Socket rcvSocket, string clientKey)
        {
            return Task.Run(() =>
            {
                // using (rcvSocket) // 不要在这里使用 using，因为我们要在 Send 或其他地方用到它，应该手动管理生命周期
                {
                    byte[] buffer = new byte[1024 * 1024]; // 建议把缓冲区调大一点，或者处理分包
                    try
                    {
                        while (true)
                        {
                            // int len = rcvSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                            // 使用这种方式可以更好地处理断开
                            if (!rcvSocket.Connected) break;

                            int len = rcvSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                            if (len > 0)
                            {
                                string msg = Encoding.Default.GetString(buffer, 0, len);

                                // 触发事件（建议将 msg 作为参数传递，而不是依赖全局变量 tmp）
                                if (OnDataArrivedEvent != null)
                                {
                                    var args = new DataExchange
                                    {
                                        ip = rcvSocket.RemoteEndPoint,
                                        tmpSkt = rcvSocket,
                                        data = msg
                                    };
                                    OnDataArrivedEvent(args);
                                }
                                if (reserveInfoSignal != null)
                                {
                                    reserveInfoSignal(msg, clientKey);
                                }
                            }
                            else
                            {
                                // len == 0 表示客户端正常关闭了连接
                                Console.WriteLine($"客户端 {clientKey} 断开连接。");
                                break;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        // 异常通常意味着强制断开或网络错误
                        Console.WriteLine($"客户端 {clientKey} 发生异常: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"接收数据异常: {ex.Message}");
                    }
                    finally
                    {
                        // 清理工作
                        RemoveClient(clientKey, rcvSocket);
                    }
                }
            });
        }

        /// <summary>
        /// 移除客户端并清理资源
        /// </summary>
        private void RemoveClient(string key, Socket skt)
        {
            // 从字典中移除
            Socket outSocket;
            if (_clientSockets.TryRemove(key, out outSocket))
            {
                Console.WriteLine($"客户端已移除: {key}，剩余连接数: {_clientSockets.Count}");
            }

            // 关闭 Socket
            if (skt != null)
            {
                try
                {
                    if (skt.Connected) skt.Shutdown(SocketShutdown.Both);
                }
                catch { } // 忽略关闭时的异常
                finally
                {
                    skt.Close();
                }
            }
        }

        /// <summary>
        /// 【核心修改5】发送数据给客户端
        /// </summary>
        /// <param name="Msg">消息内容</param>
        /// <param name="targetKey">指定发送给哪个客户端的IP端点字符串，如果为 null 则发送给所有客户端（广播）</param>
        public void Send(string Msg, string targetKey = null)
        {
            if (string.IsNullOrEmpty(Msg)) return;

            byte[] bytStr = Encoding.Default.GetBytes(Msg);

            // 获取所有要发送的目标 Socket 列表副本，避免遍历时修改字典导致异常
            var targets = _clientSockets.ToList();

            foreach (var kvp in targets)
            {
                // 如果指定了 targetKey，则只给匹配的客户端发
                // 这里只判断ip，不判断端口
                if (targetKey != null && kvp.Key.Split(':')[0] != targetKey)
                    continue;

                try
                {
                    Socket client = kvp.Value;
                    if (client != null && client.Connected)
                    {
                        // 同步发送，如果数据量大建议用 BeginSend
                        client.Send(bytStr, SocketFlags.None);
                    }
                }
                catch (Exception ex)
                {
                    // 发送失败，通常意味着对方已断开，将其移除
                    Console.WriteLine($"发送给 {kvp.Key} 失败: {ex.Message}");
                    RemoveClient(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// 获取当前所有连接的客户端信息
        /// </summary>
        public List<string> GetConnectedClients()
        {
            return _clientSockets.Keys.ToList();
        }
    }
}