﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.IO;
using System.Windows.Input;
using TAS.Server;
using TAS.Common;
using TAS.Client.Common;

namespace TAS.Client.ViewModels
{
    public class MediaEditViewmodel : EditViewmodelBase<Server.Media>, IDataErrorInfo
    {
        private readonly PreviewViewmodel _previewVm;
        private readonly bool _showButtons;
        public MediaEditViewmodel(Server.Media media, PreviewViewmodel previewVm, bool showButtons):base(media, new MediaEditView())
        {
            CommandSaveEdit = new UICommand() { ExecuteDelegate = Save, CanExecuteDelegate = o => Modified && IsValid };
            CommandCancelEdit = new UICommand() { ExecuteDelegate = Load, CanExecuteDelegate = o => Modified };
            CommandRefreshStatus = new UICommand() { ExecuteDelegate = _refreshStatus };
            CommandGetTCFromPreview = new UICommand() { ExecuteDelegate = _getTCFromPreview, CanExecuteDelegate = _canGetTCFormPreview };
            CommandCheckVolume = new UICommand() { ExecuteDelegate = _checkVolume, CanExecuteDelegate = (o) => !_isVolumeChecking };
            _previewVm = previewVm;
            _showButtons = showButtons;
            if (previewVm != null)
                previewVm.PropertyChanged += _onPreviewPropertyChanged;
            media.PropertyChanged += OnMediaPropertyChanged;
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= OnMediaPropertyChanged;
            if (_previewVm != null)
                _previewVm.PropertyChanged -= _onPreviewPropertyChanged;
        }

        public ICommand CommandSaveEdit { get; private set; }
        public ICommand CommandCancelEdit { get; private set; }
        public ICommand CommandRefreshStatus { get; private set; }
        public ICommand CommandGetTCFromPreview { get; private set; }
        public ICommand CommandCheckVolume { get; private set; }

        public override void Save(object destObject = null)
        {
            base.Save(destObject);
            if (Model is Server.PersistentMedia)
                ((Server.PersistentMedia)Model).Save();
        }

        public void Revert(object source = null)
        {
            Load(source);
        }

        void _refreshStatus(object o)
        {
            Model.MediaStatus = TMediaStatus.Unknown;
            Model.Verified = false;
            Model.Verify();
        }


        void _getTCFromPreview(object o)
        {
            if (_previewVm != null)
            {
                Server.Media previewMedia = _previewVm.LoadedMedia;
                TCPlay = _previewVm.TCIn;
                DurationPlay = _previewVm.DurationSelection;
            }
        }

