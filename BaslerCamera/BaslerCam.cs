using System;
using Basler.Pylon;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using HalconDotNet;

namespace BaslerCamera
{

    public class BaslerCam
    {
        //相机连接的个数
        //public int CameraNumber = CameraFinder.Enumerate().Count;

        //放出一个Camera
        Camera camera = null;

        //basler里用于将相机采集的图像转换成位图
        PixelDataConverter pxConvert = new PixelDataConverter();

        public BaslerParam Param = new BaslerParam();

        public string Name => camera?.CameraInfo[CameraInfoKey.UserDefinedName];

        /// <summary>
        /// 报错信息
        /// </summary>
        public string ErrMsg => _errMsg;
        string _errMsg = string.Empty;

        public BaslerCam()
        {

        }
        /// <summary>
        /// 打开相机
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            try
            {
                if (camera == null)
                {
                    camera = new Camera();

                    //运行模式
                    camera.CameraOpened += Configuration.AcquireContinuous;//自由模式

                    //断开连接事件
                    camera.ConnectionLost += Camera_ConnectionLost;

                    //打开相机
                    camera.Open();

                    return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _errMsg = ex.ToString();
                return false;
            }
        }
        /// <summary>
        /// 打开相机
        /// </summary>
        /// <returns></returns>
        public bool Open(string IDName)
        {
            try
            {
                if (camera == null)
                {
                    foreach (ICameraInfo item in CameraFinder.Enumerate())
                    {
                        if (item[CameraInfoKey.UserDefinedName] == IDName.ToString())
                        {
                            camera = new Camera(item);

                            //运行模式
                            camera.CameraOpened += Configuration.AcquireContinuous;//自由模式

                            //断开连接事件
                            camera.ConnectionLost += Camera_ConnectionLost;

                            //打开相机
                            camera.Open();

                            return true;
                        }
                    }
                    _errMsg = $"未找到{IDName}相机";
                }
                else
                {
                    if (IDName.ToString() == camera.Parameters[PLGigECamera.DeviceUserID].GetValue())
                    {
                        return true;
                    }
                    else
                    {
                        _errMsg = $"对象已打开{camera.Parameters[PLGigECamera.DeviceUserID].GetValue()},无法再打开{IDName}，请先关闭再打开";
                    }
                }
            }
            catch (Exception ex)
            {
                _errMsg = ex.ToString();
            }
            return false;
        }

        private void Camera_ConnectionLost(object sender, EventArgs e)//断开连接事件
        {
            camera.StreamGrabber.Stop();
            Close();
        }

