using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Basler.Pylon;
using System.IO;
using System.Diagnostics;
using System.Threading;
using HalconDotNet;
using System.Drawing.Drawing2D;
using static BaslerCamera.MvCamera;
using System.Reflection;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using static System.Net.Mime.MediaTypeNames;

namespace BaslerCamera
{
    public partial class FormBaslerCam : Form
    {
        Cam cam = new Cam();
        IRobot robot = new JAKARobot();
        HTuple hv_WindowHandle = null;
        bool _isAlter = false;

        Stopwatch stopWatch = new Stopwatch();
        public FormBaslerCam()
        {
            InitializeComponent();
            dataGridViewLog.Columns[1].Width = panelWindow.Width - dataGridViewLog.Columns[0].Width - 23;
            ButtonsEnable(false);

            HalconDotNet.HWindowControl hWindowControl1 = new HalconDotNet.HWindowControl();
            //hWindowControl1.BackColor = System.Drawing.Color.Black;
            //hWindowControl1.BorderColor = System.Drawing.Color.Black;
            //hWindowControl1.ImagePart = new System.Drawing.Rectangle(0, 0, 640, 480);
            hWindowControl1.Location = new System.Drawing.Point(0, 0);
            //hWindowControl1.Name = "hWindowControl1";
            hWindowControl1.Size = panelWindow.Size;
            //hWindowControl1.TabIndex = 0;
            hWindowControl1.WindowSize = panelWindow.Size;
            panelWindow.Controls.Add(hWindowControl1);
            hWindowControl1.Anchor = panelWindow.Anchor;
            HOperatorSet.SetWindowAttr("background_color", "black");
            HOperatorSet.OpenWindow(-1, -1, hWindowControl1.Width - 4, hWindowControl1.Height - 4, hWindowControl1.HalconWindow, "", "", out hv_WindowHandle);
            hWindowControl1.SizeChanged += HWindowControl1_SizeChanged;
        }

        private void HWindowControl1_SizeChanged(object sender, EventArgs e)
        {
            HWindowControl hWindowControl1 = (HWindowControl)sender;
            HOperatorSet.SetWindowExtents(hv_WindowHandle, -1, -1, hWindowControl1.Width - 4, hWindowControl1.Height - 4);

            dataGridViewLog.Columns[1].Width = panelWindow.Width - dataGridViewLog.Columns[0].Width - 23;
        }

        private void btn_FindCam_Click(object sender, EventArgs e)
        {
            if (cam.Find(out string[] names, out string[] SNs, out string[] ManufacturerNames, out MV_CC_DEVICE_INFO[] DeviceList))
            {
                comboBox_CamID.Items.Clear();
                ShowMessage("找到" + DeviceList.Length + "个相机");
                comboBox_CamID.Items.AddRange(SNs);
            }
            else
            {
                ShowMessage(cam.ErrMsg, Color.Red);
            }
        }

        private void btn_OpenCam_Click(object sender, EventArgs e)
        {
            if ("" == comboBox_CamID.Text ? cam.Open() : cam.OpenBySN(comboBox_CamID.Text))
            {
                ButtonsEnable(true);
                ShowMessage(cam.Name + "相机打开成功");

            }
            else
            {
                ShowMessage(comboBox_CamID.Text + "相机打开失败:" + cam.ErrMsg, Color.Red);
            }
        }

        Dictionary<string, HObject> ImagesCalib = new Dictionary<string, HObject>();
        Dictionary<string, HObject> ImagesLight = new Dictionary<string, HObject>();
        Dictionary<string, HTuple> Poses = new Dictionary<string, HTuple>();

        Dictionary<string, HTuple> Angles = new Dictionary<string, HTuple>();

        int Image序号 = -1;
        private void btn_OneShot_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;

            if (cam.SetExposure((float)numericUpDown_time.Value))
            {
                cam.SetLine2Inverter(true);

                if (cam.OneShot(out HImage ho_Image))
                {
                    cam.SetLine2Inverter(false);

                    if (cam.SetExposure((float)numericUpDown_lightTime.Value))
                    {
                        cam.SetLine1Inverter(true);
                        if (cam.OneShot(out HImage ho_ImageLight))
                        {
                            cam.SetLine1Inverter(false);
                            if (robot.GetType() != typeof(KukaRobot))
                            {
                                //连的上就保存，连不上，就跳过
                                if (robot.Open(textBoxRobotIP.Text, int.Parse(textBoxRobotPort.Text)))
                                {
                                    if (robot.ReadPose(out HPose hPose) && robot.ReadAngle(out HTuple hAngel))
                                    {
                                        //ShowRobotPose(hPose);
                                        Image序号++;
                                        ImagesCalib.Add(Image序号.ToString(), ho_Image);
                                        ImagesLight.Add(Image序号.ToString(), ho_ImageLight);
                                        Poses.Add(Image序号.ToString(), hPose);
                                        Angles.Add(Image序号.ToString(), hAngel);
                                        listBox图像列表.Items.Add(Image序号.ToString());
                                        listBox图像列表.SelectedIndex = listBox图像列表.Items.Count - 1;
                                    }
                                    else
                                    {
                                        ShowMessage($"机器人坐标获取失败：" + robot.ErrMsg, Color.Red);
                                    }
                                    robot.Close();
                                }
                                else
                                {
                                    ShowMessage($"机器人未连接", Color.Red);
                                }
                            }
                            else
                            {
                                if (robot.ReadPose(out HPose hPose) && robot.ReadAngle(out HTuple hAngel))
                                {
                                    //ShowRobotPose(hPose);
                                    Image序号++;
                                    ImagesCalib.Add(Image序号.ToString(), ho_Image);
                                    ImagesLight.Add(Image序号.ToString(), ho_ImageLight);
                                    Poses.Add(Image序号.ToString(), hPose);
                                    Angles.Add(Image序号.ToString(), hAngel);
                                    listBox图像列表.Items.Add(Image序号.ToString());
                                    listBox图像列表.SelectedIndex = listBox图像列表.Items.Count - 1;
                                }
                                else
                                {
                                    ShowMessage($"机器人坐标获取失败：" + robot.ErrMsg, Color.Red);
                                }
                            }
                        }
                        else
                        {
                            ShowMessage($"图像采集失败：" + cam.ErrMsg, Color.Red);
                        }
                    }
                    else
                    {
                        ShowMessage("曝光设置失败:" + cam.ErrMsg, Color.Red);
                    }
                }
                else
                {
                    ShowMessage($"图像采集失败：" + cam.ErrMsg, Color.Red);
                }
            }
            else
            {
                ShowMessage("曝光设置失败:" + cam.ErrMsg, Color.Red);
            }
        }

        private void btn_KeepShot_Click(object sender, EventArgs e)
        {
            if (btn_KeepShot.Text == "实时显示")
            {
                float exportTime = 0;
                if (checkBox2.Checked)
                {
                    exportTime = (float)numericUpDown_time.Value;
                }
                else if (checkBox1.Checked)
                {
                    exportTime = (float)numericUpDown_lightTime.Value;
                }
                else
                {
                    exportTime = (float)numericUpDown_time.Value;
                }

                if (cam.SetExposure(exportTime))
                {
                    stopWatch.Reset();
                    stopWatch.Start();
                    cam.KeepShot(KeepShot);
                    ShowMessage("实时显示开始");
                    btn_KeepShot.Text = "停止采集";
                    checkBox1.Enabled = false;
                    checkBox2.Enabled = false;

                }
                else
                {
                    ShowMessage("曝光设置失败:" + cam.ErrMsg, Color.Red);
                    checkBox1.Enabled = true;
                    checkBox2.Enabled = true;

                }
            }
            else
            {
                cam.StopGrabbing();
                stopWatch.Stop();
                ShowMessage("相机采集停止,采集时间：" + stopWatch.ElapsedMilliseconds);
                btn_KeepShot.Text = "实时显示";
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;

                //btn_OneShot_Click(null, null);
            }
        }
        HObject lastImage = null;
        void KeepShot(HObject ho_Image)
        {
            lastImage = ho_Image;
            //ShowCalibImage(ho_Image, false);
            ShowImage(ho_Image);
        }
        private void btn_CloseCam_Click(object sender, EventArgs e)
        {
            string name = cam.Name;
            cam.Close();
            ButtonsEnable(false);
            ShowMessage(name + "相机关闭");
        }

        private void btn_SetCamExposure_Click(object sender, EventArgs e)
        {
            if (cam.SetExposure((float)numericUpDown_time.Value))
            {
                ShowMessage("设置曝光：" + numericUpDown_time.Value);
            }
            else
            {
                ShowMessage("曝光设置失败:" + cam.ErrMsg, Color.Red);
            }
        }

        private void btn_GetCamExposure_Click(object sender, EventArgs e)
        {
            if (cam.GetExposure(out float exposure))
            {
                ShowMessage("曝光时间：" + exposure);
                numericUpDown_time.Value = (decimal)exposure;
            }
            else
            {
                ShowMessage("曝光时间获取失败：" + cam.ErrMsg, Color.Red);
            }
        }

        object olock = new object();
        List<string> listLogs = new List<string>();
        void ShowMessage(string message)
        {
            ShowMessage(message, Color.White);
        }
        void ShowMessage(string message, Color backColor)
        {
            DateTime now = DateTime.Now;
            string day = now.ToString("yyyy-MM-dd");
            string time = now.ToString("HH:mm:ss.fff");
            try
            {
                dataGridViewLog.BeginInvoke(new Action(() =>
                {
                    if (dataGridViewLog.Rows.Count >= 200)
                    {
                        dataGridViewLog.Rows.RemoveAt(199);
                    }
                    dataGridViewLog.Rows.Insert(0);
                    dataGridViewLog.Rows[0].Cells[0].Value = day + " " + time;
                    dataGridViewLog.Rows[0].Cells[1].Value = message;
                    dataGridViewLog.Rows[0].DefaultCellStyle.BackColor = backColor;
                }));
            }
            catch { }
            lock (olock)
            {
                try
                {
                    if (!Directory.Exists("RunLog"))
                    {
                        Directory.CreateDirectory("RunLog");
                    }
                    while (listLogs.Count > 0)
                    {
                        using (StreamWriter writer = new StreamWriter("RunLog\\" + day + ".log", true))
                        {
                            writer.WriteLine(listLogs[0]);
                            listLogs.RemoveAt(0);
                        }
                    }
                    using (StreamWriter writer = new StreamWriter("RunLog\\" + day + ".log", true))
                    {
                        writer.WriteLine(time + " " + message);
                    }
                }
                catch (Exception)//写入日志到文件失败，记录信息
                {
                    listLogs.Add(time + " " + message);
                }

                if (backColor == Color.Red)
                {
                    try
                    {
                        File.AppendAllText("Error.log", now.ToString("yyyy-MM-dd HH:mm:ss  ") + message + "\r\n\r\n");
                    }
                    catch { }
                }
            }
        }


        void ShowImage(HObject hObject)
        {
            if (hObject == null)
            {
                HOperatorSet.ClearWindow(hv_WindowHandle);
                return;
            }
            //if (hObject is HImage)
            {
                HOperatorSet.GetImageSize(hObject, out HTuple w, out HTuple h);
                HTuple row, column, width, height;
                HOperatorSet.GetWindowExtents(hv_WindowHandle, out row, out column, out width, out height);

                double wndRatio = width.D / height.D;
                double imgRatio = w.D / h.D;
                int beginRow, beginCol, endRow, endCol;
                if (wndRatio > imgRatio)
                {
                    beginRow = 0;
                    endRow = (int)h.D;
                    beginCol = (int)((w.D - h.D * wndRatio) / 2d);
                    endCol = (int)(w.D - (w.D - h.D * wndRatio) / 2d);
                }
                else
                {
                    beginRow = (int)(-(w.D / wndRatio - h.D) / 2d);
                    endRow = (int)(h.D + (w.D / wndRatio - h.D) / 2d);
                    beginCol = 0;
                    endCol = (int)w.D;
                }

                HOperatorSet.SetPart(hv_WindowHandle, beginRow, beginCol, endRow, endCol);
            }
            HOperatorSet.DispObj(hObject, hv_WindowHandle);
        }
        void ShowHObject(HObject hObject)
        {
            if (hObject == null)
            {
                HOperatorSet.ClearWindow(hv_WindowHandle);
                return;
            }
            HOperatorSet.DispObj(hObject, hv_WindowHandle);
        }

        void ButtonsEnable(bool flag)
        {
            btn_CloseCam.Enabled = flag;
            btn_GetCamExposure.Enabled = flag;
            btn_KeepShot.Enabled = flag;
            btn_OneShot.Enabled = flag;
            btn_SetCamExposure.Enabled = flag;

            comboBox_CamID.Enabled = !flag;
            btn_OpenCam.Enabled = !flag;
        }

