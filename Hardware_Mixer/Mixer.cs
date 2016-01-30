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
using HardwareMixerObj;
using System.Linq;
namespace Mixer
{
    
    public class Mixer
    {
       static List<IconData> iconbytelist = new List<IconData>();
        
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
            List<ProgramData> Programs = new List<ProgramData>();
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


        private static List<ProgramData> EnumerateProgram(List<ProgramData> programs, AudioSessionControl session)
        {
            ProgramData program = new ProgramData();
            SimpleAudioVolume simpleVolume = session.QueryInterface<SimpleAudioVolume>();
            using (var audiosession = session.QueryInterface<AudioSessionControl2>())
            {
                int id;
                audiosession.GetProcessIdNative(out id);
                
                for (int a = 0; a < programs.Count; a++)
                {
                    
                    if (programs[a].PID == id)
                    {
                        return programs;
                    }
                }
                    if (id != 8104 && id != 0)
                    {
                        Process[] AllProcess = Process.GetProcesses();
                        for (int b = 0; b < AllProcess.Length; b++ )
                        {
                            Process process = AllProcess[b];
                            if (id == process.Id)
                            {
                                try
                                {

                                    Icon[] Icons = GetIcons(process.MainModule.FileName);
                               
                                    for (int i = 0; i < Icons.Length; i++)
                                    {
                                        if (Icons[i].Width == 48 && Icons[i].Height == 48)
                                        {
                                            program.Name = process.ProcessName;
                                            program.PID = id;
                                            program.IsMute = simpleVolume.IsMuted;
                                            program.Volume = (int)(simpleVolume.MasterVolume * 100);
                                            programs.Add(program);
                                            program.Icon = Icons[i].ToBitmap();
                                            break;
                                        }
                                    }
                                }
                                catch(Exception e)
                                {
                                }
                                
                            }
                        }
                    }
                    else if (id == 8104 || id == 0)
                    {
                        program.Name = "System Sounds";
                        program.PID = id;
                        program.Icon = null;
                        program.Volume = (int)(simpleVolume.MasterVolume * 100);
                        program.IsMute = simpleVolume.IsMuted;
                        programs.Add(program);
                    }
            }
            return programs;
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
            var sessionManager = AudioSessionManager2.FromMMDevice(device);
            return sessionManager;
        }
        public static void SetDefaultMasterVolume(int volume)
        {
            MMDevice defaultdevice = GetDefaultDevice();
            SetMasterVolume(defaultdevice, volume);

        }
        public static DefaultDevice GetPCDataDefaultDevice()
        {
            DefaultDevice device = new DefaultDevice();
            MMDevice Defaultdevice = GetDefaultDevice();
            AudioEndpointVolume Endpointvolume = AudioEndpointVolume.FromDevice(Defaultdevice);
            device.IsMute = Endpointvolume.GetMute();
            device.Name = Defaultdevice.FriendlyName;
            device.Volume = (int)(Endpointvolume.MasterVolumeLevelScalar*100);
            device.Icon = null;
            return device;
        }  
        public static List<ProgramData> GetPCDataPrograms(int PID1, int PID2, int PID3, List<ProgramData> AllPrograms)
        {
            List<ProgramData> programs = new List<ProgramData>();
            bool p1 = false;
            bool p2 = false;
            bool p3 = false;
            
            if (PID1 == PID2)
                p1 = true;
            if (PID2 == PID3)
                p2 = true;
            if (PID3 == PID1)
                p3 = true;
            if(!p1)
                programs.Add(GetProgramFromPID(PID1, AllPrograms));
             // programs.Add(GetProgramFromPID(PID1));
            if(!p2)
              programs.Add(GetProgramFromPID(PID2, AllPrograms));
            if(!p3)
                programs.Add(GetProgramFromPID(PID3, AllPrograms));
            if (programs.Count < 3) 
            {
                ProgramData program = new ProgramData();
                program.Null = true;
                while (programs.Count < 3)
                {
                    programs.Add(program);
                }
            }
            
            return programs;
        }

        public static ProgramData GetProgramFromPID(int PID, List<ProgramData> AllPrograms)
        {
            foreach (ProgramData program in AllPrograms)
            {
                if (program.PID == PID || ((program.PID == 8401 || program.PID == 0) && program.PID == PID))
                {
                    return program;
                }
            }
            ProgramData Blank = new ProgramData();
            Blank.Null = true;
            return Blank;
        }

        public static List<ProgramData> GetProgramsFirstTime()
        {
            List<ProgramData> programs = new List<ProgramData>();
           
            AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
            AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
            foreach (AudioSessionControl session in sessionEnumerator)
            {
                if (programs.Count < 3)
                    programs = EnumerateProgram(programs, session);
                if (programs.Count > 3)
                    break;
                
            }
            ProgramData nulldata = new ProgramData();
            nulldata.Null = true;
            while (programs.Count < 3)
            {
                programs.Add(nulldata);
            }
            return programs;
        }

        private static void SetMasterVolume(MMDevice device, int volume)
        {
            AudioEndpointVolume Endpointvolume = AudioEndpointVolume.FromDevice(device);
            
            Endpointvolume.MasterVolumeLevelScalar = (float)volume / 100;
        }
        private static void SetMasterMute(bool isMute, ref RPIData data)
        {
            MMDevice device = GetDefaultDevice();
            AudioEndpointVolume Endpointvolume = AudioEndpointVolume.FromDevice(device);

            Endpointvolume.SetMuteNative(isMute, new Guid());
            if (isMute == false)
                for (int i = 0; i < data.Programs.Count; i++)
                {
                    data.Programs[i].IsMute = false;
                }
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
             public List<AudioDevice> Devices = new List<AudioDevice>();
             public void UpdateAudioDevices()
             {
                 Devices = EnumerateDevices();
                 Devices = Sort(Devices);

             }
             private List<AudioDevice> Sort(List<AudioDevice> devices)
             {
                 devices = devices.OrderBy(x => x.Name).ThenBy(x => x.Id).ToList();
                 return devices;
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

                Regex lineMatch = new Regex("^(?<id>\\d+): (?<name>.*)$");

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

        public static void UpdateDefaultDevice(RPIDefaultDevice device, RPIData data)
        {
            SetDefaultMasterVolume(device.Volume);
            SetMasterMute(device.IsMute, ref data);
        }
        public static void UpdateProgram(RPIProgramData RPIData)
        {
            Process[] AllProcess = Process.GetProcesses();
            if (DoesPIDExist(AllProcess, RPIData.PID))
            {
                AudioSessionEnumerator sessions = EnumerateSessions();
                foreach (AudioSessionControl session in sessions)
                {
                    if (RPIData.PID == GetSessionPID(session))
                    {
                        SimpleAudioVolume simpleVolume = session.QueryInterface<SimpleAudioVolume>();

                        simpleVolume.MasterVolume = (float)RPIData.Volume / 100;

                        simpleVolume.IsMuted = RPIData.IsMute;
                    }
                }
            }
        }

        public static List<ProgramData> GetAllPrograms()
        {
            List<ProgramData> programs = new List<ProgramData>();
            AudioSessionManager2 sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render);
            AudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
            foreach (AudioSessionControl session in sessionEnumerator)
            {
                    programs = EnumerateProgram(programs, session);
            }
            return programs;
        }
    }
}

