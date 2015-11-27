﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public abstract class ProxyBase: IDto, INotifyPropertyChanged
    {
        [JsonProperty]
        public Guid DtoGuid { get; set; }
        IRemoteClient _client;
        internal void SetClient(IRemoteClient client)
        {
            if (_client != null)
                return;
            client.EventNotification += _onEventNotificationMessage;
            _client = client;
            Debug.WriteLine(this, "Client assigned");
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            object result;
            if (_properties.TryGetValue(propertyName, out result))
                return (T)result;

            var client = _client;
            if (client != null)
            {
                result = client.Get<T>(this, propertyName);
                _properties[propertyName] = result;
                return (T)result;
            }
            return default(T);
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var client = _client;
            if (SetField(value, propertyName))
            {
                if (client != null)
                    client.Set(this, value, propertyName);
            }
        }

        protected void Invoke([CallerMemberName] string methodName = null, params object[] parameters)
        {
            var client = _client;
            if (client != null)
                client.Invoke(this, methodName, parameters);
        }
        protected T Query<T> ([CallerMemberName] string methodName = "", params object[] parameters)
        {
            var client = _client;
            if (client != null)
                return client.Query<T>(this, methodName, parameters);
            return default(T);
        }

        protected void EventAdd<T>(T handler, [CallerMemberName] string eventName = null)
        {
            if (handler == null)
            {
                var client = _client;
                if (client != null)
                {
                    client.EventAdd(this, eventName);
                }
            }
        }

        protected void EventRemove<T>(T handler, [CallerMemberName] string eventName = null)
        {
            if (handler == null)
            {
                var client = _client;
                if (client != null)
                    client.EventRemove(this, eventName);
            }
        }

        protected bool SetField(object value, string propertyName)
        {
            object oldValue;
            if (!_properties.TryGetValue(propertyName, out oldValue) || !oldValue.Equals(value))
            {
                _properties[propertyName] = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }
            return false; 
        }

        void _onEventNotificationMessage(object sender, WebSocketMessageEventArgs e)
        {
            if (e.Message.DtoGuid == DtoGuid)
            {
                Debug.WriteLine("OnMessage received {0}:{1}", this, e.Message.MemberName);
                if (e.Message.MemberName == "PropertyChanged")
                {
                    PropertyChangedEventArgs ea = (sender as IRemoteClient).Deserialize<PropertyChangedEventArgs>(e.Message.Response);
                    NotifyPropertyChanged(ea.PropertyName);
                    object o;
                    _properties.TryRemove(ea.PropertyName, out o);
                }
                else OnEventNotification(e);
            }
        }

        protected virtual void OnEventNotification(WebSocketMessageEventArgs e)
        {
            Debug.WriteLine(this, e.ToString());
        }

        protected virtual void OnEventRegistration(WebSocketMessageEventArgs e)
        {

        }


        protected T ConvertEventArgs<T>(WebSocketMessageEventArgs e) where T : EventArgs
        {
            T value = default(T);
            var client = _client;
            if (client != null)
                value = client.Deserialize<T>(e.Message.Response);
            return value;
        }

        void NotifyPropertyChanged(string propertyName)
        {
            var h = _propertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(propertyName));
        }

        private ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        private event PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                EventAdd(_propertyChanged);
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
                EventRemove(_propertyChanged);
            }
        }


    }
}