        /// <summary>
        /// 获取曝光
        /// </summary>
        /// <returns></returns>
        public bool GetExposure(out double exposure)
        {
            exposure = 0;
            if (camera != null)
            {
                try
                {
                    if (camera.Parameters.Contains(PLGigECamera.ExposureTimeAbs))
                    {
                        exposure = camera.Parameters[PLGigECamera.ExposureTimeAbs].GetValue();
                    }
                    else
                    {
                        exposure = camera.Parameters[PLCamera.ExposureTime].GetValue();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        /// <summary>
        /// 设置相机曝光
        /// </summary>
        /// <param name="time">要设置的曝光值，范围10-840000</param>
        public bool SetExposure(double time)
        {
            if (camera != null)
            {
                try
                {
                    if (camera.Parameters.Contains(PLGigECamera.ExposureTimeAbs))
                    {
                        camera.Parameters[PLGigECamera.ExposureTimeAbs].SetValue(time);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.ExposureTime].SetValue(time);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        /// <summary>
        /// 获取相机频率
        /// </summary>
        /// <returns></returns>
        public bool GetHz(out double hz)
        {
            hz = 0;
            if (camera != null)
            {
                try
                {
                    hz = camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].GetValue();
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        /// <summary>
        /// 设置相机频率
        /// </summary>
        /// <param name="hz">要设置的频率值，范围0.15-1000000</param>
        public bool SetHz(double hz)
        {
            if (camera != null)
            {
                try
                {
                    camera.Parameters[PLGigECamera.AcquisitionFrameRateEnable].SetValue(true);
                    camera.Parameters[PLGigECamera.AcquisitionFrameRateAbs].SetValue(hz);
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        /// <summary>
        /// 设置相机输出图像尺寸
        /// </summary>
        /// <param name="width">图像的宽度</param>
        /// <param name="height">图像的高度</param>
        /// <returns></returns>
        public bool SetSize(long width, long height)
        {
            if (camera != null)
            {
                try
                {
                    if (width > camera.Parameters[PLGigECamera.WidthMax].GetValue())
                    {
                        width = camera.Parameters[PLGigECamera.WidthMax].GetValue();
                    }
                    if (height > camera.Parameters[PLGigECamera.HeightMax].GetValue())
                    {
                        height = camera.Parameters[PLGigECamera.HeightMax].GetValue();
                    }
                    camera.Parameters[PLGigECamera.Width].SetValue(width);
                    camera.Parameters[PLGigECamera.Height].SetValue(height);
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        public bool GetSize(out Size size)
        {
            size = new Size(-1, -1);
            if (camera != null)
            {
                try
                {
                    size.Width = (int)camera.Parameters[PLGigECamera.Width].GetValue();
                    size.Height = (int)camera.Parameters[PLGigECamera.Height].GetValue();
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        /// <summary>
        /// 设置相机输出图像偏移
        /// </summary>
        /// <param name="offsetX">图像的X偏移</param>
        /// <param name="offsetY">图像的Y偏移</param>
        /// <returns></returns>
        public bool SetOffset(long offsetX, long offsetY)
        {
            if (camera != null)
            {
                try
                {
                    if (offsetX > camera.Parameters[PLGigECamera.WidthMax].GetValue() - camera.Parameters[PLGigECamera.Width].GetValue())
                    {
                        offsetX = camera.Parameters[PLGigECamera.WidthMax].GetValue() - camera.Parameters[PLGigECamera.Width].GetValue();
                    }
                    else if (offsetX < 0)
                    {
                        offsetX = 0;
                    }
                    if (offsetY > camera.Parameters[PLGigECamera.HeightMax].GetValue() - camera.Parameters[PLGigECamera.Height].GetValue())
                    {
                        offsetY = camera.Parameters[PLGigECamera.HeightMax].GetValue() - camera.Parameters[PLGigECamera.Height].GetValue();
                    }
                    else if (offsetY < 0)
                    {
                        offsetY = 0;
                    }
                    camera.Parameters[PLGigECamera.OffsetX].SetValue(offsetX);
                    camera.Parameters[PLGigECamera.OffsetY].SetValue(offsetY);
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        public bool GetOffset(out Point point)
        {
            point = new Point(0, 0);
            if (camera != null)
            {
                try
                {
                    point.X = (int)camera.Parameters[PLGigECamera.OffsetX].GetValue();
                    point.Y = (int)camera.Parameters[PLGigECamera.OffsetY].GetValue();
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        public HObject OneShot()
        {
            if (camera != null)
            {
                if (camera.StreamGrabber.IsGrabbing)
                {
                    camera.StreamGrabber.Stop();
                    Thread.Sleep(100);
                }

                camera.StreamGrabber.Start(1);
                while (camera.StreamGrabber.IsGrabbing)
                {
                    // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                    IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                    using (grabResult)
                    {
                        // Image grabbed successfully?
                        if (grabResult.GrabSucceeded)
                        {
                            return GrabResult2HImage(grabResult);
                        }
                    }
                    _errMsg = "采集异常";
                    return null;
                }
                _errMsg = "开始采集异常";
                return null;
            }
            else
            {
                _errMsg = "相机未打开";
                return null;
            }
        }
 
        public void KeepShot(Action<HObject> UseImages)
        {
            if (camera != null)
            {
                if (camera.StreamGrabber.IsGrabbing)
                {
                    camera.StreamGrabber.Stop();
                    Thread.Sleep(100);
                }
                camera.StreamGrabber.Start();
                Thread th = new Thread(() =>
                {
                    while (camera != null && camera.StreamGrabber.IsGrabbing)
                    {
                        // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                        IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        if (grabResult != null)
                        {
                            using (grabResult)
                            {
                                // Image grabbed successfully?
                                if (grabResult.GrabSucceeded)
                                {
                                    // Access the image data.
                                    HObject hImage = GrabResult2HImage(grabResult);
                                    UseImages(hImage);
                                }
                            }
                        }
                    };
                });
                th.Start();
            }
        }

        public void Stop()
        {
            if (camera != null)
            {
                camera.StreamGrabber.Stop();
            }
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        public void Close()
        {
            if (camera != null)
            {
                if (camera.StreamGrabber.IsGrabbing)
                {
                    camera.StreamGrabber.Stop();
                }
                while (camera.StreamGrabber.IsGrabbing)
                {
                    Thread.Sleep(7);
                }
                camera.Close();

                //camera.Dispose();
                camera = null;
            }
        }

        //将相机抓取到的图像转换成HImage图
        HObject GrabResult2HImage(IGrabResult grabResult)
        {
            if (grabResult.PixelTypeValue == PixelType.Mono8)
            {
                //HImage image = new HImage("byte", grabResult.Width, grabResult.Height, grabResult.PixelDataPointer);
                HOperatorSet.GenImage1(out HObject image, "byte", grabResult.Width, grabResult.Height, grabResult.PixelDataPointer);
                return image;
            }
            else if (grabResult.PixelTypeValue == PixelType.Mono12)
            {
                //HImage image = new HImage("uint2", grabResult.Width, grabResult.Height, grabResult.PixelDataPointer);
                HOperatorSet.GenImage1(out HObject image, "uint2", grabResult.Width, grabResult.Height, grabResult.PixelDataPointer);
                return image;
            }
            else
            {
                _errMsg = $"{grabResult.PixelTypeValue}格式转换未支持";
                return null;
            }
        }

        public bool SingleTrigger(bool bflag)
        {
            if (camera != null)
            {
                try
                {
                    if (bflag)
                    {
                        camera?.Parameters[PLGigECamera.AcquisitionMode].SetValue(PLGigECamera.AcquisitionMode.Continuous);//采集模式，连续
                        camera?.Parameters[PLGigECamera.TriggerSelector].SetValue(PLGigECamera.TriggerSelector.FrameStart);//选择信号用途？？？

                        camera?.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.On);//触发模式
                        camera?.Parameters[PLGigECamera.TriggerSource].SetValue(PLGigECamera.TriggerSource.Line1);//触发源
                        camera?.Parameters[PLGigECamera.TriggerActivation].SetValue(PLGigECamera.TriggerActivation.RisingEdge);//上升沿
                        camera?.Parameters[PLGigECamera.TimerDelayAbs].SetValue(1000);//触发延时，单位us 
                    }
                    else
                    {
                        camera?.Parameters[PLGigECamera.TriggerMode].SetValue(PLGigECamera.TriggerMode.Off);//触发模式
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }

        public bool InitSet()
        {
            if (camera != null)
            {
                try
                {
                    camera.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.Mono8);
                }
                catch (Exception ex)
                {
                    _errMsg = ex.ToString();
                    return false;
                }
                if (!SingleTrigger(false))
                {
                    return false;
                }
                if (!SetSize(Param.SizeWidth, Param.SizeHeight))
                {
                    return false;
                }
                if (!SetOffset(Param.OffsetX, Param.OffsetY))
                {
                    return false;
                }
                return true;
            }
            else
            {
                _errMsg = "相机未打开";
                return false;
            }
        }
    }

    enum Trigger
    {
        SoftTrigger = 0,
        s = 1
    }

    [Serializable]
    public class BaslerParam
    {
        public bool Enable = true;
        public int Exposure = 5000;
        public int SizeWidth = 640, SizeHeight = 480;
        public int OffsetX = 0, OffsetY = 0;
        public double LeftX = 0.25, TopY = 0.25, RightX = 0.75, DownY = 0.75;
        public byte GrayMin = 0, GrayMax = 255;
        public string ImageFormat = ".jpg";

        public string ErrMsg => _errMsg;
        string _errMsg = string.Empty;
        public bool Load()
        {
            bool result = true;
            string basePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
            try
            {
                string paramPath = basePath + "BaslerParam.xml";
                if (File.Exists(paramPath))
                {
                    BaslerParam param = null;
                    XmlSerializer xml = new XmlSerializer(this.GetType());
                    using (FileStream stream = new FileStream(paramPath, FileMode.OpenOrCreate))
                    {
                        param = (BaslerParam)xml.Deserialize(stream);
                    }
                    if (param != null)
                    {
                        this.Enable = param.Enable;
                        this.Exposure = param.Exposure;
                        this.SizeWidth = param.SizeWidth;
                        this.SizeHeight = param.SizeHeight;
                        this.OffsetX = param.OffsetX;
                        this.OffsetY = param.OffsetY;
                        this.LeftX = param.LeftX;
                        this.TopY = param.TopY;
                        this.RightX = param.RightX;
                        this.DownY = param.DownY;
                        this.GrayMin = param.GrayMin;
                        this.GrayMax = param.GrayMax;
                    }
                    else
                    {
                        result = false;
                        _errMsg = paramPath + "文件格式异常";
                    }
                }
                else
                {
                    result = false;
                    _errMsg = paramPath + "文件不存在";
                }
            }
            catch (Exception ex)
            {
                result = false;
                _errMsg = ex.ToString();
            }

            if (!result)
            {
                result = true;
                try
                {
                    string paramPath = basePath + "BaslerParam_bak.xml";
                    if (File.Exists(paramPath))
                    {
                        BaslerParam param = null;
                        XmlSerializer xml = new XmlSerializer(this.GetType());
                        using (FileStream stream = new FileStream(paramPath, FileMode.OpenOrCreate))
                        {
                            param = (BaslerParam)xml.Deserialize(stream);
                        }
                        if (param != null)
                        {
                            this.Enable = param.Enable;
                            this.Exposure = param.Exposure;
                            this.SizeWidth = param.SizeWidth;
                            this.SizeHeight = param.SizeHeight;
                            this.OffsetX = param.OffsetX;
                            this.OffsetY = param.OffsetY;
                            this.LeftX = param.LeftX;
                            this.TopY = param.TopY;
                            this.RightX = param.RightX;
                            this.DownY = param.DownY;
                            this.GrayMin = param.GrayMin;
                            this.GrayMax = param.GrayMax;
                            this.ImageFormat = param.ImageFormat;

                            File.Copy(paramPath, basePath + "BaslerParam.xml", true);
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                catch (Exception ex)
                {
                    result = false;
                }
            }

            return result;
        }
        public bool Save()
        {
            bool result = true;
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                string BaslerParamPath = basePath + "BaslerParam.xml";
                XmlSerializer xml = new XmlSerializer(this.GetType());
                using (FileStream stream = new FileStream(BaslerParamPath, FileMode.Create))
                {
                    xml.Serialize(stream, this);
                }
                File.Copy(BaslerParamPath, basePath + "BaslerParam_bak.xml", true);
            }
            catch (Exception ex) 
            { 
                result = false; 
                _errMsg = ex.ToString(); 
            }
            return result;
        }
    }
}
