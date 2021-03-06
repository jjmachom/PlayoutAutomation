﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class GpisViewModel : ViewModelBase, IPluginManager
    {
        private IConfigEngine _engine;
        public event EventHandler PluginChanged;

        private List<IPluginConfigurator> _configurators = new List<IPluginConfigurator>();
        private IPluginConfigurator _selectedConfigurator;

        private bool? _isEnabled = null;

        public GpisViewModel(IConfigEngine engine)
        {
            _engine = engine;
            Configurators = CollectionViewSource.GetDefaultView(_configurators);

            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), PluginsViewModel.FileNameSearchPattern))
            {
                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeExportedValue("Engine", _engine);
                    var pluginConfigurators = container.GetExportedValues<IPluginConfigurator>().Where(configurator => configurator.GetModel() is IGpi);
                    
                    foreach (var pluginConfigurator in pluginConfigurators)
                    {                       
                        pluginConfigurator.PluginChanged += PluginConfigurator_PluginChanged;                        
                        pluginConfigurator.Initialize(_engine.Gpis?.FirstOrDefault(g => g?.GetType() == pluginConfigurator.GetModel().GetType()));
                        _configurators.Add(pluginConfigurator);
                    }                    
                }
            }            
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {            
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            foreach (var configurator in _configurators)
                configurator.Save();
        }

        public ICollectionView Configurators { get; }
        public string Name => "GPI";
        public List<IGpi> Gpis => _configurators.Select(c => c.GetModel()).Cast<IGpi>().ToList();

        public IPluginConfigurator SelectedConfigurator
        {
            get => _selectedConfigurator;
            set
            {
                if (!SetField(ref _selectedConfigurator, value))
                    return;

                //_isEnabled = _selectedConfigurator?.IsEnabled ?? false;
                //NotifyPropertyChanged(nameof(IsEnabled));                
            }
        }

        public bool? IsEnabled
        {
            get => _isEnabled;
            set
            {
                //if (!SetField(ref _isEnabled, value))
                //    return;

                //if (value == null)
                //    return;

                //foreach (var configurator in _configurators)
                //    configurator.IsEnabled = (bool)value;
            }
        }


        protected override void OnDispose()
        {
            foreach (var configurator in _configurators)
            {
                configurator.PluginChanged -= PluginConfigurator_PluginChanged;
                configurator.Dispose();                
            }
        }
    }
}
