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
namespace Mixer
{
    public class Mixer
    {
         private static System.Timers.Timer myTimer;
         public static void StartProgram()
        {
            myTimer = new System.Timers.Timer();

            myTimer.Elapsed += new System.Timers.ElapsedEventHandler(AudioSessionManage);
            myTimer.Interval = 1000;
            myTimer.Enabled = true;
        }
        private static void AudioSessionManage(object sender, EventArgs e)
        {
                AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
                AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
                    foreach (AudioSessionControl session in sessionEnumerator)
                    {
                        AudioVolume(session);
                        AudioSession(session);
            }


        }
        private static void AudioSession(AudioSessionControl session)
        {
            using (var audiosession = session.QueryInterface<AudioSessionControl2>())
            {
                int id;
                audiosession.GetProcessIdNative(out id);
                string name = "";
                if (id != 0)
                {
                    Process currentprocess = Process.GetProcessById(id);
                    name = currentprocess.MainModule.ModuleName;
                    //Icon ico = Icon.ExtractAssociatedIcon(currentprocess.MainModule.FileName);
                    IconExtractor ie = new IconExtractor(currentprocess.MainModule.FileName);
                    Icon[] splitIcons = IconUtil.Split(ie.GetIcon(0));

                }
                else
                {
                    name = "System Sounds";
                }
                Console.WriteLine(id);
                Console.WriteLine(name);

            }
        }
        private static void AudioVolume(AudioSessionControl session)
        {
            SimpleAudioVolume simpleVolume = session.QueryInterface<SimpleAudioVolume>();

            simpleVolume.MasterVolume = 1.0f;

            bool muted = simpleVolume.IsMuted;
            simpleVolume.IsMuted = !muted;
            simpleVolume.IsMuted = muted;
        }
        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    Console.WriteLine("DefaultDevice: " + device.FriendlyName);

                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }
        
    }
}
