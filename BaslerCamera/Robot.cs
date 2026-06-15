using BaslerCamera.IO;
using BaslerCamera;
using HalconDotNet;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Robot.YASKAWA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using TCPIP;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace BaslerCamera
{
    public interface IRobot
    {
        /// <summary>
        /// 获取最后一次错误信息
        /// </summary>
        /// <returns></returns>
        string ErrMsg { get; }
        /// <summary>
        /// 加载参数
        /// </summary>
        /// <returns></returns>
        bool Load();
        /// <summary>
        /// 保存参数
        /// </summary>
        /// <returns></returns>
        bool Save();
        /// <summary>
        /// 打开（连接）
        /// </summary>
        /// <returns></returns>
        bool Open();
        /// <summary>
        /// 打开（连接）
        /// </summary>
        /// <returns></returns>
        bool Open(string ip, int port);

        /// <summary>
        /// 是否已经打开
        /// </summary>
        /// <returns></returns>
        bool isOpen();

        /// <summary>
        /// 是否已经连接成功
        /// </summary>
        /// <returns></returns>
        bool isConnected();
        /// <summary>
        /// 关闭（断开）
        /// </summary>
        /// <returns></returns>
        bool Close();
        /// <summary>
        /// 获取坐标
        /// </summary>
        /// <param name="hPose"></param>
        /// <returns></returns>
        bool ReadPose(out HPose hPose);


        bool ReadAngle(out HTuple hAngel);
    }

    public class YRCRobot : IRobot
    {
        public string ErrMsg => _errMsg;
        string _errMsg;

        string _ip = string.Empty;
        int _port = 10040;
        YRCHighEthernet yrc = new YRCHighEthernet();
        public YRCRobot() { }

        public bool Read坐标(out string[] value)
        {
            OperateResult<byte[]> operateResult = yrc.ReadCommand(117, 101, 0, 1, null);
            if (operateResult.IsSuccess)
            {
                string[] array = new string[operateResult.Content.Length / 4];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = byteTransform.TransInt32(operateResult.Content, i * 4).ToString();
                }
                value = array;
                return true;
            }
            else
            {
                value = null;
                _errMsg = operateResult.Message;
                return false;
            }
        }
        public bool Read坐标(out HPose hPose)
        {
            OperateResult<string[]> read = yrc.ReadPose();//关节坐标
            if (read.IsSuccess)
            {
                double x = double.Parse(read.Content[0]) / 1000;
                double y = double.Parse(read.Content[1]) / 1000;
                double z = double.Parse(read.Content[2]) / 1000;
                double rx = double.Parse(read.Content[3]) / 10000;
                double ry = double.Parse(read.Content[4]) / 10000;
                double rz = double.Parse(read.Content[5]) / 10000;
                hPose = new HPose(x, y, z, rx, ry, rz, "Rp+T", "abg", "point");
            }
            else
            {
                hPose = null;
            }
            _errMsg = read.Message;
            return read.IsSuccess;
        }
        private IByteTransform byteTransform = new RegularByteTransform();
        public bool ReadPose(out HPose hPose)
        {
            OperateResult<byte[]> operateResult = yrc.ReadCommand(117, 101, 0, 1, null);
            if (operateResult.IsSuccess && operateResult.Content.Length >= 44)
            {
                int[] array = new int[6];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = byteTransform.TransInt32(operateResult.Content, 20 + i * 4);
                }
                hPose = new HPose(array[0] / 1000000, array[1] / 1000000, array[2] / 1000000, array[3] / 10000, array[4] / 10000, array[5] / 10000, "Rp+T", "abg", "point");
                return true;
            }
            else
            {
                hPose = null;
                _errMsg = operateResult.Message;
                return false;
            }
        }
        public bool ReadAngle(out HTuple hAngle)
        {
            hAngle = new HTuple();

            return true;
        }
        public bool Load()
        {
            _ip = "192.168.255.1";
            _port = 10040;
            return true;
        }

        public bool Save()
        {
            return true;
        }
        public bool isOpen()
        {
            return true;
        }

        public bool isConnected()
        {
            return true;
        }
        public bool Open()
        {
            yrc.IpAddress = _ip;
            yrc.Port = _port;
            return true;
        }
        public bool Open(string ip, int port)
        {
            _ip = ip;
            _port = port;
            return Open();
        }

        public bool Close()
        {
            return true;
        }
    }

    public class JAKARobot : IRobot
    {
        public string ErrMsg => _errMsg;
        string _errMsg;
        public bool IsOpen => _isOpen;
        bool _isOpen = false;

        string _ip = string.Empty;
        int _port = 6502;
        HslCommunication.ModBus.ModbusTcpNet modbus = new HslCommunication.ModBus.ModbusTcpNet();

        public JAKARobot() { }

        public bool ReadPose(out HPose hPose)
        {
            var operateResult = modbus.ReadFloat("x=4;406", 6);
            if (operateResult.IsSuccess)
            {
                var array = operateResult.Content;
                hPose = new HPose(array[0] / 1000, array[1] / 1000, array[2] / 1000, array[3], array[4], array[5], "Rp+T", "abg", "point");
                return true;
            }
            else
            {
                hPose = null;
                _errMsg = operateResult.Message;
                return false;
            }
        }

        public bool ReadAngle(out HTuple hAngle)
        {
            var operateResult = modbus.ReadFloat("x=4;382", 6);
            if (operateResult.IsSuccess)
            {
                var array = operateResult.Content;
                hAngle = new HTuple(array[0], array[1], array[2], array[3], array[4], array[5]);
                return true;
            }
            else
            {
                hAngle = null;
                _errMsg = operateResult.Message;
                return false;
            }
        }
        /// <summary>
        /// 读取输出信号DO1~DO128
        /// </summary>
        /// <param name="index">1~128</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ReadDO(int index, out bool value)
        {
            int address = 8 + index - 1;
            var operateResult = modbus.ReadBool($"x=2;{address}");
            value = operateResult.Content;
            _errMsg = operateResult.Message;
            return operateResult.IsSuccess;
        }
        /// <summary>
        /// 写入输入信号DI1~DI128
        /// </summary>
        /// <param name="index">1~128</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteDI(int index, bool value)
        {
            int address = 40 + index - 1;
            var operateResult = modbus.Write($"x=1;{address}", value);
            _errMsg = operateResult.Message;
            return operateResult.IsSuccess;
        }
        /// <summary>
        /// 读取输出信号AO1~AO32
        /// </summary>
        /// <param name="index">1~32</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ReadAO(int index, out ushort value)
        {
            int address = 96 + index - 1;
            var operateResult = modbus.ReadUInt16($"x=4;{address}");
            value = operateResult.Content;
            _errMsg = operateResult.Message;
            return operateResult.IsSuccess;
        }
        /// <summary>
        /// 写入输入信号AI1~AI32
        /// </summary>
        /// <param name="index">1~32</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteAI(int index, ushort value)
        {
            int address = 100 + index - 1;
            var operateResult = modbus.Write($"x=3;{address}", value);
            _errMsg = operateResult.Message;
            return operateResult.IsSuccess;
        }
        /// <summary>
        /// 读取输出信号AO33~AO64
        /// </summary>
        /// <param name="index">33~64</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ReadAO(int index, out float value)
        {
            int address = 128 + (index - 33) * 2;
            var operateResult = modbus.ReadUInt16($"x=4;{address}");
            value = operateResult.Content;
            _errMsg = operateResult.Message;
            return operateResult.IsSuccess;
        }
        /// <summary>
        /// 写入输入信号AI33~AI64
        /// </summary>
        /// <param name="index">33~64</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteAI(int index, float value)
        {
            int address = 132 + (index - 33) * 2;
            var operateResult = modbus.Write($"x=3;{address}", value);
            _errMsg = operateResult.Message;
            return operateResult.IsSuccess;
        }

        public bool Load()
        {
            _ip = "192.168.100.120";
            _port = 6502;
            return true;
        }

        public bool Save()
        {
            return true;
        }
        public bool isOpen()
        {
            return IsOpen;
        }

        public bool isConnected()
        {
            return true;
        }
        public bool Open()
        {
            modbus.IpAddress = _ip;
            modbus.Port = _port;
            modbus.ConnectTimeOut = 5000;     // 连接超时，单位毫秒
            modbus.ReceiveTimeOut = 3000;     // 接收超时，单位毫秒
            modbus.Station = 1;
            modbus.AddressStartWithZero = true;
            modbus.IsCheckMessageId = true;
            modbus.IsStringReverse = false;
            modbus.DataFormat = HslCommunication.Core.DataFormat.ABCD;

            var result = modbus.ConnectServer();
            if (result.IsSuccess)
            {
                _isOpen = true;
                return true;
            }
            else
            {
                _errMsg = result.Message;
                return false;
            }
        }
        public bool Open(string ip, int port)
        {
            _ip = ip;
            _port = port;
            return Open();
        }

        public bool Close()
        {
            modbus.ConnectClose();
            _isOpen = false;
            return true;
        }
    }

    public class FanucRobot : IRobot
    {
        public string ErrMsg => _errMsg;
        string _errMsg;
        public bool IsOpen => _isOpen;
        bool _isOpen = false;

        string _ip = string.Empty;
        int _port = 60008;
        HslCommunication.Robot.FANUC.FanucInterfaceNet robot = new HslCommunication.Robot.FANUC.FanucInterfaceNet();
        public FanucRobot() { }

        public bool ReadPose(out HPose hPose)
        {
            var read = robot.ReadFanucData();
            if (read.IsSuccess)
            {
                double x = read.Content.CurrentPose.Xyzwpr[0] / 1000;
                double y = read.Content.CurrentPose.Xyzwpr[1] / 1000;
                double z = read.Content.CurrentPose.Xyzwpr[2] / 1000;
                double rx = read.Content.CurrentPose.Xyzwpr[3];
                double ry = read.Content.CurrentPose.Xyzwpr[4];
                double rz = read.Content.CurrentPose.Xyzwpr[5];
                hPose = new HPose(x, y, z, rx, ry, rz, "Rp+T", "abg", "point");


            }
            else
            {
                hPose = null;
            }
            _errMsg = read.Message;
            return read.IsSuccess;
        }

        public bool ReadAngle(out HTuple hAngle)
        {
            hAngle = new HTuple();

            return true;
        }
        public bool Load()
        {
            _ip = "192.168.255.1";
            _port = 60008;
            return true;
        }

        public bool Save()
        {
            return true;
        }
        public bool isOpen()
        {
            return IsOpen;
        }

        public bool isConnected()
        {
            return true;
        }
        public bool Open()
        {
            robot.IpAddress = _ip;
            robot.Port = _port;
            robot.CommunicationPipe = new HslCommunication.Core.Pipe.PipeTcpNet(_ip, _port)
            {
                ConnectTimeOut = 2000,    // 连接超时时间，单位毫秒
                ReceiveTimeOut = 5000,    // 接收设备数据反馈的超时时间
                SleepTime = 0,
                SocketKeepAliveTime = -1,
                IsPersistentConnection = true,
            };
            var result = robot.ConnectServer();
            if (result.IsSuccess)
            {
                _isOpen = true;
                return true;
            }
            else
            {
                _errMsg = result.Message;
                return false;
            }
        }
        public bool Open(string ip, int port)
        {
            _ip = ip;
            _port = port;
            return Open();
        }

        public bool Close()
        {
            robot.ConnectClose();
            _isOpen = false;
            return true;
        }
    }

    public class KukaRobot : IRobot
    {

        public string serializationInfo(double x, double y, double z, double a, double b, double c, int result, string setflat)
        {
            // 直接构建 XML
            // 注意：我在 ROBOTPOS 最后加了一个空字符串 ""，这是为了防止它生成自闭合标签 <ROBOTPOS />
            // 某些机器人控制器比较严格，必须要有 </ROBOTPOS> 这种闭合标签
            XElement xml = new XElement("VISION",


                new XElement("ROBOTPOS",
                    new XAttribute("X", x),
                    new XAttribute("Y", y),
                    new XAttribute("Z", z),
                    new XAttribute("A", a),
                    new XAttribute("B", b),
                    new XAttribute("C", c),
                    "" // 技巧：强制生成 <ROBOTPOS></ROBOTPOS> 而不是 <ROBOTPOS />
                ),

                new XElement("RESULT", result),

                //$"Set_Flag=\"{setflat}\""
                new XAttribute("Set_Flag", setflat), "" // 属性：Set_Flag="11"
            );

            // 直接返回字符串，不需要 XML 头部声明
            return xml.ToString();
        }

        private static bool deserializationInfo(string info, out float X, out float Y, out float Z, out float RX, out float RY, out float RZ, out float R1, out float R2, out float R3, out float R4, out float R5, out float R6, out int programID, out int commandID, out int pointID)
        {

            XDocument doc = XDocument.Parse(info);
            X = 0;
            Y = 0;
            Z = 0;
            RX = 0;
            RY = 0;
            RZ = 0;
            R1 = 0;
            R2 = 0;
            R3 = 0;
            R4 = 0;
            R5 = 0;
            R6 = 0;
            programID = 0;
            commandID = 0;
            pointID = 0;

            try
            {
                var posAct = doc.Descendants("POSACT").First();
                X = (float)posAct.Attribute("X");
                Y = (float)posAct.Attribute("Y");
                Z = (float)posAct.Attribute("Z");
                RX = (float)posAct.Attribute("C"); // 注意这里是姿态的A
                RY = (float)posAct.Attribute("B");
                RZ = (float)posAct.Attribute("A");

                // 2. 获取 Axis 节点 (读取元素 Value)
                var axis = doc.Descendants("Axis").First();
                R1 = (float)axis.Element("A1");
                R2 = (float)axis.Element("A2");
                R3 = (float)axis.Element("A3");
                R4 = (float)axis.Element("A4");
                R5 = (float)axis.Element("A5");
                R6 = (float)axis.Element("A6");

                programID = (int)doc.Descendants("PROGRAM_ID").First();
                commandID = (int)doc.Descendants("COMMAND_ID").First();
                pointID = (int)doc.Descendants("POINT_ID").First();
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }


        public string ErrMsg => _errMsg;
        string _errMsg;
        public bool IsOpen => _isOpen;
        bool _isOpen = false;

        string _ip = string.Empty;
        int _port = 60008;
        //HslCommunication.Robot.FANUC.FanucInterfaceNet robot = new HslCommunication.Robot.FANUC.FanucInterfaceNet();

        TCP_Server robot;

        //寄存数据
        bool _isExist = false;
        float _X = 0, _Y = 0, _Z = 0, _RX = 0, _RY = 0, _RZ = 0,
            _R1 = 0, _R2 = 0, _R3 = 0, _R4 = 0, _R5 = 0, _R6 = 0;
        int _programID = 0, _commandID = 0, _pointID = 0;

        bool _isConnected = false;

        public KukaRobot()
        {

        }

        public bool ReadPose(out HPose hPose)
        {
            //var read = robot.ReadFanucData();
            if (_isExist)
            {
                double x = _X / 1000;
                double y = _Y / 1000;
                double z = _Z / 1000;
                double rx = _RX;
                double ry = _RY;
                double rz = _RZ;
                hPose = new HPose(x, y, z, rx, ry, rz, "Rp+T", "abg", "point");


                string info = serializationInfo(_X, _Y, _Z, _RZ, _RY, _RX, 1, "11");

                robot.Send(info + "\r\n");

            }
            else
            {
                hPose = null;
            }
            return _isExist;
        }
        public bool ReadAngle(out HTuple hAngle)
        {
            hAngle = new HTuple();

            if (_isExist)
            {
                double r1 = _R1;
                double r2 = _R2;
                double r3 = _R3;
                double r4 = _R4;
                double r5 = _R5;
                double r6 = _R6;
                hAngle = new HTuple(r1, r2, r3, r4, r5,r6);
                //string info = serializationInfo(_X, _Y, _Z, _RZ, _RY, _RX, 1, "11");

                //robot.Send(info + "\r\n");

            }
            else
            {
                hAngle = null;
            }

            return true;
        }

        public bool Load()
        {
            _ip = "192.168.255.1";
            _port = 60008;
            return true;
        }

        public bool Save()
        {
            return true;
        }
        void ProcessInfo(string info, string clientKey)
        {
            ///
            /// 信号处理
            ///

            //序列化信息
            float X, Y, Z, RX, RY, RZ, R1, R2, R3, R4, R5, R6;
            int programID, commandID, pointID;

            bool rt = deserializationInfo(info, out X, out Y, out Z, out RX, out RY, out RZ, out R1, out R2, out R3, out R4, out R5, out R6, out programID, out commandID, out pointID);
            // 更新缓存数据
            if (rt)
            {
                _isExist = true;
            }
            else
            {
                _isExist = false;
            }
            _X = X;
            _Y = Y;
            _Z = Z;
            _RX = RX;
            _RY = RY;
            _RZ = RZ;

            _R1 = R1;
            _R2 = R2;   
            _R3 = R3;
            _R4 = R4;
            _R5 = R5;
            _R6 = R6;


        }

        public bool isOpen()
        {
            return IsOpen;
        }

        public bool isConnected()
        {
            return true;
        }
        public bool Open()
        {
            robot = new TCP_Server(_ip, _port);
            //绑定委托与事件
            robot.reserveInfoSignal += ProcessInfo;
            //开始监听
            robot.StartListen();

            _isOpen = true;
            return true;
        }
        public bool Open(string ip, int port)
        {
            _ip = ip;
            _port = port;
            return Open();
        }

        public bool Close()
        {
            if (robot != null)
            {
                robot.StopListen();
            }
            _isOpen = false;
            return true;
        }
    }
public class KawasakiRobot : IRobot
    {

        public string serializationInfo(double x, double y, double z, double a, double b, double c, int result, string setflat)
        {
            return "";
        }

        private static bool deserializationInfo(string info, out float X, out float Y, out float Z, out float RX, out float RY, out float RZ, out float R1, out float R2, out float R3, out float R4, out float R5, out float R6, out int programID, out int commandID, out int pointID)
        {
            X = 0;
            Y = 0;
            Z = 0;
            RX = 0;
            RY = 0;
            RZ = 0;
            R1 = 0;
            R2 = 0;
            R3 = 0;
            R4 = 0;
            R5 = 0;
            R6 = 0;
            programID = 0;
            commandID = 0;
            pointID = 0;

            try
            {
                // 去掉末尾的 #
                string raw = info.TrimEnd('#').Trim();
                string[] parts = raw.Split(',');

                // 辅助：安全解析 float，空串或解析失败返回默认值
                float SafeFloat(int index, float defaultValue = 0f)
                {
                    if (index < 0 || index >= parts.Length) return defaultValue;
                    string s = parts[index].Trim();
                    if (string.IsNullOrEmpty(s)) return defaultValue;
                    return float.TryParse(s, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : defaultValue;
                }

                // 辅助：安全解析 int
                int SafeInt(int index, int defaultValue = 0)
                {
                    if (index < 0 || index >= parts.Length) return defaultValue;
                    string s = parts[index].Trim();
                    if (string.IsNullOrEmpty(s)) return defaultValue;
                    return int.TryParse(s, out int v) ? v : defaultValue;
                }

                // ---- 关节角 J1~J6 (索引 7~12) ----
                R1 = SafeFloat(7);    // J1: -86.6277
                R2 = SafeFloat(8);    // J2: -27.53063
                R3 = SafeFloat(9);    // J3: -18.65816
                R4 = SafeFloat(10);   // J4: -178.16177
                R5 = SafeFloat(11);   // J5: 12.41696
                R6 = SafeFloat(12);   // J6: -5.59366

                // ---- 末端位姿 X,Y,Z,RX,RY,RZ (倒数第 7~2 位) ----
                int total = parts.Length;
                X = SafeFloat(total - 6);   // -2446.65137
                Y = SafeFloat(total - 5);   // 218.11565
                Z = SafeFloat(total - 4);   // 2085.89941
                RX = SafeFloat(total - 3);   // 87.2569
                RY = SafeFloat(total - 2);   // 86.23324
                RZ = SafeFloat(total - 1);   // -3.54589

                //zyz 转 xyz
                double transformRX = 0, transformRY = 0, transformRz = 0;
                int robot_r_type = 2;   //机器人的坐标系类型，0为xyz，1为zyx，2为zyz
                int alg_r_type = 0;      //相机的坐标系类型，默认都是0，0为xyz，1为zyx，2为zyz

                Tool.transformCartPose2(RX, RY, RZ, robot_r_type, ref transformRX, ref transformRY, ref transformRz, alg_r_type);

                RX = (float)transformRX;
                RY = (float)transformRY;
                RZ = (float)transformRz;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[deserializationInfo] 解析异常: {ex.Message}");
                return false;
            }
        }


        public string ErrMsg => _errMsg;
        string _errMsg;
        public bool IsOpen => _isOpen;
        bool _isOpen = false;

        string _ip = string.Empty;
        int _port = 60008;
        //HslCommunication.Robot.FANUC.FanucInterfaceNet robot = new HslCommunication.Robot.FANUC.FanucInterfaceNet();

        TCP_Client robot;

        //寄存数据
        bool _isExist = false;
        float _X = 0, _Y = 0, _Z = 0, _RX = 0, _RY = 0, _RZ = 0,
            _R1 = 0, _R2 = 0, _R3 = 0, _R4 = 0, _R5 = 0, _R6 = 0;
        int _programID = 0, _commandID = 0, _pointID = 0;

        bool _isConnected = false;

        public KawasakiRobot()
        {

        }

        public bool ReadPose(out HPose hPose)
        {
            //var read = robot.ReadFanucData();
            if (_isExist)
            {
                double x = _X / 1000;
                double y = _Y / 1000;
                double z = _Z / 1000;
                double rx = _RX;
                double ry = _RY;
                double rz = _RZ;
                hPose = new HPose(x, y, z, rx, ry, rz, "Rp+T", "abg", "point");


                string info = serializationInfo(_X, _Y, _Z, _RZ, _RY, _RX, 1, "11");
                robot.Send(info + "\r\n");
            }
            else
            {
                hPose = null;
            }
            return _isExist;
        }

          public bool ReadAngle(out HTuple hAngle)
        {
            hAngle = new HTuple();

            if (_isExist)
            {
                double r1 = _R1;
                double r2 = _R2;
                double r3 = _R3;
                double r4 = _R4;
                double r5 = _R5;
                double r6 = _R6;
                hAngle = new HTuple(r1, r2, r3, r4, r5,r6);
                //string info = serializationInfo(_X, _Y, _Z, _RZ, _RY, _RX, 1, "11");

                //robot.Send(info + "\r\n");

            }
            else
            {
                hAngle = null;
            }

            return true;
        }


        public bool Load()
        {
            _ip = "192.168.255.1";
            _port = 60008;
            return true;
        }

        public bool Save()
        {
            return true;
        }
        void ProcessInfo(string info)
        {
            ///
            /// 信号处理
            ///

            //序列化信息
            float X, Y, Z, RX, RY, RZ, R1, R2, R3, R4, R5, R6;
            int programID, commandID, pointID;

            bool rt = deserializationInfo(info, out X, out Y, out Z, out RX, out RY, out RZ, out R1, out R2, out R3, out R4, out R5, out R6, out programID, out commandID, out pointID);
            // 更新缓存数据
            if (rt)
            {
                _isExist = true;
            }
            else
            {
                _isExist = false;
            }
            _X = X;
            _Y = Y;
            _Z = Z;
            _RX = RX;
            _RY = RY;
            _RZ = RZ;

            _R1 = R1;
            _R2 = R2;
            _R3 = R3;
            _R4 = R4;
            _R5 = R5;
            _R6 = R6;

        }

        public bool isOpen()
        {
            return IsOpen;
        }

        public bool isConnected()
        {
            return true;
        }
        public bool Open()
        {
            robot = new TCP_Client(_ip, _port);
            //绑定委托与事件
            robot.OnDataReceived += ProcessInfo;
            //开始监听
            robot.Connect();

            _isOpen = true;
            return true;
        }
        public bool Open(string ip, int port)
        {
            _ip = ip;
            _port = port;
            return Open();
        }

        public bool Close()
        {
            if (robot != null)
            {
                robot.Disconnect();
            }
            _isOpen = false;
            return true;
        }
    }
}
