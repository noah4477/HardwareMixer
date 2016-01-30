using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using CSCore;
using CSCore.Codecs;
using CSCore.XAudio2;
using CSCore.CoreAudioAPI;
using System.Diagnostics;
using System.Drawing;
using TsudaKageyu;
namespace XAudio2Playback
{
    class Program
    {
        
        private static void Main(string[] args)
        {
            
        Mixer.Mixer.StartProgram();
            Console.ReadKey();
        }
    
    }
}