        void _checkVolume(object o)
        {
            if (_isVolumeChecking)
                return;
            IsVolumeChecking = true;
            Model.GetLoudness(
                this.TCPlay - this.TCStart,
                this.DurationPlay,
                (obj, e) =>
                {
                    if (((LoudnessOperation)obj).SourceMedia == Model)
                        AudioVolume = e.AudioVolume;
                },
                () =>
                {
                    IsVolumeChecking = false; // finishCallback
                });
        }

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (!string.IsNullOrEmpty(e.PropertyName))
                    {
                        PropertyInfo sourcePi = Model.GetType().GetProperty(e.PropertyName);
                        PropertyInfo destPi = this.GetType().GetProperty(e.PropertyName);
                        if (sourcePi != null
                            && destPi != null
                            && sourcePi.CanRead
                            && destPi.CanWrite)
                        {
                            bool oldModified = Modified;
                            destPi.SetValue(this, sourcePi.GetValue(Model, null), null);
                            Modified = oldModified;
                            NotifyPropertyChanged(e.PropertyName);
                        }
                    }
                }),
            null);

            if (e.PropertyName == "MediaStatus")
            {
                NotifyPropertyChanged(e.PropertyName);
                NotifyPropertyChanged("IsIngestDataShown");
            }
            if (e.PropertyName == "MediaGuid")
            {
                NotifyPropertyChanged(e.PropertyName);
            }
        }

        private void _onPreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LoadedMedia")
                NotifyPropertyChanged("CommandGetTCFromPreview");
        }

        private bool _isVolumeChecking;
        public bool IsVolumeChecking
        {
            get { return _isVolumeChecking; }
            set
            {
                if (_isVolumeChecking != value)
                {
                    _isVolumeChecking = value;
                    NotifyPropertyChanged("IsVolumeChecking");
                    NotifyPropertyChanged("CommandCheckVolume");
                }
            }
        }

        public void Delete()
        {
            if (Model != null)
                Model.Delete();
        }

        public bool ShowButtons { get { return _showButtons; } }

        private string _folder;
        public string Folder { get { return _folder; } set { SetField(ref _folder, value, "Folder"); } }
        
        private string _fileName;
        public string FileName 
        {
            get { return _fileName; }
            set
            {
                if (SetField(ref _fileName, value, "FileName"))
                    NotifyPropertyChanged("IsValid");
            }
        }

        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetField(ref _lastUpdated, value, "LastUpdated"); }
        }

        private DateTime _lastAccess;
        public DateTime LastAccess
        {
            get { return _lastAccess; }
            set { SetField(ref _lastAccess, value, "LastAccess"); }
        }

        private TMediaType _mediaType;
        public TMediaType MediaType
        {
            get { return _mediaType; }
            set { SetField(ref _mediaType, value, "MediaType"); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value, "Duration"); }
        }

        private TimeSpan _durationPlay;
        public TimeSpan DurationPlay
        {
            get { return _durationPlay; }
            set { SetField(ref _durationPlay, value, "DurationPlay"); }
        }

        private TimeSpan _tCStart;
        public TimeSpan TCStart
        {
            get { return _tCStart; }
            set { SetField(ref _tCStart, value, "TCStart"); }
        }

        private TimeSpan _tCPlay;
        public TimeSpan TCPlay
        {
            get { return _tCPlay; }
            set { SetField(ref _tCPlay, value, "TCPlay"); }
        }

        readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }
        private TVideoFormat _videoFormat;
        public TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set { SetField(ref _videoFormat, value, "VideoFormat"); }
        }

        readonly Array _audioChannelMappings = Enum.GetValues(typeof(TAudioChannelMapping)); 
        public Array AudioChannelMappings { get { return _audioChannelMappings;} }
        private TAudioChannelMapping _audioChannelMapping;
        public TAudioChannelMapping AudioChannelMapping
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value, "AudioChannelMapping"); }
        }

        private decimal _audioVolume;
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, "AudioVolume"); }
        }

        private string _mediaName;
        public string MediaName
        {
            get { return _mediaName; }
            set {
                if (SetField(ref _mediaName, value, "MediaName"))
                {
                    if (MediaStatus == TMediaStatus.Required)
                        FileName = Server.FileUtils.SanitizeFileName(value) + Server.MediaDirectory.DefaultFileExtension(MediaType);
                };
            }
        }

        readonly Array _mediaEmphasises = Enum.GetValues(typeof(TMediaEmphasis)); 
        public Array MediaEmphasises { get { return _mediaEmphasises;} }
        private TMediaEmphasis _mediaEmphasis;
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, "MediaEmphasis"); }
        }
        
        private DateTime? _killDate;
        public DateTime? KillDate
        {
            get { return _killDate; }
            set
            {
                if (_killDate != value)
                {
                    if (value == default(DateTime))
                        _killDate = null;
                    else
                        _killDate = value;
                    Modified = true;
                    NotifyPropertyChanged("IsKillDate");
                    NotifyPropertyChanged("KillDate");
                }
            }
        }

        public bool IsKillDate
        {
            get { return _killDate != null; }
            set
            {
                if (value != IsKillDate)
                {
                    if (value)
                        _killDate = DateTime.UtcNow + TimeSpan.FromDays(30);
                    else
                        _killDate = null;
                    Modified = true;
                    NotifyPropertyChanged("KillDate");
                    NotifyPropertyChanged("IsKillDate");
                }
            }
        }

        public TMediaStatus MediaStatus { get { return Model.MediaStatus; }}
        public Guid MediaGuid { get { return Model.MediaGuid; }}


        private bool _doNotArchive;
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value, "DoNotArchive"); }
        }

        readonly Array _parentals = Enum.GetValues(typeof(TParental)); 
        public Array Parentals { get { return _parentals; } }
        private TParental _parental;
        public TParental Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value, "Parental"); }
        }

        readonly Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory)); 
        public Array MediaCategories { get { return _mediaCategories; } }
        private TMediaCategory _mediaCategory;
        public TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value, "MediaCategory"); }
        }

        private string _idAux;
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, "IdAux"); }
        }

        private bool _isAutoSave;
        public bool IsAutoSave
        {
            get { return _isAutoSave; }
            set
            {
                if (SetField(ref _isAutoSave, value, "IsAutoSave"))
                    NotifyPropertyChanged("IsNoAutoSave");
            }
        }

        public bool IsNoAutoSave
        {
            get { return !_isAutoSave; }
        }

        public bool IsPersistentMedia
        {
            get { return Model is Server.PersistentMedia; }
        }

        public bool IsServerMedia
        {
            get { return Model is Server.ServerMedia; }
        }

        public bool IsIngestDataShown
        {
            get
            {
                return (Model is Server.PersistentMedia && Model.MediaStatus != TMediaStatus.Required);
            }
        }

        private bool _canGetTCFormPreview(object o)
        {
            return _previewVm != null
                && _previewVm.LoadedMedia != null
                && Model.MediaGuid.Equals(_previewVm.LoadedMedia.MediaGuid);
        }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case "FileName":
                        validationResult = _validateFileName();
                        break;
                    case "TCPlay":
                        validationResult = _validateTCPlay();
                        break;
                    case "DurationPlay":
                        validationResult = _validateDurationPlay();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateFileName()
        {
            string validationResult = string.Empty;
                var dir = Model.Directory;
                string newName = _fileName;
                if (dir != null && _fileName != null)
                {
                    if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        validationResult = Properties.Resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        newName = newName.ToLowerInvariant();
                        if ((Model.MediaStatus == TMediaStatus.Required || newName != Model.FileName.ToLowerInvariant())
                            && dir.FileExists(newName, Model.Folder))
                            validationResult = Properties.Resources._validate_FileAlreadyExists;
                        else
                            if (Model is Server.PersistentMedia)
                            {
                                if (Model.MediaType == TMediaType.Movie
                                    && !Server.MediaDirectory.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(Properties.Resources._validate_FileMustHaveExtension, string.Join(Properties.Resources._or_, Server.MediaDirectory.VideoFileTypes));
                                if (Model.MediaType == TMediaType.Still
                                    && !Server.MediaDirectory.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(Properties.Resources._validate_FileMustHaveExtension, string.Join(Properties.Resources._or_, Server.MediaDirectory.StillFileTypes));
                            }
                    }
                    //if (dir is ArchiveDirectory)
                    //{
                    //    if (DatabaseConnector.ArchiveFileExists(dir, _fileName))
                    //        validationResult = "Plik o takiej nazwie archiwizowano już w tym miesiącu";
                    //}
                    //else
                    //    if (dir.Files.Where(m => m != media && m.FileName == _fileName).Count() > 0)
                    //        validationResult = "Plik o takiej nazwie już istnieje";
                }
            return validationResult;
        }

        private string _validateTCPlay()
        {
            string validationResult = string.Empty;
            if (TCPlay < TCStart
                || TCPlay > TCStart + Duration)
                validationResult = Properties.Resources._validateStartPlayMustBeInsideFile;
            return validationResult;
        }

        private string _validateDurationPlay()
        {
            string validationResult = string.Empty;
            if (DurationPlay + TCPlay > Duration + TCStart)
                validationResult = Properties.Resources._validate_DurationInvalid;
            return validationResult;
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }
    }


}
