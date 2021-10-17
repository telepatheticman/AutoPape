using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoPape
{
    public class MonitorSettings
    {
        public int hight;
        public int width;
        public int xOffset;
        public int yOffset;
    }



    class WallpaperManager
    {
        public int hight;
        public int width;
        public int xOffset;
        public int yOffset;

        public void getScreenSpace()
        {
            Screen[] zScreen = Screen.AllScreens;
            string csScreens = "";

            for (int i = 0; i < zScreen.Length; i++)
            {
                // For each screen, add the screen properties to a list box.

                csScreens += "Device Name: " + zScreen[i].DeviceName + "\r\n";
                Rectangle Rectangle_Screen = zScreen[i].Bounds;
                csScreens += String.Format
                (
                  "  Bounds: {0}\r\n" +
                  "  Width: {0}\r\n" +
                  "  Height: {0}\r\n" +
                  "",
                  zScreen[i].Bounds.ToString(),
                  zScreen[i].Bounds.Left.ToString(),
                  zScreen[i].Bounds.Top.ToString(),
                  zScreen[i].Bounds.Width.ToString(),
                  zScreen[i].Bounds.Height.ToString()
                );
                csScreens += "Type: " + zScreen[i].GetType().ToString() + "\r\n";
                csScreens += "Working Area: " + zScreen[i].WorkingArea.ToString() + "\r\n";
                csScreens += "Primary Screen: " + zScreen[i].Primary.ToString() + "\r\n";


            }
        }
    }
}
