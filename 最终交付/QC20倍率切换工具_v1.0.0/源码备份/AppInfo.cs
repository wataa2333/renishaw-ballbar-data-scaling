using System;

namespace Wataa.QC20Tool
{
    internal static class AppInfo
    {
        public const string ProductName = "雷尼绍QC20球杆仪位移偏差数据倍率切换工具";
        public const string ShortName = "QC20倍率切换工具";
        public const string Version = "1.0.0";
        public const string Author = "wataa";
        public const string Email = "wataa2333@gmail.com";

        public const string BallbarFile = "Renishaw.Calibration.Ballbar.dll";
        public const string DeviceServiceFile =
            "Renishaw.Calibration.Ballbar.BallbarDeviceService.dll";

        public const string OriginalBallbarHash =
            "12B679343DD871B0FF4F39A7E5A404266EB3AA7AA55BB3D2F86B2F4884B8A255";
        public const string OriginalDeviceServiceHash =
            "7494DF3816A27D4EC90449B0B836D66C667490357D6B2784EA47080CB4C7B86B";
    }
}
