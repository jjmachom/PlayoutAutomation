﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Server.Common;

namespace TVPlayClient
{
    public class MainWindowViewmodel : ViewmodelBase
    {
        private const string ConfigurationFileName = "Channels.xml";
        private readonly string _configurationFile;
        private ViewmodelBase _content;
        private bool _showConfigButton = true;

        public MainWindowViewmodel()
        {
            Application.Current.Dispatcher.ShutdownStarted += _dispatcher_ShutdownStarted;
            _configurationFile = Path.Combine(FileUtils.LocalApplicationDataPath, ConfigurationFileName);
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                _loadTabs();
            CommandConfigure = new UICommand { ExecuteDelegate = _configure };
        }

        public ICommand CommandConfigure { get; }

        public ViewmodelBase Content { get { return _content; } private set { SetField(ref _content, value); } }

        public bool ShowConfigButton { get { return _showConfigButton; } private set { SetField(ref _showConfigButton, value); } }

        protected override void OnDispose()
        {
            (_content as ChannelsViewmodel)?.Dispose();
        }

        private void _configure(object obj)
        {
            (_content as ChannelsViewmodel)?.Dispose();
            var vm = new ConfigurationViewmodel(_configurationFile);
            vm.Closed += _configClosed;
            ShowConfigButton = false;
            Content = vm;
        }

        private void _configClosed(object sender, EventArgs e)
        {
            var vm = sender as ConfigurationViewmodel;
            if (vm != null)
            {
                vm.Closed -= _configClosed;
                vm.Dispose();
            }
            ShowConfigButton = true;
            _loadTabs();
        }

        private void _dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Dispose();
        }

        private void _loadTabs()
        {
            if (File.Exists(_configurationFile))
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<ConfigurationChannel>), new XmlRootAttribute("Channels"));
                using (StreamReader file = new StreamReader(_configurationFile))
                    Content = new ChannelsViewmodel((List<ConfigurationChannel>)reader.Deserialize(file));
            }
        }
        
    }
}
