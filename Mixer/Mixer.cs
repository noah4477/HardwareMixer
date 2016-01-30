using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using CSCore;
using CSCore.Codecs;
using CSCore.XAudio2;
using CSCore.CoreAudioAPI;
using CSCore.DirectSound;
using System.Diagnostics;
using System.Drawing;
using TsudaKageyu;
using System.Text.RegularExpressions;

namespace Mixer
{
    public class Mixer
    {
      static List<IconData> iconbytelist = new List<IconData>();

        private static System.Timers.Timer myTimer;
        public static void StartProgram()
        {
            myTimer = new System.Timers.Timer();

            myTimer.Elapsed += new System.Timers.ElapsedEventHandler(AudioSessionManage);
            myTimer.Interval = 300;
            myTimer.Enabled = true;
        }
        public static void startsession()
        {
            AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
            AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
            foreach (AudioSessionControl session in sessionEnumerator)
            {
                AudioVolume(session, 100);
                AudioSession(session);
            }
            SetVolumePID(13160, 100);
            SetDefaultMasterVolume(100);

            AudioDevices test = new AudioDevices();
            test.UpdateAudioDevices();
            test.SetDefaultAudioDevice(0);

        }
        private static void AudioSessionManage(object sender, EventArgs e)
        {
            AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
            AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
            foreach (AudioSessionControl session in sessionEnumerator)
            {
                AudioVolume(session, 100);
                AudioSession(session);
            }
            SetVolumePID(13160, 100);
            SetDefaultMasterVolume(21);

            AudioDevices test = new AudioDevices();
            test.UpdateAudioDevices();
            test.SetDefaultAudioDevice(0);

        }
        private static AudioSessionEnumerator EnumerateSessions()
        {
            AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
            AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
            return sessionEnumerator;
           
        }
        private static void SetVolumePID(int PID, int volume)
        {
            Process[] AllProcess = Process.GetProcesses();
            if (DoesPIDExist(AllProcess, PID))
            {
                AudioSessionEnumerator sessions = EnumerateSessions();
                foreach (AudioSessionControl session in sessions)
                {
                    if (PID == GetSessionPID(session))
                    {
                        AudioVolume(session, volume);
                    }
                }
            }
        }
        private static int GetSessionPID(AudioSessionControl session)
        {
            int PID;
            using (var audiosession = session.QueryInterface<AudioSessionControl2>())
            {
                
                audiosession.GetProcessIdNative(out PID);
            }
            return PID;
        }
        private static Boolean DoesPIDExist(Process[] AllProcess, int PID)
        {
            foreach (Process process in AllProcess)
            {
                if (PID == process.Id)
                    return true;
            }
            return false;
        }

        private static void AudioSession(AudioSessionControl session)
        {
            using (var audiosession = session.QueryInterface<AudioSessionControl2>())
            {
                int id;
                audiosession.GetProcessIdNative(out id);

                string name = "";
                if (id != 8104 && id != 0)
                {
                    Process[] AllProcess = Process.GetProcesses();
                    foreach (Process process in AllProcess)
                    {
                        if (id == process.Id)
                        {
                            name = process.ProcessName;
                            
                            Icon[] Icons = GetIcons(process.MainModule.FileName);
                            for(int i =0; i < Icons.Length; i++){
                                if(Icons[i].Width == 48 && Icons[i].Height == 48){
                                    IconData icondata = new IconData(id, Icons[i]);
                                    
                                    iconbytelist.Add( icondata);
                                    Console.WriteLine(name);
                                    Console.WriteLine(id);
                                    return;
                                }
                            }
                            

                        }
                    }


                }
                else if (id == 8104 || id == 0)
                {
                    AudioVolume(session, 100);
                    name = "System Sounds";
                    Console.WriteLine(name);
                    Console.WriteLine(id);
                }
            }
        }
        private static Icon[] GetIcons(string filename)
        {
            Icon[] splitIcons = new Icon[0];
            IconExtractor ie = new IconExtractor(filename);
            if (ie.Count > 0)
            {
               splitIcons  = IconUtil.Split(ie.GetIcon(0));
            }
            return splitIcons;
        }
        private static void AudioVolume(AudioSessionControl session, int volume)
        {
            SimpleAudioVolume simpleVolume = session.QueryInterface<SimpleAudioVolume>();
             
            simpleVolume.MasterVolume = (float)volume / 100;

            bool muted = simpleVolume.IsMuted;
            simpleVolume.IsMuted = !muted;
            simpleVolume.IsMuted = muted;
        }
        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {   
                MMDevice device = GetDefaultDevice();
               // Console.WriteLine("DefaultDevice: " + device.FriendlyName);
                var sessionManager = AudioSessionManager2.FromMMDevice(device);

            return sessionManager;
        }
        public static void SetDefaultMasterVolume(int volume)
        {
            MMDevice defaultdevice = GetDefaultDevice();
            SetMasterVolume(defaultdevice, volume);

        }
        private static void SetMasterVolume(MMDevice device, int volume)
        {
            AudioEndpointVolume Endpointvolume = AudioEndpointVolume.FromDevice(device);

            Endpointvolume.MasterVolumeLevelScalar = (float)volume / 100;
        }
        public static MMDevice GetDefaultDevice()
        {
            
               using (var enumerator = new MMDeviceEnumerator())
            {
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return device;
            }

        }
       


