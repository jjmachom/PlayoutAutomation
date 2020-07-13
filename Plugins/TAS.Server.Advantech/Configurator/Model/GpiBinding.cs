﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using jNet.RPC.Server;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.Advantech.Configurator.Model
{
    public class GpiBinding : ServerObjectBase, IGpi, IPlugin
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _isTriggered;

        [Hibernate]
        public byte DeviceId { get; set; }
        [Hibernate]
        public int PortNumber { get; set; }
        [Hibernate]
        public byte PinNumber { get; set; }
        public bool IsTriggered 
        {
            get => _isTriggered;
            set
            {
                if (!(SetField(ref _isTriggered, value)))
                    return;

                if (!value)
                    return;

                Task.Run(async() => 
                {
                    await Task.Delay(250);
                    IsTriggered = false;
                    Logger.Trace("Realeasing trigger");
                });                
            }
        }

        public GpiBinding(byte deviceId, int portNumber, byte pinNumber)
        {
            DeviceId = deviceId;
            PortNumber = portNumber;
            PinNumber = pinNumber;
        }        

        internal void NotifyChange(byte deviceId, byte port, byte pin, bool newValue)
        {
            if (!newValue)
                return;
                        
            if (deviceId != DeviceId || port != PortNumber || pin != PinNumber)
                return;

            IsTriggered = true;
            Logger.Trace("Advantech pin triggered");

            Logger.Debug("Advantech device {0} notification port {1} bit {2}", deviceId, port, pin);
            Started?.Invoke(this, EventArgs.Empty);            
        }
                       
        [Hibernate]
        public bool IsEnabled { get; set; }
        public event EventHandler Started;
    }
}
