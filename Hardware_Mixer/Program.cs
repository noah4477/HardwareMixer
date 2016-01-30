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
using System.Threading;
using System.IO.Ports;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using HardwareMixerObj;
using SerializeData;
namespace XAudio2Playback
{

    class Program
    {
        //Device Name class - Used to have friendly and real names to identify a device
        public class DeviceName
        {
            public DeviceName(string realname, string friendlyname) { RealName = realname; FriendlyName = friendlyname; }
            public string RealName;
            public string FriendlyName;
        }
        static List<DeviceName> DefaultDeviceNames = new List<DeviceName>();
        static TcpClient client;
        static NetworkStream NS;
        static Thread readThread;
        static Thread writeThread;
        static TcpListener server;

        static PCData GlobalPCData = new PCData();
        static RPIData GlobalPiData = new RPIData();
        static List<ProgramData> AllPrograms = new List<ProgramData>();
       
        private static void Main(string[] args)
        {
            //Add Devices to device list
            DefaultDeviceNames.Add(new DeviceName("Speakers (2- Logitech G930 Headset)", "Logitech G930 Headset"));
            DefaultDeviceNames.Add(new DeviceName("Realtek Digital Output(Optical) (Realtek High Definition Audio)", "Realtek Digital Output"));
            //Initialize the Volume Mixer data
            InitGlobalPCData();
            
            //Start the server on port 8000
            int port = 8000;
            Console.WriteLine("Port: " + port);
            server = new TcpListener(IPAddress.Any, port);
            StartServer();
            
        }
        //Initialize the PC data
        private static void InitGlobalPCData()
        {
            GlobalPCData.Programs = Mixer.Mixer.GetProgramsFirstTime();
            AllPrograms = Mixer.Mixer.GetAllPrograms();
            GlobalPCData.defaultDevice = Mixer.Mixer.GetPCDataDefaultDevice();
        }
        //Cleanup the Client and Server and restart them
        static void CleanupAndRestart()
        {
            server.Stop();
            client.Close();
            NS.Close();
            writeThread.Abort();
            readThread.Abort();
            StartServer();
        }

        //Start the read and write sockets
        public static void StartServer()
        {
            server.Start();
            //Connect to client and get networkstream
            Console.WriteLine("Connected!");
            client = server.AcceptTcpClient();
           
            NS = client.GetStream();
            NS.Flush();
            //Start write and read threads
            readThread = new Thread(() => readSocket(NS));
            readThread.Start();

            writeThread = new Thread(() => writeSocket(NS));
            writeThread.Start();
        }

