namespace Haihv.Elis.Tool.ChuyenDvhc
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Lấy tỷ lệ thu phóng của màn hình:
            var scale = DeviceDisplay.MainDisplayInfo.Density;

            return new Window(new MainPage())
            {
                Title = "Chuyển đổi đơn vị hành chính",
                Height = 750 * scale,
                Width = 960 * scale,
                MinimumHeight = 750,
                MinimumWidth = 960,
            };
        }
    }
}