        private void comboBox_CamID_TextChanged(object sender, EventArgs e)
        {

        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += InitRobot;
            timer.Start();
        }
        private void InitRobot(object sender, EventArgs e)
        {
            if ((robot.GetType() == typeof(KukaRobot) && radioButton_kuka.Checked) || robot.GetType() == typeof(KawasakiRobot) && radioButton_kawasaki.Checked)
            {

                if (textBoxRobotIP.Text != "" && textBoxRobotPort.Text != "" && !robot.isOpen())
                {
                    robot.Open(textBoxRobotIP.Text, int.Parse(textBoxRobotPort.Text));
                }



            }
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                cam.Close();
                ShowMessage("关闭相机");
            }
            catch { }
        }

        private void FormBaslerCam_Paint(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;//抗锯齿

            GraphicsPath graphicsPath = new GraphicsPath();
            if (control.ClientRectangle.Width > 0 && control.ClientRectangle.Height > 0)
            {
                graphicsPath.AddRectangle(control.ClientRectangle);
                LinearGradientBrush brush = new LinearGradientBrush(control.ClientRectangle, Color.FromArgb(100, 212, 225), Color.FromArgb(100, 162, 225), LinearGradientMode.BackwardDiagonal);
                g.FillPath(brush, graphicsPath);
            }
        }

        private void checkBox手动输入_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox手动输入.Checked)
            {
                textBox像元宽.Enabled = true;
                textBox像元高.Enabled = true;
                textBox焦距.Enabled = true;
            }
            else
            {
                textBox像元宽.Enabled = false;
                textBox像元高.Enabled = false;
                textBox焦距.Enabled = false;
            }
        }
        private void button加载内参_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择要加载的文件";
            openFileDialog.Filter = "CamParam(*.cal)|*.cal";
            if (DialogResult.OK == openFileDialog.ShowDialog())
            {
                try
                {
                    HOperatorSet.ReadCamPar(openFileDialog.FileName, out hv_CameraParameters);
                    ShowCameraParameters();
                }
                catch (Exception ex)
                {
                    ShowMessage($"加载内参{openFileDialog.FileName}失败：" + ex.Message, Color.Red);
                }
            }
            openFileDialog.Dispose();
        }
        private void button保存内参_Click(object sender, EventArgs e)
        {
            if (hv_CameraParameters != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "选择要保存的位置";
                sfd.Filter = "CamParam(*.cal)|*.cal";
                sfd.FileName = "camparam.cal";
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    try
                    {
                        string path = sfd.FileName;
                        HOperatorSet.WriteCamPar(hv_CameraParameters, path);
                        ShowMessage("保存内参成功");
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message, Color.Red);
                    }
                }
                sfd.Dispose();
            }
        }
        private void button浏览描述文件_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择要加载的描述文件";
            openFileDialog.Filter = "描述文件|*.descr";
            if (DialogResult.OK == openFileDialog.ShowDialog())
            {
                textBox描述文件路径.Text = openFileDialog.FileName;
            }
            openFileDialog.Dispose();
        }



        private HTuple GetCalibHandle(HTuple imageW, HTuple imageH)
        {
            HTuple hv_StartParameters = new HTuple();
            if (checkBox手动输入.Checked)
            {
                try
                {
                    //hv_StartParameters[0] = "area_scan_division";
                    hv_StartParameters[0] = double.Parse(textBox焦距.Text) * 0.001;
                    hv_StartParameters[1] = 0;
                    hv_StartParameters[2] = double.Parse(textBox像元宽.Text) * 1e-06;
                    hv_StartParameters[3] = double.Parse(textBox像元高.Text) * 1e-06;
                    hv_StartParameters[4] = imageW / 2;//图像中心x
                    hv_StartParameters[5] = imageH / 2;//图像中心y
                    hv_StartParameters[6] = imageW;//图像宽
                    hv_StartParameters[7] = imageH;//图像高
                    //hv_StartParameters[8] = 
                }
                catch (Exception ex)
                {
                    throw new Exception("内参输入格式错误：" + ex.Message);
                }
            }
            else
            {
                if (hv_CameraParameters == null)
                {
                    throw new Exception("未加载相机内参");
                }
                if (hv_CameraParameters.Length == 8)
                {
                    hv_StartParameters = hv_CameraParameters;
                }
                else if (hv_CameraParameters.Length == 9)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        hv_StartParameters[i] = hv_CameraParameters[i + 1];
                    }
                }
                else
                {
                    throw new Exception("相机内参格式长度异常");
                }
            }

            HOperatorSet.CreateCalibData("hand_eye_moving_cam", 1, 1, out HTuple hv_CalibHandle); //创建一个标定数据对象
            HOperatorSet.SetCalibDataCamParam(hv_CalibHandle, 0, "area_scan_division", hv_StartParameters);
            if (textBox描述文件路径.Text == "")
            {
                throw new Exception("未加载描述文件");
            }
            else
            {
                try
                {
                    HOperatorSet.SetCalibDataCalibObject(hv_CalibHandle, 0, textBox描述文件路径.Text);
                }
                catch (Exception ex)
                {
                    throw new Exception("描述文件异常：" + ex.Message);
                }
            }
            HOperatorSet.SetCalibData(hv_CalibHandle, "model", "general", "optimization_method", "nonlinear");//设置标定所使用的方法
            return hv_CalibHandle;
        }

        private void listBox图像列表_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox图像列表.SelectedIndex != -1)
            {
                string key = (string)listBox图像列表.SelectedItem;
                ShowImage(null);
                if (radioButton_平面图.Checked)
                {
                    ShowImage(ImagesCalib[key]);
                }
                else
                {
                    ShowImage(ImagesLight[key]);
                }
                //ShowRobotPose(Poses[key]);
            }
        }

        private void ShowCalibImage(HObject ho_Image, bool 显示异常信息 = true)
        {
            try
            {
                HTuple hv_TmpCtrl_FindCalObjParNames = new HTuple();
                hv_TmpCtrl_FindCalObjParNames[0] = "gap_tolerance";
                hv_TmpCtrl_FindCalObjParNames[1] = "alpha";
                hv_TmpCtrl_FindCalObjParNames[2] = "skip_find_caltab";
                HTuple hv_TmpCtrl_FindCalObjParValues = new HTuple();
                hv_TmpCtrl_FindCalObjParValues[0] = 1;
                hv_TmpCtrl_FindCalObjParValues[1] = 1;
                hv_TmpCtrl_FindCalObjParValues[2] = "false";

                HOperatorSet.GetImageSize(ho_Image, out HTuple width, out HTuple height);
                HTuple hv_CalibHandle = GetCalibHandle(width, height);

                //HOperatorSet.FindCalibObject(ho_Image, hv_CalibHandle, 0, 0, ImageID, hv_TmpCtrl_FindCalObjParNames, hv_TmpCtrl_FindCalObjParValues);
                HOperatorSet.FindCalibObject(ho_Image, hv_CalibHandle, 0, 0, 0, hv_TmpCtrl_FindCalObjParNames, hv_TmpCtrl_FindCalObjParValues);
                //HOperatorSet.GetCalibDataObservPoints(hv_CalibHandle, 0, 0, ImageID, out HTuple hv_TmpCtrl_MarkRows, out HTuple hv_TmpCtrl_MarkColumns, out HTuple hv_TmpCtrl_Ind, out HTuple hv_CameraPose);
                HOperatorSet.GetCalibDataObservPoints(hv_CalibHandle, 0, 0, 0, out HTuple hv_TmpCtrl_MarkRows, out HTuple hv_TmpCtrl_MarkColumns, out HTuple hv_TmpCtrl_Ind, out HTuple hv_CameraPose);
                HOperatorSet.GenCrossContourXld(out HObject ho_Cross, hv_TmpCtrl_MarkRows, hv_TmpCtrl_MarkColumns, 16, 0.785398);
                int m_index = hv_TmpCtrl_MarkRows.Length / 2;
                int x_index = m_index + (int)Math.Sqrt(hv_TmpCtrl_MarkRows.Length) / 2;
                int y_index = hv_TmpCtrl_MarkRows.Length - (int)Math.Sqrt(hv_TmpCtrl_MarkRows.Length) / 2 - 1;
                HOperatorSet.GenContourPolygonXld(out HObject xline, new HTuple(hv_TmpCtrl_MarkRows[m_index].D, hv_TmpCtrl_MarkRows[x_index].D), new HTuple(hv_TmpCtrl_MarkColumns[m_index].D, hv_TmpCtrl_MarkColumns[x_index].D));
                HOperatorSet.GenContourPolygonXld(out HObject yline, new HTuple(hv_TmpCtrl_MarkRows[m_index].D, hv_TmpCtrl_MarkRows[y_index].D), new HTuple(hv_TmpCtrl_MarkColumns[m_index].D, hv_TmpCtrl_MarkColumns[y_index].D));

                ShowImage(ho_Image);
                HOperatorSet.SetColored(hv_WindowHandle, 12);
                ShowHObject(ho_Cross);
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                ShowHObject(xline);
                HOperatorSet.SetTposition(hv_WindowHandle, hv_TmpCtrl_MarkRows[x_index], hv_TmpCtrl_MarkColumns[x_index]);
                HOperatorSet.WriteString(hv_WindowHandle, "X");
                HOperatorSet.SetColor(hv_WindowHandle, "green");
                ShowHObject(yline);
                HOperatorSet.SetTposition(hv_WindowHandle, hv_TmpCtrl_MarkRows[y_index], hv_TmpCtrl_MarkColumns[y_index]);
                HOperatorSet.WriteString(hv_WindowHandle, "Y");
            }
            catch (Exception ex)
            {
                ShowImage(ho_Image);
                if (显示异常信息)
                {
                    ShowMessage(ex.Message, Color.Red);
                }
            }
        }

        private void button移除_Click(object sender, EventArgs e)
        {
            if (listBox图像列表.SelectedIndex != -1)
            {
                string key = (string)listBox图像列表.SelectedItem;
                ImagesCalib.Remove(key);
                ImagesLight.Remove(key);
                Poses.Remove(key);
                Angles.Remove(key);

                listBox图像列表.Items.Remove(listBox图像列表.SelectedItem);
            }
        }

        private void button移除全部_Click(object sender, EventArgs e)
        {
            ImagesCalib.Clear();
            ImagesLight.Clear();
            Poses.Clear();
            Angles.Clear();
            listBox图像列表.Items.Clear();
            Image序号 = -1;
        }
        private void button加载图像_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "请选择要加载的图片";
            openFileDialog.Filter = "所有文件|*.*";
            if (DialogResult.OK == openFileDialog.ShowDialog())
            {
                foreach (var imagePath in openFileDialog.FileNames)
                {
                    try
                    {
                        int index = imagePath.LastIndexOf("_0.");
                        string lightPath = imagePath.Substring(0, index) + "_1" + imagePath.Substring(index + 2);
                        string posePath = imagePath.Substring(0, index) + "_robotPose.dat";
                        string anglePath = imagePath.Substring(0, index) + "_robotAngle.dat";

                        if (File.Exists(posePath))
                        {
                            HOperatorSet.ReadImage(out HObject ho_Image, imagePath);
                            HOperatorSet.ReadPose(posePath, out HTuple pose);
                            HOperatorSet.ReadTuple(anglePath, out HTuple angle);
                            HOperatorSet.ReadImage(out HObject ho_ImageLight, lightPath);
                            string key = Path.GetFileNameWithoutExtension(imagePath);
                            ImagesCalib.Add(key, ho_Image);
                            ImagesLight.Add(key, ho_ImageLight);
                            Poses.Add(key, pose);
                            Angles.Add(key, angle);

                            listBox图像列表.Items.Add(key);
                            listBox图像列表.SelectedIndex = listBox图像列表.Items.Count - 1;
                            Image序号++;
                        }
                        else
                        {
                            ShowMessage($"对应位姿{posePath}文件不存在", Color.Red);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessage($"加载{imagePath}失败：" + ex.Message, Color.Red);
                    }
                }
            }
            openFileDialog.Dispose();
        }
        private void button保存图像_Click(object sender, EventArgs e)
        {
            //获取内参
            if (hv_CameraParameters == null)
            {
                ShowMessage($"内参为空，请加载文件", Color.Red);
                return;
            }
            //获取光平面
            if (hv_LightInCam == null)
            {
                ShowMessage($"光平面外参为空，请加载文件", Color.Red);
                return;
            }
            //获取手眼标定
            if (hv_ToolInCamPose == null)
            {
                ShowMessage($"手眼标定外参为空，请加载文件", Color.Red);
                return;
            }

            //读取内参
            HTuple hv_CamParamOut;
            HOperatorSet.ChangeRadialDistortionCamPar("adaptive", hv_CameraParameters, 0, out hv_CamParamOut);
            HObject ho_Map;
            HOperatorSet.GenRadialDistortionMap(out ho_Map, hv_CameraParameters, hv_CamParamOut, "bilinear");
            //读取光面标定参数
            HTuple hv_LightToCam;
            HOperatorSet.PoseToHomMat3d(hv_LightInCam, out hv_LightToCam);
            //读取手眼标定参数
            HTuple hv_CamToTool;
            HOperatorSet.PoseToHomMat3d(hv_ToolInCamPose, out hv_CamToTool);
            HTuple camMat;
            HOperatorSet.CamParToCamMat(hv_CamParamOut, out camMat, out HTuple imageWidth, out HTuple imageHeight);


            if (listBox图像列表.SelectedIndex != -1)
            {

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "选择要保存的位置";
                sfd.Filter = "所有文件(*.*)|*.*";
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    string Directory = Path.GetDirectoryName(sfd.FileName);
                    string FileName = Path.GetFileNameWithoutExtension(sfd.FileName);
                    string format = Path.GetExtension(sfd.FileName);
                    string imagePath = $"{Directory}\\{FileName}_0.png";
                    string lightPath = $"{Directory}\\{FileName}_1.png";

                    //保存路径
                    string imageTransformPath = $"{Directory}\\{FileName}_0_transform.png";
                    string lightTransformPath = $"{Directory}\\{FileName}_1_transform.png";
                    string worldMapPath = $"{Directory}\\{FileName}_2_transform.tiff";

                    string lightMapPath = $"{Directory}\\{FileName}_3_transform.tiff";


                    string InternalReferencePath = $"{Directory}\\{FileName}_Internal_Reference.dat";

                    string posePath = $"{Directory}\\{FileName}_robotPose.dat";
                    string anglePath = $"{Directory}\\{FileName}_robotAngle.dat";


                    HObject ho_ImageMapped, ho_LightMapped;
                    HObject worldImageMapped;
                    HObject lightImageMapped;


                    HTuple hv_Width = new HTuple();
                    HTuple hv_Height = new HTuple();

                    string key = (string)listBox图像列表.SelectedItem;

                    hv_Width.Dispose(); hv_Height.Dispose();
                    HOperatorSet.GetImageSize(ImagesCalib[key], out hv_Width, out hv_Height);





                    TransformImageLight( ho_Map, hv_LightToCam, hv_Width, hv_Height, out lightImageMapped);
                    TransformImageWorld(ImagesCalib[key], ImagesLight[key], ho_Map, hv_LightToCam, hv_CamToTool, out ho_ImageMapped, out ho_LightMapped, out worldImageMapped);


                    HOperatorSet.WriteImage(ImagesCalib[key], "png", 0, imagePath);
                    HOperatorSet.WriteImage(ImagesLight[key], "png", 0, lightPath);
                    HOperatorSet.WriteImage(ho_ImageMapped, "png", 0, imageTransformPath);
                    HOperatorSet.WriteImage(ho_LightMapped, "png", 0, lightTransformPath);
                    HOperatorSet.WriteImage(worldImageMapped, "tiff", 0, worldMapPath);

                    HOperatorSet.WriteImage(lightImageMapped, "tiff", 0, lightMapPath);


                    HOperatorSet.WritePose(Poses[key], posePath);
                    HOperatorSet.WriteTuple(Angles[key], anglePath);

                    HOperatorSet.WriteTuple(camMat, InternalReferencePath);

                }
                sfd.Dispose();


            }
        }
        private void button保存全部图像_Click(object sender, EventArgs e)
        {
            //获取内参
            if (hv_CameraParameters == null)
            {
                ShowMessage($"内参为空，请加载文件", Color.Red);
                return;
            }
            //获取光平面
            if (hv_LightInCam == null)
            {
                ShowMessage($"光平面外参为空，请加载文件", Color.Red);
                return;
            }
            //获取手眼标定
            if (hv_ToolInCamPose == null)
            {
                ShowMessage($"手眼标定外参为空，请加载文件", Color.Red);
                return;
            }

            //读取内参
            HTuple hv_CamParamOut;
            HOperatorSet.ChangeRadialDistortionCamPar("adaptive", hv_CameraParameters, 0, out hv_CamParamOut);
            HObject ho_Map;
            HOperatorSet.GenRadialDistortionMap(out ho_Map, hv_CameraParameters, hv_CamParamOut, "bilinear");
            //读取光面标定参数
            HTuple hv_LightToCam;
            HOperatorSet.PoseToHomMat3d(hv_LightInCam, out hv_LightToCam);
            //读取手眼标定参数
            HTuple hv_CamToTool;
            HOperatorSet.PoseToHomMat3d(hv_ToolInCamPose, out hv_CamToTool);
            HTuple camMat;
            HOperatorSet.CamParToCamMat(hv_CamParamOut, out camMat, out HTuple imageWidth, out HTuple imageHeight);


            if (ImagesCalib.Count > 0)
            {
                FolderBrowserDialog folder = new FolderBrowserDialog();
                folder.Description = "选择要保存的文件夹位置";
                if (DialogResult.OK == folder.ShowDialog())
                {
                    string InternalReferencePath = $"{folder.SelectedPath}\\Internal_Reference_Map.dat";
                    HOperatorSet.WriteTuple(camMat, InternalReferencePath);

                    foreach (var name in ImagesCalib.Keys)
                    {
                        try
                        {
                            string imagePath = $"{folder.SelectedPath}\\{name}_0.png";
                            string lightPath = $"{folder.SelectedPath}\\{name}_1.png";

                            //保存路径
                            string imageTransformPath = $"{folder.SelectedPath}\\{name}_0_transform.png";
                            string lightTransformPath = $"{folder.SelectedPath}\\{name}_1_transform.png";
                            string worldMapPath = $"{folder.SelectedPath}\\{name}_2_transform.tiff";
                            string lightMapPath = $"{folder.SelectedPath}\\{name}_3_transform.tiff";

                            string posePath = $"{folder.SelectedPath}\\{name}_robotPose.dat";
                            string anglePath = $"{folder.SelectedPath}\\{name}_robotAngle.dat";

                            HObject ho_ImageMapped, ho_LightMapped;
                            HObject worldImageMapped;
                            HObject lightImageMapped;

                            HTuple hv_Width = new HTuple();
                            HTuple hv_Height = new HTuple();

                            hv_Width.Dispose(); hv_Height.Dispose();
                            HOperatorSet.GetImageSize(ImagesCalib[name], out hv_Width, out hv_Height);


                            TransformImageWorld(ImagesCalib[name], ImagesLight[name], ho_Map, hv_LightToCam, hv_CamToTool, out ho_ImageMapped, out ho_LightMapped, out worldImageMapped);

                            TransformImageLight(ho_Map, hv_LightToCam, hv_Width, hv_Height, out lightImageMapped);


                            HOperatorSet.WriteImage(ImagesCalib[name], "png", 0, imagePath);
                            HOperatorSet.WriteImage(ImagesLight[name], "png", 0, lightPath);
                            HOperatorSet.WriteImage(ho_ImageMapped, "png", 0, imageTransformPath);
                            HOperatorSet.WriteImage(ho_LightMapped, "png", 0, lightTransformPath);
                            HOperatorSet.WriteImage(worldImageMapped, "tiff", 0, worldMapPath);

                            HOperatorSet.WriteImage(lightImageMapped, "tiff", 0, lightMapPath);

                            HOperatorSet.WritePose(Poses[name], posePath);
                            HOperatorSet.WriteTuple(Angles[name], anglePath);

                        }
                        catch (Exception ex)
                        {
                            ShowMessage(ex.Message, Color.Red);
                        }
                    }
                    ShowMessage("全部图片保存完成");
                }
            }
        }

        HTuple hv_CameraParameters = null;
        HTuple hv_LightInCam = null;
        HTuple hv_ToolInCamPose = null;



        private void ShowCameraParameters()
        {
            if (hv_CameraParameters != null)
            {
                if (hv_CameraParameters.Length == 8)
                {
                    //ShowMessage($"像元宽:{hv_CameraParameters[2].D * 1e06},像元高:{hv_CameraParameters[3].D * 1e06},焦距:{hv_CameraParameters[0].D * 1000:0.0000},Kappa:{hv_CameraParameters[1].D:0.0000}," +
                    //    $"Cx:{hv_CameraParameters[4].D:0.00},Cy:{hv_CameraParameters[5].D:0.00},图像宽:{hv_CameraParameters[6].D},图像高:{hv_CameraParameters[7].D},");

                    textBox像元宽.Text = $"{hv_CameraParameters[2].D * 1e06:G6}";
                    textBox像元高.Text = $"{hv_CameraParameters[3].D * 1e06:G6}";
                    textBox焦距.Text = $"{hv_CameraParameters[0].D * 1000:G6}";
                    textBoxKappa.Text = $"{hv_CameraParameters[1].D:G6}";
                    textBox中心x.Text = $"{hv_CameraParameters[4].D:G6}";
                    textBox中心y.Text = $"{hv_CameraParameters[5].D:G6}";
                    textBox图像宽.Text = $"{hv_CameraParameters[6].D}";
                    textBox图像高.Text = $"{hv_CameraParameters[7].D}";
                }
                else if (hv_CameraParameters.Length == 9)
                {
                    //ShowMessage($"像元宽:{hv_CameraParameters[3].D * 1e06},像元高:{hv_CameraParameters[4].D * 1e06},焦距:{hv_CameraParameters[1].D * 1000:0.0000},Kappa:{hv_CameraParameters[2].D:0.0000}," +
                    //    $"Cx:{hv_CameraParameters[5].D:0.00},Cy:{hv_CameraParameters[6].D:0.00},图像宽:{hv_CameraParameters[7].D},图像高:{hv_CameraParameters[8].D},");

                    textBox像元宽.Text = $"{hv_CameraParameters[3].D * 1e06:G6}";
                    textBox像元高.Text = $"{hv_CameraParameters[4].D * 1e06:G6}";
                    textBox焦距.Text = $"{hv_CameraParameters[1].D * 1000:G6}";
                    textBoxKappa.Text = $"{hv_CameraParameters[2].D:G6}";
                    textBox中心x.Text = $"{hv_CameraParameters[5].D:G6}";
                    textBox中心y.Text = $"{hv_CameraParameters[6].D:G6}";
                    textBox图像宽.Text = $"{hv_CameraParameters[7].D}";
                    textBox图像高.Text = $"{hv_CameraParameters[8].D}";
                }
            }
        }
        private void ClearCameraParameters()
        {
            textBox像元宽.Text = "";
            textBox像元高.Text = "";
            textBox焦距.Text = "";
            textBoxKappa.Text = "";
            textBox中心x.Text = "";
            textBox中心y.Text = "";
            textBox图像宽.Text = "";
            textBox图像高.Text = "";
        }

        private void button手眼标定_Click(object sender, EventArgs e)
        {
            if (ImagesCalib.Count > 0)
            {
                try
                {
                    HTuple hv_TmpCtrl_FindCalObjParNames = new HTuple();
                    hv_TmpCtrl_FindCalObjParNames[0] = "gap_tolerance";
                    hv_TmpCtrl_FindCalObjParNames[1] = "alpha";
                    hv_TmpCtrl_FindCalObjParNames[2] = "skip_find_caltab";
                    HTuple hv_TmpCtrl_FindCalObjParValues = new HTuple();
                    hv_TmpCtrl_FindCalObjParValues[0] = 1;
                    hv_TmpCtrl_FindCalObjParValues[1] = 1;
                    hv_TmpCtrl_FindCalObjParValues[2] = "false";

                    HTuple hv_CalibHandle = null;
                    int id = 0;
                    foreach (string item in ImagesCalib.Keys)
                    {
                        if (hv_CalibHandle == null)
                        {
                            HOperatorSet.GetImageSize(ImagesCalib[item], out HTuple width, out HTuple height);
                            hv_CalibHandle = GetCalibHandle(width, height);
                        }
                        try
                        {
                            HOperatorSet.FindCalibObject(ImagesCalib[item], hv_CalibHandle, 0, 0, id, hv_TmpCtrl_FindCalObjParNames, hv_TmpCtrl_FindCalObjParValues);
                            HOperatorSet.SetCalibData(hv_CalibHandle, "tool", id, "tool_in_base_pose", Poses[item]);
                            id++;
                        }
                        catch (Exception ex)
                        {
                            ShowMessage($"图片\"{item}\"识别失败：" + ex.Message, Color.Red);
                        }
                    }
                    //检查用于进行标定的位姿矩阵是否具有一致性
                    check_hand_eye_calibration_input_poses(hv_CalibHandle, 0.05, 0.005, out HTuple hv_Warnings);
                    if (hv_Warnings.Length != 0)
                    {
                        ShowMessage($"位姿矩阵一致性不达标：" + hv_Warnings, Color.Red);
                    }
                    //进行手眼标定
                    HOperatorSet.CalibrateHandEye(hv_CalibHandle, out HTuple hv_Errors);
                    //获取相机标定的误差
                    HOperatorSet.GetCalibData(hv_CalibHandle, "model", "general", "camera_calib_error", out HTuple hv_CamCalibError);
                    //获取相机的参数
                    HOperatorSet.GetCalibData(hv_CalibHandle, "camera", 0, "params", out HTuple hv_CamParam);
                    //获取机械手末端夹具在相机坐标系下的坐标
                    HOperatorSet.GetCalibData(hv_CalibHandle, "camera", 0, "tool_in_cam_pose", out HTuple hv_ToolInCamPose);
                    //计算标定板在基座坐标系下的坐标
                    HOperatorSet.GetCalibData(hv_CalibHandle, "calib_obj", 0, "obj_in_base_pose", out HTuple hv_CalObjInBasePose);

                    //相机在机械手末端夹具坐标系下的坐标
                    HOperatorSet.PoseInvert(hv_ToolInCamPose, out HTuple hv_CamInToolPose);

                    ShowMessage($"相机标定误差：{hv_CamCalibError}像素");
                    ShowMessage($"手眼标定误差：RMS({hv_Errors[0].D * 1000:G6}mm，{hv_Errors[1].D:G6}°),最大({hv_Errors[2].D * 1000:G6}mm，{hv_Errors[3].D:G6}°)");
                    ShowMessage($"相机参数：" + hv_CamParam.ToString());
                    ShowMessage($"机械手末端夹具在相机坐标系下的坐标：" + hv_ToolInCamPose.ToString());
                    ShowMessage($"相机在机械手末端夹具坐标系下的坐标：" + hv_CamInToolPose.ToString());
                    ShowMessage($"标定板在基座坐标系下的坐标：" + hv_CalObjInBasePose.ToString());
                    //显示数据
                    ShowCamPose(hv_ToolInCamPose);
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message, Color.Red);
                }
            }
        }

        // Procedures 
        // Chapter: Calibration / Hand-Eye
        // Short Description: Check the input poses of the hand-eye calibration for consistency. 
        public void check_hand_eye_calibration_input_poses(HTuple hv_CalibDataID, HTuple hv_RotationTolerance,
            HTuple hv_TranslationTolerance, out HTuple hv_Warnings)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_MinLargeRotationFraction = new HTuple();
            HTuple hv_MinLargeAnglesFraction = new HTuple(), hv_StdDevFactor = new HTuple();
            HTuple hv_Type = new HTuple(), hv_Exception = new HTuple();
            HTuple hv_IsHandEyeScara = new HTuple(), hv_IsHandEyeArticulated = new HTuple();
            HTuple hv_NumCameras = new HTuple(), hv_NumCalibObjs = new HTuple();
            HTuple hv_I1 = new HTuple(), hv_PosesIdx = new HTuple();
            HTuple hv_RefCalibDataID = new HTuple(), hv_UseTemporaryCopy = new HTuple();
            HTuple hv_CamPoseCal = new HTuple(), hv_SerializedItemHandle = new HTuple();
            HTuple hv_TmpCalibDataID = new HTuple(), hv_Error = new HTuple();
            HTuple hv_Index = new HTuple(), hv_CamDualQuatCal = new HTuple();
            HTuple hv_BasePoseTool = new HTuple(), hv_BaseDualQuatTool = new HTuple();
            HTuple hv_NumCalibrationPoses = new HTuple(), hv_LX2s = new HTuple();
            HTuple hv_LY2s = new HTuple(), hv_LZ2s = new HTuple();
            HTuple hv_TranslationToleranceSquared = new HTuple(), hv_RotationToleranceSquared = new HTuple();
            HTuple hv_Index1 = new HTuple(), hv_CamDualQuatCal1 = new HTuple();
            HTuple hv_Cal1DualQuatCam = new HTuple(), hv_BaseDualQuatTool1 = new HTuple();
            HTuple hv_Tool1DualQuatBase = new HTuple(), hv_Index2 = new HTuple();
            HTuple hv_CamDualQuatCal2 = new HTuple(), hv_DualQuat1 = new HTuple();
            HTuple hv_BaseDualQuatTool2 = new HTuple(), hv_DualQuat2 = new HTuple();
            HTuple hv_LX1 = new HTuple(), hv_LY1 = new HTuple(), hv_LZ1 = new HTuple();
            HTuple hv_MX1 = new HTuple(), hv_MY1 = new HTuple(), hv_MZ1 = new HTuple();
            HTuple hv_Rot1 = new HTuple(), hv_Trans1 = new HTuple();
            HTuple hv_LX2 = new HTuple(), hv_LY2 = new HTuple(), hv_LZ2 = new HTuple();
            HTuple hv_MX2 = new HTuple(), hv_MY2 = new HTuple(), hv_MZ2 = new HTuple();
            HTuple hv_Rot2 = new HTuple(), hv_Trans2 = new HTuple();
            HTuple hv_MeanRot = new HTuple(), hv_MeanTrans = new HTuple();
            HTuple hv_SinTheta2 = new HTuple(), hv_CosTheta2 = new HTuple();
            HTuple hv_SinTheta2Squared = new HTuple(), hv_CosTheta2Squared = new HTuple();
            HTuple hv_ErrorRot = new HTuple(), hv_StdDevQ0 = new HTuple();
            HTuple hv_ToleranceDualQuat0 = new HTuple(), hv_ErrorDualQuat0 = new HTuple();
            HTuple hv_StdDevQ4 = new HTuple(), hv_ToleranceDualQuat4 = new HTuple();
            HTuple hv_ErrorDualQuat4 = new HTuple(), hv_Message = new HTuple();
            HTuple hv_NumPairs = new HTuple(), hv_NumPairsMax = new HTuple();
            HTuple hv_LargeRotationFraction = new HTuple(), hv_NumPairPairs = new HTuple();
            HTuple hv_NumPairPairsMax = new HTuple(), hv_Angles = new HTuple();
            HTuple hv_Idx = new HTuple(), hv_LXA = new HTuple(), hv_LYA = new HTuple();
            HTuple hv_LZA = new HTuple(), hv_LXB = new HTuple(), hv_LYB = new HTuple();
            HTuple hv_LZB = new HTuple(), hv_ScalarProduct = new HTuple();
            HTuple hv_LargeAngles = new HTuple(), hv_LargeAnglesFraction = new HTuple();

            HTupleVector hvec_CamDualQuatsCal = new HTupleVector(1);
            HTupleVector hvec_BaseDualQuatsTool = new HTupleVector(1);
            // Initialize local and output iconic variables 
            hv_Warnings = new HTuple();
            try
            {
                //This procedure checks the hand-eye calibration input poses that are stored in
                //the calibration data model CalibDataID for consistency.
                //
                //For this check, it is necessary to know the accuracy of the input poses.
                //Therefore, the RotationTolerance and TranslationTolerance must be
                //specified that approximately describe the error in the rotation and in the
                //translation part of the input poses, respectively. The rotation tolerance must
                //be passed in RotationTolerance in radians. The translation tolerance must be
                //passed in TranslationTolerance in the same unit in which the input poses were
                //given, i.e., typically in meters. Therefore, the more accurate the
                //input poses are, the lower the values for RotationTolerance and
                //TranslationTolerance should be chosen. If the accuracy of the robot's tool
                //poses is different from the accuracy of the calibration object poses, the
                //tolerance values of the poses with the lower accuracy (i.e., the higher
                //tolerance values) should be passed.
                //
                //Typically, check_hand_eye_calibration_input_poses is called after all
                //calibration poses have been set in the calibration data model and before the
                //hand eye calibration is performed. The procedure checks all pairs of robot
                //tool poses and compares them to the corresponding pair of calibration object
                //poses. For each inconsistent pose pair, a string is returned in Warnings that
                //indicates the inconsistent pose pair. For larger values for RotationTolerance
                //or TranslationTolerance, i.e., for less accurate input poses, fewer warnings
                //will be generated because the check is more tolerant, and vice versa. The
                //procedure is also helpful if the errors that are returned by the hand-eye
                //calibration are larger than expected to identify potentially erroneous poses.
                //Note that it is not possible to check the consistency of a single pose but
                //only of pose pairs. Nevertheless, if a certain pose occurs multiple times in
                //different warning messages, it is likely that the pose is erroneous.
                //Erroneous poses that result in inconsistent pose pairs should removed
                //from the calibration data model by using remove_calib_data_observ and
                //remove_calib_data before performing the hand-eye calibration.
                //
                //check_hand_eye_calibration_input_poses also checks whether enough calibration
                //pose pairs are passed with a significant relative rotation angle, which
                //is necessary for a robust hand-eye calibration.
                //
                //check_hand_eye_calibration_input_poses also verifies that the correct
                //calibration model was chosen in create_calib_data. If a model of type
                //'hand_eye_stationary_cam' or 'hand_eye_moving_cam' was chosen, the calibration
                //of an articulated robot is assumed. For 'hand_eye_scara_stationary_cam' or
                //'hand_eye_scara_moving_cam', the calibration of a SCARA robot is assumed.
                //Therefore, if all input poses for an articulated robot are parallel or if some
                //robot poses for a SCARA robot are tilted, a corresponding message is returned
                //in Warnings. Furthermore, if the number of tilted input poses for articulated
                //robots is below a certain value, a corresponding message in Warnings indicates
                //that the accuracy of the result of the hand-eye calibration might be low.
                //
                //If no problems have been detected in the input poses, an empty tuple is
                //returned in Warnings.
                //
                //
                //Define the minimum fraction of pose pairs with a rotation angle exceeding
                //2*RotationTolerance.
                hv_MinLargeRotationFraction.Dispose();
                hv_MinLargeRotationFraction = 0.1;
                //Define the minimum fraction of screw axes pairs with an angle exceeding
                //2*RotationTolerance for articulated robots.
                hv_MinLargeAnglesFraction.Dispose();
                hv_MinLargeAnglesFraction = 0.1;
                //Factor that is used to multiply the standard deviations to obtain an error
                //threshold.
                hv_StdDevFactor.Dispose();
                hv_StdDevFactor = 3.0;
                //
                //Check input control parameters.
                if ((int)(new HTuple((new HTuple(hv_CalibDataID.TupleLength())).TupleNotEqual(
                    1))) != 0)
                {
                    throw new HalconException("Wrong number of values of control parameter: 1");
                }
                if ((int)(new HTuple((new HTuple(hv_RotationTolerance.TupleLength())).TupleNotEqual(
                    1))) != 0)
                {
                    throw new HalconException("Wrong number of values of control parameter: 2");
                }
                if ((int)(new HTuple((new HTuple(hv_TranslationTolerance.TupleLength())).TupleNotEqual(
                    1))) != 0)
                {
                    throw new HalconException("Wrong number of values of control parameter: 3");
                }
                try
                {
                    hv_Type.Dispose();
                    HOperatorSet.GetCalibData(hv_CalibDataID, "model", "general", "type", out hv_Type);
                }
                // catch (Exception) 
                catch (HalconException HDevExpDefaultException1)
                {
                    HDevExpDefaultException1.ToHTuple(out hv_Exception);
                    throw new HalconException("Wrong value of control parameter: 1");
                }
                if ((int)(new HTuple(hv_RotationTolerance.TupleLess(0))) != 0)
                {
                    throw new HalconException("Wrong value of control parameter: 2");
                }
                if ((int)(new HTuple(hv_TranslationTolerance.TupleLess(0))) != 0)
                {
                    throw new HalconException("Wrong value of control parameter: 3");
                }
                //
                //Read out the calibration data model.
                hv_IsHandEyeScara.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_IsHandEyeScara = (new HTuple(hv_Type.TupleEqual(
                        "hand_eye_scara_stationary_cam"))).TupleOr(new HTuple(hv_Type.TupleEqual(
                        "hand_eye_scara_moving_cam")));
                }
                hv_IsHandEyeArticulated.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_IsHandEyeArticulated = (new HTuple(hv_Type.TupleEqual(
                        "hand_eye_stationary_cam"))).TupleOr(new HTuple(hv_Type.TupleEqual("hand_eye_moving_cam")));
                }
                //This procedure only works for hand-eye calibration applications.
                if ((int)((new HTuple(hv_IsHandEyeScara.TupleNot())).TupleAnd(hv_IsHandEyeArticulated.TupleNot()
                    )) != 0)
                {
                    throw new HalconException("check_hand_eye_calibration_input_poses only works for hand-eye calibrations");
                }
                hv_NumCameras.Dispose();
                HOperatorSet.GetCalibData(hv_CalibDataID, "model", "general", "num_cameras",
                    out hv_NumCameras);
                hv_NumCalibObjs.Dispose();
                HOperatorSet.GetCalibData(hv_CalibDataID, "model", "general", "num_calib_objs",
                    out hv_NumCalibObjs);
                //
                //Get all valid calibration pose indices.
                hv_I1.Dispose(); hv_PosesIdx.Dispose();
                HOperatorSet.QueryCalibDataObservIndices(hv_CalibDataID, "camera", 0, out hv_I1,
                    out hv_PosesIdx);
                hv_RefCalibDataID.Dispose();
                hv_RefCalibDataID = new HTuple(hv_CalibDataID);
                hv_UseTemporaryCopy.Dispose();
                hv_UseTemporaryCopy = 0;
                //If necessary, calibrate the interior camera parameters.
                if ((int)(hv_IsHandEyeArticulated) != 0)
                {
                    //For articulated (non-SCARA) robots, we have to check whether the camera
                    //is already calibrated. Otherwise, the queried poses might not be very
                    //accurate.
                    try
                    {
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_CamPoseCal.Dispose();
                            HOperatorSet.GetCalibData(hv_CalibDataID, "calib_obj_pose", (new HTuple(0)).TupleConcat(
                                hv_PosesIdx.TupleSelect(0)), "pose", out hv_CamPoseCal);
                        }
                    }
                    // catch (Exception) 
                    catch (HalconException HDevExpDefaultException1)
                    {
                        HDevExpDefaultException1.ToHTuple(out hv_Exception);
                        if ((int)((new HTuple(hv_NumCameras.TupleNotEqual(0))).TupleAnd(new HTuple(hv_NumCalibObjs.TupleNotEqual(
                            0)))) != 0)
                        {
                            //If the interior camera parameters are not calibrated yet, perform
                            //the camera calibration by using a temporary copy of the calibration
                            //data model.
                            hv_SerializedItemHandle.Dispose();
                            HOperatorSet.SerializeCalibData(hv_CalibDataID, out hv_SerializedItemHandle);
                            hv_TmpCalibDataID.Dispose();
                            HOperatorSet.DeserializeCalibData(hv_SerializedItemHandle, out hv_TmpCalibDataID);
                            HOperatorSet.ClearSerializedItem(hv_SerializedItemHandle);
                            hv_RefCalibDataID.Dispose();
                            hv_RefCalibDataID = new HTuple(hv_TmpCalibDataID);
                            hv_UseTemporaryCopy.Dispose();
                            hv_UseTemporaryCopy = 1;
                            hv_Error.Dispose();
                            HOperatorSet.CalibrateCameras(hv_TmpCalibDataID, out hv_Error);
                        }
                    }
                }
                //Query all robot tool and calibration object poses.
                for (hv_Index = 0; (int)hv_Index <= (int)((new HTuple(hv_PosesIdx.TupleLength()
                    )) - 1); hv_Index = (int)hv_Index + 1)
                {
                    try
                    {
                        //For an articulated robot with a camera and a calibration object,
                        //a calibrated poses should always be available.
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_CamPoseCal.Dispose();
                            HOperatorSet.GetCalibData(hv_RefCalibDataID, "calib_obj_pose", (new HTuple(0)).TupleConcat(
                                hv_PosesIdx.TupleSelect(hv_Index)), "pose", out hv_CamPoseCal);
                        }
                    }
                    // catch (Exception) 
                    catch (HalconException HDevExpDefaultException1)
                    {
                        HDevExpDefaultException1.ToHTuple(out hv_Exception);
                        //For a SCARA robot or for an articulated robots with a general
                        //sensor and no calibration object, directly use the observed poses.
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_CamPoseCal.Dispose();
                            HOperatorSet.GetCalibDataObservPose(hv_RefCalibDataID, 0, 0, hv_PosesIdx.TupleSelect(
                                hv_Index), out hv_CamPoseCal);
                        }
                    }
                    //Transform the calibration object poses to dual quaternions.
                    hv_CamDualQuatCal.Dispose();
                    HOperatorSet.PoseToDualQuat(hv_CamPoseCal, out hv_CamDualQuatCal);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hvec_CamDualQuatsCal[hv_Index] = dh.Add(new HTupleVector(hv_CamDualQuatCal));
                    }
                    //Transform the robot tool pose to dual quaternions.
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_BasePoseTool.Dispose();
                        HOperatorSet.GetCalibData(hv_RefCalibDataID, "tool", hv_PosesIdx.TupleSelect(
                            hv_Index), "tool_in_base_pose", out hv_BasePoseTool);
                    }
                    hv_BaseDualQuatTool.Dispose();
                    HOperatorSet.PoseToDualQuat(hv_BasePoseTool, out hv_BaseDualQuatTool);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hvec_BaseDualQuatsTool[hv_Index] = dh.Add(new HTupleVector(hv_BaseDualQuatTool));
                    }
                }
                hv_NumCalibrationPoses.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumCalibrationPoses = new HTuple(hv_PosesIdx.TupleLength()
                        );
                }
                if ((int)(hv_UseTemporaryCopy) != 0)
                {
                    HOperatorSet.ClearCalibData(hv_TmpCalibDataID);
                }
                //
                //In the first test, check the poses for consistency. The principle of
                //the hand-eye calibration is that the movement of the robot from time
                //i to time j is represented by the relative pose of the calibration
                //object from i to j in the camera coordinate system and also by the
                //relative pose of the robot tool from i to j in the robot base
                //coordinate system. Because both relative poses represent the same 3D
                //rigid transformation, but only seen from two different coordinate
                //systems, their screw axes differ but their screw angle and their
                //screw translation should be identical. This knowledge can be used to
                //check the consistency of the input poses. Furthermore, remember the
                //screw axes for all robot movements to later check whether the
                //correct calibration model (SCARA or articulated) was selected by the
                //user.
                hv_Warnings.Dispose();
                hv_Warnings = new HTuple();
                hv_LX2s.Dispose();
                hv_LX2s = new HTuple();
                hv_LY2s.Dispose();
                hv_LY2s = new HTuple();
                hv_LZ2s.Dispose();
                hv_LZ2s = new HTuple();
                hv_TranslationToleranceSquared.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_TranslationToleranceSquared = hv_TranslationTolerance * hv_TranslationTolerance;
                }
                hv_RotationToleranceSquared.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_RotationToleranceSquared = hv_RotationTolerance * hv_RotationTolerance;
                }
                HTuple end_val162 = hv_NumCalibrationPoses - 2;
                HTuple step_val162 = 1;
                for (hv_Index1 = 0; hv_Index1.Continue(end_val162, step_val162); hv_Index1 = hv_Index1.TupleAdd(step_val162))
                {
                    hv_CamDualQuatCal1.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_CamDualQuatCal1 = new HTuple(hvec_CamDualQuatsCal[hv_Index1].T);
                    }
                    hv_Cal1DualQuatCam.Dispose();
                    HOperatorSet.DualQuatConjugate(hv_CamDualQuatCal1, out hv_Cal1DualQuatCam);
                    hv_BaseDualQuatTool1.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_BaseDualQuatTool1 = new HTuple(hvec_BaseDualQuatsTool[hv_Index1].T);
                    }
                    hv_Tool1DualQuatBase.Dispose();
                    HOperatorSet.DualQuatConjugate(hv_BaseDualQuatTool1, out hv_Tool1DualQuatBase);
                    HTuple end_val167 = hv_NumCalibrationPoses - 1;
                    HTuple step_val167 = 1;
                    for (hv_Index2 = hv_Index1 + 1; hv_Index2.Continue(end_val167, step_val167); hv_Index2 = hv_Index2.TupleAdd(step_val167))
                    {
                        //For two robot poses, ...
                        //... compute the movement of the calibration object in the
                        //camera coordinate system.
                        hv_CamDualQuatCal2.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_CamDualQuatCal2 = new HTuple(hvec_CamDualQuatsCal[hv_Index2].T);
                        }
                        hv_DualQuat1.Dispose();
                        HOperatorSet.DualQuatCompose(hv_Cal1DualQuatCam, hv_CamDualQuatCal2, out hv_DualQuat1);
                        //
                        //... compute the movement of the tool in the robot base
                        //coordinate system.
                        hv_BaseDualQuatTool2.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_BaseDualQuatTool2 = new HTuple(hvec_BaseDualQuatsTool[hv_Index2].T);
                        }
                        hv_DualQuat2.Dispose();
                        HOperatorSet.DualQuatCompose(hv_Tool1DualQuatBase, hv_BaseDualQuatTool2,
                            out hv_DualQuat2);
                        //
                        //Check whether the two movements are consistent. If the two
                        //movements are consistent, the scalar parts of the corresponding
                        //dual quaternions should be equal. For the equality check, we
                        //have to take the accuracy of the input poses into account, which
                        //are given by RotationTolerance and TranslationTolerance.
                        hv_LX1.Dispose(); hv_LY1.Dispose(); hv_LZ1.Dispose(); hv_MX1.Dispose(); hv_MY1.Dispose(); hv_MZ1.Dispose(); hv_Rot1.Dispose(); hv_Trans1.Dispose();
                        HOperatorSet.DualQuatToScrew(hv_DualQuat1, "moment", out hv_LX1, out hv_LY1,
                            out hv_LZ1, out hv_MX1, out hv_MY1, out hv_MZ1, out hv_Rot1, out hv_Trans1);
                        hv_LX2.Dispose(); hv_LY2.Dispose(); hv_LZ2.Dispose(); hv_MX2.Dispose(); hv_MY2.Dispose(); hv_MZ2.Dispose(); hv_Rot2.Dispose(); hv_Trans2.Dispose();
                        HOperatorSet.DualQuatToScrew(hv_DualQuat2, "moment", out hv_LX2, out hv_LY2,
                            out hv_LZ2, out hv_MX2, out hv_MY2, out hv_MZ2, out hv_Rot2, out hv_Trans2);
                        while ((int)(new HTuple(hv_Rot1.TupleGreater((new HTuple(180.0)).TupleRad()
                            ))) != 0)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_Rot1 = hv_Rot1 - ((new HTuple(360.0)).TupleRad()
                                        );
                                    hv_Rot1.Dispose();
                                    hv_Rot1 = ExpTmpLocalVar_Rot1;
                                }
                            }
                        }
                        while ((int)(new HTuple(hv_Rot2.TupleGreater((new HTuple(180.0)).TupleRad()
                            ))) != 0)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_Rot2 = hv_Rot2 - ((new HTuple(360.0)).TupleRad()
                                        );
                                    hv_Rot2.Dispose();
                                    hv_Rot2 = ExpTmpLocalVar_Rot2;
                                }
                            }
                        }
                        //
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Rot1 = hv_Rot1.TupleFabs()
                                    ;
                                hv_Rot1.Dispose();
                                hv_Rot1 = ExpTmpLocalVar_Rot1;
                            }
                        }
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Trans1 = hv_Trans1.TupleFabs()
                                    ;
                                hv_Trans1.Dispose();
                                hv_Trans1 = ExpTmpLocalVar_Trans1;
                            }
                        }
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Rot2 = hv_Rot2.TupleFabs()
                                    ;
                                hv_Rot2.Dispose();
                                hv_Rot2 = ExpTmpLocalVar_Rot2;
                            }
                        }
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Trans2 = hv_Trans2.TupleFabs()
                                    ;
                                hv_Trans2.Dispose();
                                hv_Trans2 = ExpTmpLocalVar_Trans2;
                            }
                        }
                        hv_MeanRot.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_MeanRot = 0.5 * (hv_Rot1 + hv_Rot2);
                        }
                        hv_MeanTrans.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_MeanTrans = 0.5 * (hv_Trans1 + hv_Trans2);
                        }
                        hv_SinTheta2.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_SinTheta2 = ((0.5 * hv_MeanRot)).TupleSin()
                                ;
                        }
                        hv_CosTheta2.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_CosTheta2 = ((0.5 * hv_MeanRot)).TupleCos()
                                ;
                        }
                        hv_SinTheta2Squared.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_SinTheta2Squared = hv_SinTheta2 * hv_SinTheta2;
                        }
                        hv_CosTheta2Squared.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_CosTheta2Squared = hv_CosTheta2 * hv_CosTheta2;
                        }
                        //
                        //1. Check the scalar part of the real part of the dual quaternion,
                        //which encodes the rotation component of the screw:
                        //  q[0] = cos(theta/2)
                        //Here, theta is the screw rotation angle.
                        hv_ErrorRot.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_ErrorRot = ((hv_Rot1 - hv_Rot2)).TupleFabs()
                                ;
                        }
                        while ((int)(new HTuple(hv_ErrorRot.TupleGreater((new HTuple(180.0)).TupleRad()
                            ))) != 0)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_ErrorRot = hv_ErrorRot - ((new HTuple(360.0)).TupleRad()
                                        );
                                    hv_ErrorRot.Dispose();
                                    hv_ErrorRot = ExpTmpLocalVar_ErrorRot;
                                }
                            }
                        }
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_ErrorRot = hv_ErrorRot.TupleFabs()
                                    ;
                                hv_ErrorRot.Dispose();
                                hv_ErrorRot = ExpTmpLocalVar_ErrorRot;
                            }
                        }
                        //Compute the standard deviation of the scalar part of the real part
                        //by applying the law of error propagation.
                        hv_StdDevQ0.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_StdDevQ0 = (0.5 * hv_SinTheta2) * hv_RotationTolerance;
                        }
                        //Multiply the standard deviation by a factor to increase the certainty.
                        hv_ToleranceDualQuat0.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_ToleranceDualQuat0 = hv_StdDevFactor * hv_StdDevQ0;
                        }
                        hv_ErrorDualQuat0.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_ErrorDualQuat0 = (((((hv_DualQuat2.TupleSelect(
                                0))).TupleFabs()) - (((hv_DualQuat1.TupleSelect(0))).TupleFabs()))).TupleFabs()
                                ;
                        }
                        //
                        //2. Check the scalar part of the dual part of the dual quaternion,
                        //which encodes translation and rotation components of the screw:
                        //  q[4] = -d/2*sin(theta/2)
                        //Here, d is the screw translation.
                        //
                        //Compute the standard deviation of the scalar part of the dual part
                        //by applying the law of error propagation.
                        hv_StdDevQ4.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_StdDevQ4 = ((((0.25 * hv_SinTheta2Squared) * hv_TranslationToleranceSquared) + ((((0.0625 * hv_MeanTrans) * hv_MeanTrans) * hv_CosTheta2Squared) * hv_RotationToleranceSquared))).TupleSqrt()
                                ;
                        }
                        //Multiply the standard deviation by a factor to increase the certainty.
                        hv_ToleranceDualQuat4.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_ToleranceDualQuat4 = hv_StdDevFactor * hv_StdDevQ4;
                        }
                        hv_ErrorDualQuat4.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_ErrorDualQuat4 = (((((hv_DualQuat2.TupleSelect(
                                4))).TupleFabs()) - (((hv_DualQuat1.TupleSelect(4))).TupleFabs()))).TupleFabs()
                                ;
                        }
                        //If one of the two errors exceeds the computed thresholds, return
                        //a warning for the current pose pair.
                        if ((int)((new HTuple(hv_ErrorDualQuat0.TupleGreater(hv_ToleranceDualQuat0))).TupleOr(
                            new HTuple(hv_ErrorDualQuat4.TupleGreater(hv_ToleranceDualQuat4)))) != 0)
                        {
                            hv_Message.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_Message = ((("Inconsistent pose pair (" + (((hv_PosesIdx.TupleSelect(
                                    hv_Index1))).TupleString("2d"))) + new HTuple(",")) + (((hv_PosesIdx.TupleSelect(
                                    hv_Index2))).TupleString("2d"))) + ")";
                            }
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                        hv_Message);
                                    hv_Warnings.Dispose();
                                    hv_Warnings = ExpTmpLocalVar_Warnings;
                                }
                            }
                        }
                        //
                        //Remember the screw axes (of the robot tool movements) for screws
                        //with a significant rotation part. For movements without rotation
                        //the direction of the screw axis is determined by the translation
                        //part only. Hence, the direction of the screw axis cannot be used
                        //to decide whether an articulated or a SCARA robot is used.
                        if ((int)(new HTuple(hv_Rot2.TupleGreater(hv_StdDevFactor * hv_RotationTolerance))) != 0)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_LX2s = hv_LX2s.TupleConcat(
                                        hv_LX2);
                                    hv_LX2s.Dispose();
                                    hv_LX2s = ExpTmpLocalVar_LX2s;
                                }
                            }
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_LY2s = hv_LY2s.TupleConcat(
                                        hv_LY2);
                                    hv_LY2s.Dispose();
                                    hv_LY2s = ExpTmpLocalVar_LY2s;
                                }
                            }
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_LZ2s = hv_LZ2s.TupleConcat(
                                        hv_LZ2);
                                    hv_LZ2s.Dispose();
                                    hv_LZ2s = ExpTmpLocalVar_LZ2s;
                                }
                            }
                        }
                    }
                }
                //
                //In the second test, we check whether enough calibration poses with a
                //significant rotation part are available for calibration.
                hv_NumPairs.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumPairs = new HTuple(hv_LX2s.TupleLength()
                        );
                }
                hv_NumPairsMax.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumPairsMax = (hv_NumCalibrationPoses * (hv_NumCalibrationPoses - 1)) / 2;
                }
                if ((int)(new HTuple(hv_NumPairs.TupleLess(2))) != 0)
                {
                    hv_Message.Dispose();
                    hv_Message = "There are not enough rotated calibration poses available.";
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                hv_Message);
                            hv_Warnings.Dispose();
                            hv_Warnings = ExpTmpLocalVar_Warnings;
                        }
                    }
                    //In this case, we can skip further test.

                    hv_MinLargeRotationFraction.Dispose();
                    hv_MinLargeAnglesFraction.Dispose();
                    hv_StdDevFactor.Dispose();
                    hv_Type.Dispose();
                    hv_Exception.Dispose();
                    hv_IsHandEyeScara.Dispose();
                    hv_IsHandEyeArticulated.Dispose();
                    hv_NumCameras.Dispose();
                    hv_NumCalibObjs.Dispose();
                    hv_I1.Dispose();
                    hv_PosesIdx.Dispose();
                    hv_RefCalibDataID.Dispose();
                    hv_UseTemporaryCopy.Dispose();
                    hv_CamPoseCal.Dispose();
                    hv_SerializedItemHandle.Dispose();
                    hv_TmpCalibDataID.Dispose();
                    hv_Error.Dispose();
                    hv_Index.Dispose();
                    hv_CamDualQuatCal.Dispose();
                    hv_BasePoseTool.Dispose();
                    hv_BaseDualQuatTool.Dispose();
                    hv_NumCalibrationPoses.Dispose();
                    hv_LX2s.Dispose();
                    hv_LY2s.Dispose();
                    hv_LZ2s.Dispose();
                    hv_TranslationToleranceSquared.Dispose();
                    hv_RotationToleranceSquared.Dispose();
                    hv_Index1.Dispose();
                    hv_CamDualQuatCal1.Dispose();
                    hv_Cal1DualQuatCam.Dispose();
                    hv_BaseDualQuatTool1.Dispose();
                    hv_Tool1DualQuatBase.Dispose();
                    hv_Index2.Dispose();
                    hv_CamDualQuatCal2.Dispose();
                    hv_DualQuat1.Dispose();
                    hv_BaseDualQuatTool2.Dispose();
                    hv_DualQuat2.Dispose();
                    hv_LX1.Dispose();
                    hv_LY1.Dispose();
                    hv_LZ1.Dispose();
                    hv_MX1.Dispose();
                    hv_MY1.Dispose();
                    hv_MZ1.Dispose();
                    hv_Rot1.Dispose();
                    hv_Trans1.Dispose();
                    hv_LX2.Dispose();
                    hv_LY2.Dispose();
                    hv_LZ2.Dispose();
                    hv_MX2.Dispose();
                    hv_MY2.Dispose();
                    hv_MZ2.Dispose();
                    hv_Rot2.Dispose();
                    hv_Trans2.Dispose();
                    hv_MeanRot.Dispose();
                    hv_MeanTrans.Dispose();
                    hv_SinTheta2.Dispose();
                    hv_CosTheta2.Dispose();
                    hv_SinTheta2Squared.Dispose();
                    hv_CosTheta2Squared.Dispose();
                    hv_ErrorRot.Dispose();
                    hv_StdDevQ0.Dispose();
                    hv_ToleranceDualQuat0.Dispose();
                    hv_ErrorDualQuat0.Dispose();
                    hv_StdDevQ4.Dispose();
                    hv_ToleranceDualQuat4.Dispose();
                    hv_ErrorDualQuat4.Dispose();
                    hv_Message.Dispose();
                    hv_NumPairs.Dispose();
                    hv_NumPairsMax.Dispose();
                    hv_LargeRotationFraction.Dispose();
                    hv_NumPairPairs.Dispose();
                    hv_NumPairPairsMax.Dispose();
                    hv_Angles.Dispose();
                    hv_Idx.Dispose();
                    hv_LXA.Dispose();
                    hv_LYA.Dispose();
                    hv_LZA.Dispose();
                    hv_LXB.Dispose();
                    hv_LYB.Dispose();
                    hv_LZB.Dispose();
                    hv_ScalarProduct.Dispose();
                    hv_LargeAngles.Dispose();
                    hv_LargeAnglesFraction.Dispose();
                    hvec_CamDualQuatsCal.Dispose();
                    hvec_BaseDualQuatsTool.Dispose();

                    return;
                }
                hv_LargeRotationFraction.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_LargeRotationFraction = (hv_NumPairs.TupleReal()
                        ) / hv_NumPairsMax;
                }
                if ((int)((new HTuple(hv_NumPairs.TupleLess(4))).TupleOr(new HTuple(hv_LargeRotationFraction.TupleLess(
                    hv_MinLargeRotationFraction)))) != 0)
                {
                    hv_Message.Dispose();
                    hv_Message = new HTuple("Only few rotated robot poses available, which might result in a reduced accuracy of the calibration results.");
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                hv_Message);
                            hv_Warnings.Dispose();
                            hv_Warnings = ExpTmpLocalVar_Warnings;
                        }
                    }
                }
                //
                //In the third test, we compute the angle between the screw axes with
                //a significant rotation part. For SCARA robots, this angle must be 0 in
                //all cases. For articulated robots, for a significant fraction of robot
                //poses, this angle should exceed a certain threshold. For this test, we
                //use the robot tool poses as they are assumed to be more accurate than the
                //calibration object poses.
                hv_NumPairPairs.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumPairPairs = (hv_NumPairs * (hv_NumPairs - 1)) / 2;
                }
                hv_NumPairPairsMax.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumPairPairsMax = (hv_NumPairsMax * (hv_NumPairsMax - 1)) / 2;
                }
                hv_Angles.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Angles = HTuple.TupleGenConst(
                        hv_NumPairPairs, 0);
                }
                hv_Idx.Dispose();
                hv_Idx = 0;
                HTuple end_val277 = hv_NumPairs - 2;
                HTuple step_val277 = 1;
                for (hv_Index1 = 0; hv_Index1.Continue(end_val277, step_val277); hv_Index1 = hv_Index1.TupleAdd(step_val277))
                {
                    hv_LXA.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_LXA = hv_LX2s.TupleSelect(
                            hv_Index1);
                    }
                    hv_LYA.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_LYA = hv_LY2s.TupleSelect(
                            hv_Index1);
                    }
                    hv_LZA.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_LZA = hv_LZ2s.TupleSelect(
                            hv_Index1);
                    }
                    HTuple end_val281 = hv_NumPairs - 1;
                    HTuple step_val281 = 1;
                    for (hv_Index2 = hv_Index1 + 1; hv_Index2.Continue(end_val281, step_val281); hv_Index2 = hv_Index2.TupleAdd(step_val281))
                    {
                        hv_LXB.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LXB = hv_LX2s.TupleSelect(
                                hv_Index2);
                        }
                        hv_LYB.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LYB = hv_LY2s.TupleSelect(
                                hv_Index2);
                        }
                        hv_LZB.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LZB = hv_LZ2s.TupleSelect(
                                hv_Index2);
                        }
                        //Compute the scalar product, i.e. the cosine of the screw
                        //axes. To obtain valid values, crop the cosine to the
                        //interval [-1,1].
                        hv_ScalarProduct.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_ScalarProduct = ((((((((((hv_LXA * hv_LXB) + (hv_LYA * hv_LYB)) + (hv_LZA * hv_LZB))).TupleConcat(
                                1))).TupleMin())).TupleConcat(-1))).TupleMax();
                        }
                        //Compute the angle between the axes in the range [0,pi/2].
                        if (hv_Angles == null)
                            hv_Angles = new HTuple();
                        hv_Angles[hv_Idx] = ((hv_ScalarProduct.TupleFabs())).TupleAcos();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Idx = hv_Idx + 1;
                                hv_Idx.Dispose();
                                hv_Idx = ExpTmpLocalVar_Idx;
                            }
                        }
                    }
                }
                //Large angles should significantly exceed the RotationTolerance.
                hv_LargeAngles.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_LargeAngles = ((hv_Angles.TupleGreaterElem(
                        hv_StdDevFactor * hv_RotationTolerance))).TupleSum();
                }
                //Calculate the fraction of pairs of movements, i.e., pairs of pose
                //pairs, that have a large angle between their corresponding screw
                //axes.
                hv_LargeAnglesFraction.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_LargeAnglesFraction = (hv_LargeAngles.TupleReal()
                        ) / hv_NumPairPairsMax;
                }
                //For SCARA robots, all screw axes should be parallel, i.e., no
                //two screw axes should have a large angle.
                if ((int)(hv_IsHandEyeScara.TupleAnd(new HTuple(hv_LargeAngles.TupleGreater(
                    0)))) != 0)
                {
                    hv_Message.Dispose();
                    hv_Message = new HTuple("The robot poses indicate that this might be an articulated robot, although a SCARA robot was selected in the calibration data model.");
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                hv_Message);
                            hv_Warnings.Dispose();
                            hv_Warnings = ExpTmpLocalVar_Warnings;
                        }
                    }
                }
                //For articulated robots, the screw axes should have a large
                //angles.
                if ((int)(hv_IsHandEyeArticulated) != 0)
                {
                    if ((int)(new HTuple(hv_LargeAngles.TupleEqual(0))) != 0)
                    {
                        //If there is no pair of movements with a large angle between
                        //their corresponding screw axes, this might be a SCARA robot.
                        hv_Message.Dispose();
                        hv_Message = new HTuple("The robot poses indicate that this might be a SCARA robot (no tilted robot poses available), although an articulated robot was selected in the calibration data model.");
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                    hv_Message);
                                hv_Warnings.Dispose();
                                hv_Warnings = ExpTmpLocalVar_Warnings;
                            }
                        }
                    }
                    else if ((int)(new HTuple(hv_LargeAngles.TupleLess(3))) != 0)
                    {
                        //If there are at most 2 movements with a large angle between
                        //their corresponding screw axes, the calibration might be
                        //unstable.
                        hv_Message.Dispose();
                        hv_Message = "Not enough tilted robot poses available for an accurate calibration of an articulated robot.";
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                    hv_Message);
                                hv_Warnings.Dispose();
                                hv_Warnings = ExpTmpLocalVar_Warnings;
                            }
                        }
                    }
                    else if ((int)(new HTuple(hv_LargeAnglesFraction.TupleLess(hv_MinLargeAnglesFraction))) != 0)
                    {
                        //If there is only a low fraction of pairs of movements with
                        //a large angle between their corresponding screw axes, the
                        //accuracy of the calibration might be low.
                        hv_Message.Dispose();
                        hv_Message = new HTuple("Only few tilted robot poses available, which might result in a reduced accuracy of the calibration results.");
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_Warnings = hv_Warnings.TupleConcat(
                                    hv_Message);
                                hv_Warnings.Dispose();
                                hv_Warnings = ExpTmpLocalVar_Warnings;
                            }
                        }
                    }
                }

                hv_MinLargeRotationFraction.Dispose();
                hv_MinLargeAnglesFraction.Dispose();
                hv_StdDevFactor.Dispose();
                hv_Type.Dispose();
                hv_Exception.Dispose();
                hv_IsHandEyeScara.Dispose();
                hv_IsHandEyeArticulated.Dispose();
                hv_NumCameras.Dispose();
                hv_NumCalibObjs.Dispose();
                hv_I1.Dispose();
                hv_PosesIdx.Dispose();
                hv_RefCalibDataID.Dispose();
                hv_UseTemporaryCopy.Dispose();
                hv_CamPoseCal.Dispose();
                hv_SerializedItemHandle.Dispose();
                hv_TmpCalibDataID.Dispose();
                hv_Error.Dispose();
                hv_Index.Dispose();
                hv_CamDualQuatCal.Dispose();
                hv_BasePoseTool.Dispose();
                hv_BaseDualQuatTool.Dispose();
                hv_NumCalibrationPoses.Dispose();
                hv_LX2s.Dispose();
                hv_LY2s.Dispose();
                hv_LZ2s.Dispose();
                hv_TranslationToleranceSquared.Dispose();
                hv_RotationToleranceSquared.Dispose();
                hv_Index1.Dispose();
                hv_CamDualQuatCal1.Dispose();
                hv_Cal1DualQuatCam.Dispose();
                hv_BaseDualQuatTool1.Dispose();
                hv_Tool1DualQuatBase.Dispose();
                hv_Index2.Dispose();
                hv_CamDualQuatCal2.Dispose();
                hv_DualQuat1.Dispose();
                hv_BaseDualQuatTool2.Dispose();
                hv_DualQuat2.Dispose();
                hv_LX1.Dispose();
                hv_LY1.Dispose();
                hv_LZ1.Dispose();
                hv_MX1.Dispose();
                hv_MY1.Dispose();
                hv_MZ1.Dispose();
                hv_Rot1.Dispose();
                hv_Trans1.Dispose();
                hv_LX2.Dispose();
                hv_LY2.Dispose();
                hv_LZ2.Dispose();
                hv_MX2.Dispose();
                hv_MY2.Dispose();
                hv_MZ2.Dispose();
                hv_Rot2.Dispose();
                hv_Trans2.Dispose();
                hv_MeanRot.Dispose();
                hv_MeanTrans.Dispose();
                hv_SinTheta2.Dispose();
                hv_CosTheta2.Dispose();
                hv_SinTheta2Squared.Dispose();
                hv_CosTheta2Squared.Dispose();
                hv_ErrorRot.Dispose();
                hv_StdDevQ0.Dispose();
                hv_ToleranceDualQuat0.Dispose();
                hv_ErrorDualQuat0.Dispose();
                hv_StdDevQ4.Dispose();
                hv_ToleranceDualQuat4.Dispose();
                hv_ErrorDualQuat4.Dispose();
                hv_Message.Dispose();
                hv_NumPairs.Dispose();
                hv_NumPairsMax.Dispose();
                hv_LargeRotationFraction.Dispose();
                hv_NumPairPairs.Dispose();
                hv_NumPairPairsMax.Dispose();
                hv_Angles.Dispose();
                hv_Idx.Dispose();
                hv_LXA.Dispose();
                hv_LYA.Dispose();
                hv_LZA.Dispose();
                hv_LXB.Dispose();
                hv_LYB.Dispose();
                hv_LZB.Dispose();
                hv_ScalarProduct.Dispose();
                hv_LargeAngles.Dispose();
                hv_LargeAnglesFraction.Dispose();
                hvec_CamDualQuatsCal.Dispose();
                hvec_BaseDualQuatsTool.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {

                hv_MinLargeRotationFraction.Dispose();
                hv_MinLargeAnglesFraction.Dispose();
                hv_StdDevFactor.Dispose();
                hv_Type.Dispose();
                hv_Exception.Dispose();
                hv_IsHandEyeScara.Dispose();
                hv_IsHandEyeArticulated.Dispose();
                hv_NumCameras.Dispose();
                hv_NumCalibObjs.Dispose();
                hv_I1.Dispose();
                hv_PosesIdx.Dispose();
                hv_RefCalibDataID.Dispose();
                hv_UseTemporaryCopy.Dispose();
                hv_CamPoseCal.Dispose();
                hv_SerializedItemHandle.Dispose();
                hv_TmpCalibDataID.Dispose();
                hv_Error.Dispose();
                hv_Index.Dispose();
                hv_CamDualQuatCal.Dispose();
                hv_BasePoseTool.Dispose();
                hv_BaseDualQuatTool.Dispose();
                hv_NumCalibrationPoses.Dispose();
                hv_LX2s.Dispose();
                hv_LY2s.Dispose();
                hv_LZ2s.Dispose();
                hv_TranslationToleranceSquared.Dispose();
                hv_RotationToleranceSquared.Dispose();
                hv_Index1.Dispose();
                hv_CamDualQuatCal1.Dispose();
                hv_Cal1DualQuatCam.Dispose();
                hv_BaseDualQuatTool1.Dispose();
                hv_Tool1DualQuatBase.Dispose();
                hv_Index2.Dispose();
                hv_CamDualQuatCal2.Dispose();
                hv_DualQuat1.Dispose();
                hv_BaseDualQuatTool2.Dispose();
                hv_DualQuat2.Dispose();
                hv_LX1.Dispose();
                hv_LY1.Dispose();
                hv_LZ1.Dispose();
                hv_MX1.Dispose();
                hv_MY1.Dispose();
                hv_MZ1.Dispose();
                hv_Rot1.Dispose();
                hv_Trans1.Dispose();
                hv_LX2.Dispose();
                hv_LY2.Dispose();
                hv_LZ2.Dispose();
                hv_MX2.Dispose();
                hv_MY2.Dispose();
                hv_MZ2.Dispose();
                hv_Rot2.Dispose();
                hv_Trans2.Dispose();
                hv_MeanRot.Dispose();
                hv_MeanTrans.Dispose();
                hv_SinTheta2.Dispose();
                hv_CosTheta2.Dispose();
                hv_SinTheta2Squared.Dispose();
                hv_CosTheta2Squared.Dispose();
                hv_ErrorRot.Dispose();
                hv_StdDevQ0.Dispose();
                hv_ToleranceDualQuat0.Dispose();
                hv_ErrorDualQuat0.Dispose();
                hv_StdDevQ4.Dispose();
                hv_ToleranceDualQuat4.Dispose();
                hv_ErrorDualQuat4.Dispose();
                hv_Message.Dispose();
                hv_NumPairs.Dispose();
                hv_NumPairsMax.Dispose();
                hv_LargeRotationFraction.Dispose();
                hv_NumPairPairs.Dispose();
                hv_NumPairPairsMax.Dispose();
                hv_Angles.Dispose();
                hv_Idx.Dispose();
                hv_LXA.Dispose();
                hv_LYA.Dispose();
                hv_LZA.Dispose();
                hv_LXB.Dispose();
                hv_LYB.Dispose();
                hv_LZB.Dispose();
                hv_ScalarProduct.Dispose();
                hv_LargeAngles.Dispose();
                hv_LargeAnglesFraction.Dispose();
                hvec_CamDualQuatsCal.Dispose();
                hvec_BaseDualQuatsTool.Dispose();

                throw HDevExpDefaultException;
            }
        }


        private void ShowCamPose(HTuple hv_Pose)
        {
            textBoxX.Text = $"{hv_Pose[0].D * 1000:G6}";
            textBoxY.Text = $"{hv_Pose[1].D * 1000:G6}";
            textBoxZ.Text = $"{hv_Pose[2].D * 1000:G6}";
            textBoxRX.Text = $"{hv_Pose[3].D:G6}";
            textBoxRY.Text = $"{hv_Pose[4].D:G6}";
            textBoxRZ.Text = $"{hv_Pose[5].D:G6}";
        }

        private void ShowLightPose(HTuple hv_Pose)
        {
            textBox_LightInCam_X.Text = $"{hv_Pose[0].D * 1000:G6}";
            textBox_LightInCam_Y.Text = $"{hv_Pose[1].D * 1000:G6}";
            textBox_LightInCam_Z.Text = $"{hv_Pose[2].D * 1000:G6}";
            textBox_LightInCam_RX.Text = $"{hv_Pose[3].D:G6}";
            textBox_LightInCam_RY.Text = $"{hv_Pose[4].D:G6}";
            textBox_LightInCam_RZ.Text = $"{hv_Pose[5].D:G6}";
        }
        private void ClearCamPose()
        {
            textBoxX.Text = "";
            textBoxY.Text = "";
            textBoxZ.Text = "";
            textBoxRX.Text = "";
            textBoxRY.Text = "";
            textBoxRZ.Text = "";
        }

        HTuple RobotPose = null;
        private void ShowRobotPose(HTuple hv_Pose)
        {
            textBoxRobotX.Text = $"{hv_Pose[0].D * 1000:G6}";
            textBoxRobotY.Text = $"{hv_Pose[1].D * 1000:G6}";
            textBoxRobotZ.Text = $"{hv_Pose[2].D * 1000:G6}";
            textBoxRobotRX.Text = $"{hv_Pose[3].D:G6}";
            textBoxRobotRY.Text = $"{hv_Pose[4].D:G6}";
            textBoxRobotRZ.Text = $"{hv_Pose[5].D:G6}";
            RobotPose = hv_Pose;
        }
        private void ClearRobotPose()
        {
            textBoxRobotX.Text = "";
            textBoxRobotY.Text = "";
            textBoxRobotZ.Text = "";
            textBoxRobotRX.Text = "";
            textBoxRobotRY.Text = "";
            textBoxRobotRZ.Text = "";
        }
        private void button保存坐标_Click(object sender, EventArgs e)
        {
            if (RobotPose != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "选择要保存的位置";
                sfd.Filter = "Pose(*.dat)|*.dat";
                if (DialogResult.OK == sfd.ShowDialog())
                {
                    try
                    {
                        string path = sfd.FileName;
                        HOperatorSet.WritePose(RobotPose, path);
                        ShowMessage("保存外参成功");
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message, Color.Red);
                    }
                }
                sfd.Dispose();
            }
        }


        private void radioButton节卡_CheckedChanged(object sender, EventArgs e)
        {
            //先判断一下是不是库卡机器人，是的话，要先关闭通讯
            if (robot.GetType() == typeof(KukaRobot) || robot.GetType() == typeof(KawasakiRobot))
            {
                robot.Close();
            }
            //创建新对象
            if (radioButton节卡.Checked)
            {
                robot = new JAKARobot();
            }
            else if (radioButton安川.Checked)
            {
                robot = new YRCRobot();
            }
            else if (radioButton发那科.Checked)
            {
                robot = new FanucRobot();
            }
            else if (radioButton_kuka.Checked)
            {
                robot = new KukaRobot();
            }
            else if (radioButton_kawasaki.Checked)
            {
                robot = new KawasakiRobot();
            }

            else
            {
                robot = new JAKARobot();
            }
        }

        private void buttonGetRobotPose_Click(object sender, EventArgs e)
        {
            if (robot.GetType() != typeof(KukaRobot))
            {
                //连的上就保存，连不上，就跳过
                if (robot.Open(textBoxRobotIP.Text, int.Parse(textBoxRobotPort.Text)))
                {
                    if (robot.ReadPose(out HPose hPose))
                    {
                        ShowRobotPose(hPose);
                    }
                    else
                    {
                        ShowMessage($"机器人坐标获取失败：" + robot.ErrMsg, Color.Red);
                    }
                    robot.Close();
                }
                else
                {
                    ShowMessage($"机器人未连接", Color.Red);
                }
            }
            else
            {
                if (robot.ReadPose(out HPose hPose))
                {
                    ShowRobotPose(hPose);
                }
                else
                {
                    ShowMessage($"机器人坐标获取失败：" + robot.ErrMsg, Color.Red);
                }
            }
        }

        private void button_load_Light_In_Cam_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择要加载的文件";
            openFileDialog.Filter = "PoseFile(*.dat)|*.dat";
            if (DialogResult.OK == openFileDialog.ShowDialog())
            {
                try
                {
                    HOperatorSet.ReadPose(openFileDialog.FileName, out hv_LightInCam);
                    ShowLightPose(hv_LightInCam);
                }
                catch (Exception ex)
                {
                    ShowMessage($"加载光平面标定文件{openFileDialog.FileName}失败：" + ex.Message, Color.Red);
                }
            }
            openFileDialog.Dispose();
        }

        private void button_load_Tool_In_Cam_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择要加载的文件";
            openFileDialog.Filter = "PoseFile(*.dat)|*.dat";
            if (DialogResult.OK == openFileDialog.ShowDialog())
            {
                try
                {
                    HOperatorSet.ReadPose(openFileDialog.FileName, out hv_ToolInCamPose);
                    ShowCamPose(hv_ToolInCamPose);
                }
                catch (Exception ex)
                {
                    ShowMessage($"加载手眼标定文件{openFileDialog.FileName}失败：" + ex.Message, Color.Red);
                }
            }
            openFileDialog.Dispose();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            cam.SetLine1Inverter(checkBox1.Checked);
        }

        private void button_transform_Click(object sender, EventArgs e)
        {
            //获取内参
            if (hv_CameraParameters == null)
            {
                ShowMessage($"内参为空，请加载文件", Color.Red);
                return;
            }
            //获取光平面
            if (hv_LightInCam == null)
            {
                ShowMessage($"光平面外参为空，请加载文件", Color.Red);
                return;
            }
            //获取手眼标定
            if (hv_ToolInCamPose == null)
            {
                ShowMessage($"手眼标定外参为空，请加载文件", Color.Red);
                return;
            }

            //读取内参
            HTuple hv_CamParamOut;
            HOperatorSet.ChangeRadialDistortionCamPar("adaptive", hv_CameraParameters, 0, out hv_CamParamOut);
            HObject ho_Map;
            HOperatorSet.GenRadialDistortionMap(out ho_Map, hv_CameraParameters, hv_CamParamOut, "bilinear");
            //读取光面标定参数
            HTuple hv_LightToCam;
            HOperatorSet.PoseToHomMat3d(hv_LightInCam, out hv_LightToCam);
            //读取手眼标定参数
            HTuple hv_CamToTool;
            HOperatorSet.PoseToHomMat3d(hv_ToolInCamPose, out hv_CamToTool);

            if (listBox图像列表.SelectedIndex != -1)
            {
                HObject ho_ImageMapped, ho_LightMapped;
                HObject worldImageMapped;

                string key = (string)listBox图像列表.SelectedItem;

                TransformImageWorld(ImagesCalib[key], ImagesLight[key], ho_Map, hv_LightToCam, hv_CamToTool, out ho_ImageMapped, out ho_LightMapped, out worldImageMapped);
            }
        }

        // 图像坐标转光平面再转相机坐标系坐标，之前还有转为机器人坐标，后来没用
        private void TransformImageWorld(HObject image, HObject imageLight, HObject ho_Map, HTuple hv_LightToCam, HTuple hv_CamToTool,out HObject ho_ImageMapped, out HObject ho_LightMapped, out HObject worldImageMapped )
        {
            HOperatorSet.GenEmptyObj(out ho_ImageMapped);
            HOperatorSet.GenEmptyObj(out ho_LightMapped);
            HOperatorSet.GenEmptyObj(out worldImageMapped);

                


                //HTuple pose = Poses[key];

                //光面计算
                HTuple hv_Width = new HTuple();
                HTuple hv_Height = new HTuple();
                HTuple hv_X = new HTuple(), hv_Y = new HTuple();


                hv_Width.Dispose(); hv_Height.Dispose();
                HOperatorSet.GetImageSize(image, out hv_Width, out hv_Height);

                HImage worldXImage = new HImage();
                HImage worldYImage = new HImage();
                HImage worldZImage = new HImage();
                worldXImage.GenImageConst("real", hv_Width, hv_Height);
                worldYImage.GenImageConst("real", hv_Width, hv_Height);
                worldZImage.GenImageConst("real", hv_Width, hv_Height);

                int W = hv_Width;
                int H = hv_Height;
                int N = W * H;

                // 行号 0…H-1  重复 W 次
                int[] yArr = new int[N];
                for (int k = 0; k < N; k++) yArr[k] = k / W;   // 单层循环，速度已经很快

                // 列号 0…W-1  连续重复 H 次
                int[] xArr = new int[N];
                for (int k = 0; k < N; k++) xArr[k] = k % W;

                ////测试几个点
                //int[] yArr = new int[7] { 895,1082,1264,1443,1623,1810,1992};
                //int[] xArr = new int[7] { 1136, 1136, 1136, 1136, 1136, 1136, 1136};


                HTuple yList = new HTuple(yArr);
                HTuple xList = new HTuple(xArr);


                HOperatorSet.ImagePointsToWorldPlane(hv_CameraParameters, hv_LightInCam, yList,
                    xList, "m", out hv_X, out hv_Y);

                HTuple hv_Z = new HTuple(new double[hv_X.Length]);

                HOperatorSet.AffineTransPoint3d(hv_LightToCam, hv_X, hv_Y, hv_Z, out HTuple camX, out HTuple camY,
                    out HTuple camZ);

                ////转法兰盘坐标系
                //HOperatorSet.AffineTransPoint3d(hv_CamToTool, camX, camY, camZ, out HTuple toolX, out HTuple toolY,
                //    out HTuple toolZ);
                ////转基座坐标
                //HPose robotPose = new HPose();
                //robotPose.ReadPose(robotPose_path);

                //var ToolToRobot = robotPose.PoseToHomMat3d();
                //HTuple robotX = ToolToRobot.AffineTransPoint3d(toolX, toolY, toolZ, out HTuple robotY, out HTuple robotZ);

                worldXImage.SetGrayval(yList, xList, (HTuple)(camX));
                worldYImage.SetGrayval(yList, xList, (HTuple)(camY));
                worldZImage.SetGrayval(yList, xList, (HTuple)(camZ));


                HOperatorSet.Compose3(worldXImage, worldYImage, worldZImage, out HObject worldImage);


                //图片矫正
                ho_ImageMapped.Dispose();
                HOperatorSet.MapImage(image, ho_Map, out ho_ImageMapped);
                ho_LightMapped.Dispose();
                HOperatorSet.MapImage(imageLight, ho_Map, out ho_LightMapped);
                HOperatorSet.MapImage(worldImage, ho_Map, out worldImageMapped);

        }

        // 图像坐标转光平面

        private void TransformImageLight(HObject ho_Map, HTuple hv_LightToCam, HTuple hv_Width,HTuple hv_Height, out HObject ho_LightMapped)
        {
            HOperatorSet.GenEmptyObj(out ho_LightMapped);

            //光面计算
            HTuple hv_X = new HTuple(), hv_Y = new HTuple();

            HImage lightXImage = new HImage();
            HImage lightYImage = new HImage();
            lightXImage.GenImageConst("real", hv_Width, hv_Height);
            lightYImage.GenImageConst("real", hv_Width, hv_Height);

            int W = hv_Width;
            int H = hv_Height;
            int N = W * H;

            // 行号 0…H-1  重复 W 次
            int[] yArr = new int[N];
            for (int k = 0; k < N; k++) yArr[k] = k / W;   // 单层循环，速度已经很快

            // 列号 0…W-1  连续重复 H 次
            int[] xArr = new int[N];
            for (int k = 0; k < N; k++) xArr[k] = k % W;

            ////测试几个点
            //int[] yArr = new int[7] { 895,1082,1264,1443,1623,1810,1992};
            //int[] xArr = new int[7] { 1136, 1136, 1136, 1136, 1136, 1136, 1136};


            HTuple yList = new HTuple(yArr);
            HTuple xList = new HTuple(xArr);


            HOperatorSet.ImagePointsToWorldPlane(hv_CameraParameters, hv_LightInCam, yList,
                xList, "m", out hv_X, out hv_Y);

            lightXImage.SetGrayval(yList, xList, (HTuple)(hv_X));
            lightYImage.SetGrayval(yList, xList, (HTuple)(hv_Y));

            HOperatorSet.Compose2(lightXImage, lightYImage, out HObject lightImage);

            //图片矫正
            ho_LightMapped.Dispose();
            HOperatorSet.MapImage(lightImage, ho_Map, out ho_LightMapped);
            //ho_LightMapped = lightImage;
        }

        private void radioButton_平面图_CheckedChanged(object sender, EventArgs e)
        {
            listBox图像列表_SelectedIndexChanged(null, null);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            cam.SetLine2Inverter(checkBox2.Checked);

        }

        private void radioButton_kuka_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBoxRobotIP_TextChanged(object sender, EventArgs e)
        {
            if (robot.GetType() == typeof(KukaRobot))
            {
                robot.Close();
            }
        }

        private void textBoxRobotPort_TextChanged(object sender, EventArgs e)
        {
            if (robot.GetType() == typeof(KukaRobot))
            {
                robot.Close();
            }
        }

        private void button_getLightMap_Click(object sender, EventArgs e)
        {
            //获取内参
            if (hv_CameraParameters == null)
            {
                ShowMessage($"内参为空，请加载文件", Color.Red);
                return;
            }
            //获取光平面
            if (hv_LightInCam == null)
            {
                ShowMessage($"光平面外参为空，请加载文件", Color.Red);
                return;
            }

            //读取内参
            HTuple hv_CamParamOut;
            HOperatorSet.ChangeRadialDistortionCamPar("adaptive", hv_CameraParameters, 0, out hv_CamParamOut);
            HObject ho_Map;
            HOperatorSet.GenRadialDistortionMap(out ho_Map, hv_CameraParameters, hv_CamParamOut, "bilinear");
            //读取光面标定参数
            HTuple hv_LightToCam;
            HOperatorSet.PoseToHomMat3d(hv_LightInCam, out hv_LightToCam);

            if (listBox图像列表.SelectedIndex != -1)
            {
                HObject lightImageMapped;
                //暂时测试先保存到软件文件夹
                string lightMapPath = $"./lightImageMapped.tiff";


                string key = (string)listBox图像列表.SelectedItem;
                HTuple hv_Width = new HTuple();
                HTuple hv_Height = new HTuple();

                hv_Width.Dispose(); hv_Height.Dispose();
                HOperatorSet.GetImageSize(ImagesCalib[key], out hv_Width, out hv_Height);


                TransformImageLight(ho_Map, hv_LightToCam, hv_Width, hv_Height, out lightImageMapped);

                HOperatorSet.WriteImage(lightImageMapped, "tiff", 0, lightMapPath);

            }
        }
    }
}
