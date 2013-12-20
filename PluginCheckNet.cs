using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Rainmeter;

namespace PluginCheckNet
{
    internal class Measure
    {
        public string ConnectionType;
        public double ReturnValue;
        public int UpdateCounter;
        public int UpdateRate;

        static Thread _networkThread;

        void CheckConnection(object type)
        {
            if ((string)type == "NETWORK" || (string)type == "INTERNET")
            {
                if (Convert.ToDouble(NetworkInterface.GetIsNetworkAvailable()) == 0)
                {
                    ReturnValue = -1.0;
                }
                else
                {
                    ReturnValue = 1.0;
                }
            }

            if (ReturnValue == 1.0 && (string)type == "INTERNET")
            {
                try
                {
                    IPAddress[] addresslist = Dns.GetHostAddresses("www.msftncsi.com");

                    if (addresslist[0].ToString().Length > 6)
                    {
                        ReturnValue = 1.0;
                    }
                    else
                    {
                        ReturnValue = -1.0;
                    }
                }
                catch
                {
                    ReturnValue = -1.0;
                }
            }

            Thread.CurrentThread.Abort(); // Never end a thread in the main Update() function.
        }

        internal Measure()
        {
        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            ConnectionType = rm.ReadString("ConnectionType", "INTERNET").ToUpperInvariant();
            UpdateRate = rm.ReadInt("UpdateRate", 20);

            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }

            if (ConnectionType != "NETWORK" && ConnectionType != "INTERNET")
            {
                API.Log(API.LogType.Error, "CheckNet.dll: ConnectionType=" + ConnectionType + " not valid");
            }

        }

        internal double Update()
        {
            if (UpdateCounter == 0)
            {
                if (ConnectionType == "NETWORK" || ConnectionType == "INTERNET")
                {
                    if (_networkThread == null ||_networkThread.ThreadState == ThreadState.Stopped)
                    //We check here to see if all existing instances of the thread have stopped,
                    //and start a new one if so.
                    {
                        _networkThread = new Thread(CheckConnection);
                        _networkThread.Start(ConnectionType);
                    }
                }
            }

            UpdateCounter = UpdateCounter + 1;
            if (UpdateCounter >= UpdateRate)
            {
                UpdateCounter = 0;
            }

            return ReturnValue;
        }

        //internal string GetString()
        //{
        //    return "";
        //}

        //internal void ExecuteBang(string args)
        //{
        //}

        //it is recommended that this Dispose() function be in all examples,
        //and called in Finalize().

        internal static void Dispose()
        {
            if (_networkThread.IsAlive)
                _networkThread.Abort();
        }
    }

    static class Plugin
    {
        static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            Measure.Dispose();
            uint id = (uint)data;
            Measures.Remove(id);
        }

        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Measures.Add(id, new Measure());
        }
        [DllExport]
        public unsafe static void Reload(void* data, void* rm, double* maxValue)
        {
            uint id = (uint)data;
            Measures[id].Reload(new Rainmeter.API((IntPtr)rm), ref *maxValue);
        }

        [DllExport]
        public unsafe static double Update(void* data)
        {
            uint id = (uint)data;
            return Measures[id].Update();
        }

        //[DllExport]
        //public unsafe static char* GetString(void* data)
        //{
        //    uint id = (uint)data;
        //    fixed (char* s = Measures[id].GetString()) return s;
        //}

        //[DllExport]
        //public unsafe static void ExecuteBang(void* data, char* args)
        //{
        //    uint id = (uint)data;
        //    Measures[id].ExecuteBang(new string(args));
        //}
    }

}