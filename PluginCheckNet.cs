using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using Rainmeter;

namespace PluginCheckNet
{

    internal class Measure
    {
        public string ConnectionType;
        public string Type;
        public double Value;
        public int UpdateRate;
        public int UpdateCounter = 0;

        internal Measure()
        {
        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            ConnectionType = rm.ReadString("ConnectionType", "Internet");
            UpdateRate = rm.ReadInt("UpdateRate", 20);
            if (UpdateRate <= 0)
            {
                UpdateRate = 20;
            }
            
            switch (ConnectionType.ToLowerInvariant())
            {
                case "network":
                    Type = "1";
                    break;
                case "internet":
                    Type = "2";
                    break;
                default:
                    API.Log(API.LogType.Error, "CheckNet.dll: ConnectionType=" + ConnectionType + " not valid");
                    break;
            }

        }

        internal double Update()
        {
            if (UpdateCounter == 0)
            {
                switch (Type)
                {
                    case "1":
                        if (System.Convert.ToDouble(NetworkInterface.GetIsNetworkAvailable()) == 0)
                        {
                            Value = -1.0;
                            break;
                        }
                        else
                        {
                            Value = 1.0;
                            break;
                        }

                    case "2":
                        try
                        {
                            IPAddress[] addresslist = Dns.GetHostAddresses("www.msftncsi.com");

                            if (addresslist[0].ToString().Length > 6)
                            {
                                Value = 1.0;
                                break;
                            }
                            else
                            {
                                Value = -1.0;
                                break;
                            }
                        }
                        catch
                        {
                            Value = -1.0;
                            break;
                        }

                    default:
                        Value = 0.0;
                        break;
                }
                
            }

            UpdateCounter = UpdateCounter + 1;
            if (UpdateCounter >= UpdateRate)
            {
                UpdateCounter = 0;
            }

            return Value;
        
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
