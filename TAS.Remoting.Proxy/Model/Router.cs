﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model
{
    public class Router : ProxyBase, IRouter
    {
        #pragma warning disable CS0649 

        [JsonProperty(nameof(IRouter.InputPorts))]
        private IList<IRouterPort> _inputPorts;

        [JsonProperty(nameof(IRouter.SelectedInputPort))]
        private IRouterPort _selectedInputPort;

        #pragma warning restore

        public IList<IRouterPort> InputPorts { get => _inputPorts; set { Set(value); } }
       
        public IRouterPort SelectedInputPort 
        {
            get => _selectedInputPort;
            set { Set(value); }
        }

        public void SelectInput(int inputId)
        {
            Invoke(parameters: new object[] { inputId });
        }

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
