using System;
using System.Threading;
using ININ.InteractionClient.AddIn;
using Microsoft.Win32;

namespace TelUriRepair
{
    public class RegistryMonitor :IAddIn
    {
        const string RegistryKey = @"Software\Classes\tel\shell\open\command";

        private ITraceContext _trace;
        private Timer _timer = null;

        public void Load(IServiceProvider serviceProvider)
        {
            _trace = serviceProvider.GetService(typeof(ITraceContext)) as ITraceContext;

            if (_trace == null)
            {
                return;
            }

            _trace.Always("CallTo registry monitor loaded");

            try
            {
                CheckRegistry();
                
                _timer = new Timer(new TimerCallback(o=>{
                    try{
                        CheckRegistry();
                    }
                    catch{}
                }),null, 1000,1000);
                
            }
            catch (Exception ex)
            {
                _trace.Exception(ex, "Exception caught getting registry value");
            }

        }

        public void Unload()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }
        }

        private void CheckRegistry()
        {
            if (!WillInteractionClientHandleCallTo())
            {
                using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(RegistryKey,true))
                {
                    reg.SetValue(null, String.Format("\"{0}\" \"%1\"", GetInteractionClientExePath()));
                }
            }
        }

        private bool WillInteractionClientHandleCallTo()
        {
            using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(RegistryKey))
            {
                string s = reg.GetValue(null).ToString();

                bool willHandle = s.Contains(GetInteractionClientExePath());

                if (!willHandle)
                {
                    _trace.Always("Client is not set to handle CallTo: " + s);
                }

                return willHandle;
            }
        }

        public string GetInteractionClientExePath()
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        }
    }
}
