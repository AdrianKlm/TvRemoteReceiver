using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TvRemoteReceiver
{
    public class RemoteRes
    {
        public ButtonName Result { get; set; } 
        public string Hex { get; set; } 
        public bool Hold { get; set; }


    }

    public enum ButtonName
    {
        //ChDown,
        //Chanel,
        //ChUp,
        //Prev,
        //Next,
        //Play,
        VolDown,
        VolUp,
        CrossUp,
        CrossRight,
        CrossDown,
        CrossLeft,
        CrossOk,
        BtnRed,
        BtnGreen,
        VolMute,
        Error






    }
}