         public class IconData
        {
            public IconData(int iconname, Icon icon) { MemoryStream ms = new MemoryStream(); icon.Save(ms); Icondata = ms.GetBuffer(); Iconname = iconname; }
            public IconData(int iconname, byte[] icondata) { Iconname = iconname; Icondata = icondata; }
            int Iconname;
            byte[] Icondata;
        };
         public class AudioDevices
         {
             // static List<System.Collections.ObjectModel.ReadOnlyCollection<DirectSoundDevice>> Devices = new List<System.Collections.ObjectModel.ReadOnlyCollection<DirectSoundDevice>;
             public List<AudioDevice> Devices = new List<AudioDevice>();
             public void UpdateAudioDevices()
             {
                 Devices = EnumerateDevices();

             }
             
             public bool SetDefaultAudioDevice(int id)
             {
                 if (id == GetDefaultID())
                     return true;
                 var process = Process.Start(new ProcessStartInfo("lib/SoundSwitch.AudioInterface.exe", id.ToString())
                 {
                     UseShellExecute = false,
                     RedirectStandardOutput = true,
                     CreateNoWindow = true
                 });
                 if (!process.WaitForExit(5000))
                 {
                     throw new TimeoutException("Timed out while trying to switch audio output.");
                 }
                 return (process.ExitCode == 0);
             }
             private int GetDefaultID()
             {
                 int id =0;
                 UpdateAudioDevices();
                 var defaultdevice = GetDefaultDevice();

                 foreach (AudioDevice device in Devices)
                 {
                     if (device.Name == defaultdevice.FriendlyName)
                     {
                         id = device.Id;
                         break;
                     }
                 }

                 return id;
             }
             private List<AudioDevice> EnumerateDevices()
             { 
                 List<AudioDevice> devicestemp = new List<AudioDevice>();
                 List<AudioDevice> devices = new List<AudioDevice>();
                 devicestemp = ListDevices();
                
                 var Masterdevices = DirectSoundDeviceEnumerator.EnumerateDevices();
                 for (int i = 0; i < Masterdevices.Count; i++)
                 {
                     if (Masterdevices[i].Guid != new Guid("00000000-0000-0000-0000-000000000000"))
                     {
                         for (int k = 0; k < devicestemp.Capacity; k++)
                         {
                             if (Masterdevices[i].Description == devicestemp[k].Name)
                             {
                                 devices.Add(new AudioDevice(devicestemp[k].Name, devicestemp[k].Id));
                                 break;
                             }
                         }
                     }
                 }
                     return devices;
             }
             private List<AudioDevice> ListDevices()
             {
                string output = GetDevicesList();
                List<AudioDevice> devices = new List<AudioDevice>();

            var lineMatch = new Regex("^(?<id>\\d+): (?<name>.*)$");

            foreach (var line in output.Split('\n'))
            {
                Match match;
                if ((match = lineMatch.Match(line)).Success)
                {
                    devices.Add(new AudioDevice(match.Groups["name"].Value.Trim(), System.Convert.ToInt32(match.Groups["id"].Value)));
                }
            }

                return devices;
             }
             
             private string GetDevicesList()
             {
                 Process process = Process.Start(new ProcessStartInfo("lib/SoundSwitch.AudioInterface.exe")
                     {
                         UseShellExecute = false,
                         RedirectStandardOutput = true,
                         CreateNoWindow = true
                     });
                return process.StandardOutput.ReadToEnd();
             }
         };
       
        public class AudioDevice
        {
            public AudioDevice(string name, int id) { Name = name; Id = id; }

            public string Name;
            public int Id;
        };
    }
}

