using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Threading;
using Rainmeter;

namespace PluginCheckNet
{

    internal class Measure
    {
        public string ConnectionType;
        public double ReturnValue;
        public int UpdateRate;
        public int UpdateCounter;
        public IntPtr SkinHandle;
        public string FinishAction;
        public Thread ConnectionThread;

        
        private void CheckConnection(string CurrentType)
        {
            if (CurrentType == "NETWORK" || CurrentType == "INTERNET")
            {
                if (System.Convert.ToDouble(NetworkInterface.GetIsNetworkAvailable()) == 0)
                {
                    ReturnValue = -1.0;
                }
                else
                {
                    ReturnValue = 1.0;
                }
            }

            if (ReturnValue == 1.0 && CurrentType == "INTERNET")
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
            
            if (!String.IsNullOrEmpty(FinishAction))
            {
                API.Execute(SkinHandle, FinishAction);
            }
        }

        internal Measure()
        {
        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            SkinHandle = rm.GetSkin();
		    FinishAction = rm.ReadString("FinishAction", "");
		    ConnectionType = rm.ReadString("ConnectionType", "INTERNET").ToUpperInvariant();
            if (ConnectionType != "NETWORK" && ConnectionType != "INTERNET")
            {
                API.Log(API.LogType.Error, "CheckNet.dll: ConnectionType=" + ConnectionType + " not valid");
            }
            
            UpdateRate = rm.ReadInt("UpdateRate", 20);
            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }
        }

        internal double Update()
        {
            if (UpdateCounter == 0)
            {
                if (ConnectionThread == null || ConnectionThread.ThreadState == ThreadState.Stopped)
                {
                    ConnectionThread = new Thread(() =>
                    {
                        try
                        {
                            CheckConnection(ConnectionType);
                        }
                        catch (OperationCanceledException) { }
                    });

                    ConnectionThread.Start();
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
    }

    public static class Plugin
    {
        [DllExport]
        public unsafe static void Initialize(void** data, void* rm)
        {
            uint id = (uint)((void*)*data);
            Measures.Add(id, new Measure());
        }

        [DllExport]
        public unsafe static void Finalize(void* data)
        {
            uint id = (uint)data;
            Measures.Remove(id);
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

        internal static Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();
    }
}