        //Gets and sends PCData to a socket
        static void writeSocket(NetworkStream ns)
        {
           
            while (true)
            {
                AllPrograms = Mixer.Mixer.GetAllPrograms();
                List<ProgramData> program = new List<ProgramData>();
                for (int k = 0; k < GlobalPCData.Programs.Count; k++)
                {
                    for (int i = 0; i < AllPrograms.Count; i++)
                    {
                        if (GlobalPCData.Programs[k].PID == AllPrograms[i].PID)
                        {
                            program.Add(GlobalPCData.Programs[k]);
                        }
                    }
                }

                GlobalPCData.Programs = program;
                PCData PCdata = GetPCData();
                PCdata = FixNames(PCdata);
                //Converts PCdata to a byte array
                Byte[] data = SerializeToByteArray(PCdata);
                SendBytes(data, ns);

                Thread.Sleep(2000);
            }
        }
        //Converts an object into a byte array
        public static byte[] SerializeToByteArray(object request)
        {
            byte[] result;
            BinaryFormatter serializer = new BinaryFormatter();
            using (MemoryStream memStream = new MemoryStream())
            {
                serializer.Serialize(memStream, request);
                result = memStream.GetBuffer();
            }
            return result;
        }
        //Converts a name from it's "real" name to it's user friendly name
        private static PCData FixNames(PCData PCdata)
        {
            for (int i = 0; i < DefaultDeviceNames.Count; i++)
            {
                if (PCdata.defaultDevice.Name == DefaultDeviceNames[i].RealName)
                {
                    PCdata.defaultDevice.Name = DefaultDeviceNames[i].FriendlyName;
                }
            }
                    return PCdata;
        }
        //Gets PCData and returns it
        private static PCData GetPCData()
        {
            PCData data = new PCData();
            data.defaultDevice = Mixer.Mixer.GetPCDataDefaultDevice();
            List<int> PIDList = GetPIDList();
            PIDList = RemoveDuplicates(PIDList);

            data.Programs = Mixer.Mixer.GetPCDataPrograms(PIDList[0], PIDList[1], PIDList[2], AllPrograms);
            return data;
        }
        //Removes dupicate PIDs
        private static List<int> RemoveDuplicates(List<int> PIDList)
        {
            for (int i = 0; i < PIDList.Count; i++)
            { 
                if(i < PIDList.Count -1)
                {
                    if (PIDList[i] == PIDList[i + 1])
                    {
                       PIDList[i+1] = GetNewProgram(PIDList);
                    }
                }
                else if (i == PIDList.Count - 1)
                {
                    if (PIDList[i] == PIDList[0])
                        PIDList[i] = GetNewProgram(PIDList);
                }
            }
            return PIDList;
        }
        //Get PIDs from the master list
        private static List<int> GetPIDList()
        {
            List<int> pidlist = new List<int>();
            for (int i = 0; i < GlobalPCData.Programs.Count; i++)
            {
                pidlist.Add(GlobalPCData.Programs[i].PID);
            }
            while (pidlist.Count < 3)
            {
                pidlist.Add(GetNewProgram(pidlist));
            }
            return pidlist;
        }
        //Gets a new program that isn't in the existing pidlist
        private static int GetNewProgram(List<int> pidlist)
        {
            for (int i = 0; i < AllPrograms.Count; i++)
            {
                int count = 0;
                for (int k = 0; k < pidlist.Count; k++)
                {
                    if (AllPrograms[i].PID == pidlist[k])
                    {
                        count++;
                    }
                }
                if (count == 0)
                    return AllPrograms[i].PID;
            }
                return -1;
        }
        //Sends byte data to through a network stream
        private static void SendBytes(byte[] data, NetworkStream ns)
        {
            try
            {
                byte[] userDataBytes = data; ;
                
                byte[] userDataLen = BitConverter.GetBytes((Int32)userDataBytes.Length);
                if (userDataBytes.Length == 0)
                    throw new NotImplementedException();
                //Writes the length of the data and the data itself
                ns.Write(userDataLen, 0, 4);
                ns.Write(userDataBytes, 0, userDataBytes.Length);
            }
            //If something goes wrong restart the connection
            catch (Exception e)
            {
                CleanupAndRestart();
            }
        }
        //Reads incoming data from a network stream
        static void readSocket(NetworkStream Networkstream)
        {
            while (true)
            {
                byte[] readMsgData = { 0 };
                try
                {
                    byte[] readMsgLen = new byte[4];
                    Networkstream.Read(readMsgLen, 0, 4);

                    int dataLen = BitConverter.ToInt32(readMsgLen, 0);
                    readMsgData = new byte[dataLen];
                    Networkstream.Read(readMsgData, 0, dataLen);
                }
                catch (System.IO.IOException) { /*when we force close, this goes off, so ignore it*/ }
                catch (Exception e)
                { 
                    Console.WriteLine(e.ToString());
                }
                //Reformat data from the network stream to make it readable and process it
                GlobalPiData = Serialize.DeserializeFromByteArray<RPIData>(readMsgData);
                ProcessPIData();
                Thread.Sleep(1);
            }
        }
        //Process the data and change what is necessary on the computer
        private static void ProcessPIData()
        {
            GlobalPCData.defaultDevice = UpdateDefaultDevice(GlobalPiData.DefaultDevice, ref GlobalPiData, ref GlobalPCData.Programs);
            for(int i =0; i < 3; i++)
            {
                if(GlobalPiData.Programs[i].Null == false)
                for (int j = 0; j < 3; j++)
                {
                    if (GlobalPCData.Programs[j].Null == false)
                    {
                        if (GlobalPiData.Programs[i].PID == GlobalPCData.Programs[j].PID)
                        {
                            ProgramData data = ChangeData(GlobalPiData.Programs[i], GlobalPCData.Programs[j]);
                            GlobalPCData.Programs[j] = data;
                            break;
                        }
                        
                    }
                }
            }

        }

        private static DefaultDevice UpdateDefaultDevice(RPIDefaultDevice Defaultdevice, ref RPIData RPIdata, ref List<ProgramData> Programs)
        {
            if (GlobalPiData.DefaultDevice.ChangeDevice == false)
            {
                DefaultDevice device = GlobalPCData.defaultDevice;
                device.Volume = Defaultdevice.Volume;
                device.IsMute = Defaultdevice.IsMute;
                Mixer.Mixer.UpdateDefaultDevice(Defaultdevice, RPIdata);
                return device;
            }
            else 
            {
                GlobalPiData.DefaultDevice.ChangeDevice = false;
                Mixer.Mixer.AudioDevices defaultDevices = new Mixer.Mixer.AudioDevices();
                
                defaultDevices.UpdateAudioDevices();

                for (int i = 0; i < defaultDevices.Devices.Count; i++)
                {
                    if (defaultDevices.Devices[i].Name == GlobalPCData.defaultDevice.Name)
                    {
                        if (i == defaultDevices.Devices.Count - 1)
                        {
                            i = 0;
                        }
                        else 
                        {
                            i += 1;
                        }

                        defaultDevices.SetDefaultAudioDevice(defaultDevices.Devices[i].Id);
                        Programs = Mixer.Mixer.GetProgramsFirstTime();
                        return Mixer.Mixer.GetPCDataDefaultDevice();
                    }
                }
            }
            return Mixer.Mixer.GetPCDataDefaultDevice();
        }

        private static ProgramData ChangeData(RPIProgramData RPIData, ProgramData PCData)
        {
            if (RPIData.ChangeDevice)
            {
                List<ProgramData> AllPrograms = Mixer.Mixer.GetAllPrograms();
                for (int i = 0; i < AllPrograms.Count; i++)
                {
                    Boolean IsUsed = false;
                    for (int j = 0; j < GlobalPCData.Programs.Count; j++)
                    {
                        if (AllPrograms[i].PID == GlobalPCData.Programs[j].PID)
                        {
                            IsUsed = true;
                            break;
                        }
                    }
                    if(IsUsed == false)
                    return AllPrograms[i];
                }


            }
            else
            {
                if (true)
                {
                    Mixer.Mixer.UpdateProgram(RPIData);
                    PCData.Volume = RPIData.Volume;
                    PCData.IsMute = RPIData.IsMute;
                    return PCData;
                }
            }
            return PCData;
        }
    }
}
