using System.Diagnostics.CodeAnalysis;
using Foundation;

namespace Haihv.Elis.Tool.ChuyenDvhc
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        [Experimental("EXTEXP0018")]
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}