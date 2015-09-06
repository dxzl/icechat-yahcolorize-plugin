// This program is distributed under the terms of the GNU General Public
// License (2015).
#region Using directives
using System.Windows.Forms;
using System.Runtime.InteropServices;

#endregion

namespace IceChatPlugin
{
    #region FormMsgPump

    // NOTE: To find this window from a Win32 app with FindWindow or FindWindowEx
    // use the class name "WindowsForms10.Window.8.app.0" and the window text
    // "IceChatMsgPump"
    public partial class FormMsgPump : Form
    {
        private int RWM_ColorizeNet = 0;

        public FormMsgPump(int WinMsg)
        {
            InitializeComponent();
            RWM_ColorizeNet = WinMsg;
            //this.Name = "IceChatMsgPump"; // set already in the Designer!
        }

//        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            // Raise the OnDataReceived event if WM_COPYDATA...
            if (m.Msg == Plugin.WM_COPYDATA)
            {
                // Get the COPYDATASTRUCT struct from lParam.
                Plugin.COPYDATASTRUCT cds = 
                    (Plugin.COPYDATASTRUCT)m.GetLParam(typeof(Plugin.COPYDATASTRUCT));
                // If the size and registered windows messages match...
                if (cds.cbData == Marshal.SizeOf(typeof(Plugin.COLORIZENETSTRUCT)) &&
                    (int)cds.dwData == RWM_ColorizeNet)
                {
                    // Marshal the data from the unmanaged memory block to a COLORIZENET struct
                    Plugin.COLORIZENETSTRUCT cns =
                        (Plugin.COLORIZENETSTRUCT)Marshal.PtrToStructure(cds.lpData, 
                                                          typeof(Plugin.COLORIZENETSTRUCT));

                    // Send data to our main Plugin object via GlobalNotifier...
                    GlobalNotifier.OnDataReceived(cns);
                }
            }

            base.WndProc(ref m);
        }
    }
    #endregion
}
