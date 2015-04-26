﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using ADOTabular.AdomdClientWrappers;
using Caliburn.Micro;
using DaxStudio.UI.Events;
using DaxStudio.UI.Model;
using Microsoft.AnalysisServices;
using Serilog;
using DaxStudio.Interfaces;
using DaxStudio.UI.Interfaces;
using DaxStudio.QueryTrace;

namespace DaxStudio.UI.ViewModels
{
    [InheritedExport(typeof(ITraceWatcher)), PartCreationPolicy(CreationPolicy.NonShared)]
    public abstract class TraceWatcherBaseViewModel 
        : PropertyChangedBase
        , IToolWindow
        , ITraceWatcher
        , IHandle<DocumentConnectionUpdateEvent>
        , IHandle<QueryStartedEvent>
        , IHandle<CancelQueryEvent>
        //, IHandle<QueryTraceCompletedEvent>
    {
        private List<DaxStudioTraceEventArgs> _events;
        private readonly IEventAggregator _eventAggregator;

        [ImportingConstructor]
        protected TraceWatcherBaseViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            WaitForEvent = TraceEventClass.QueryEnd;
            Init();
            //_eventAggregator.Subscribe(this); 
        }

        private void Init()
        {
            MonitoredEvents = GetMonitoredEvents();
        }

        public List<TraceEventClass> MonitoredEvents { get; private set; }
        public TraceEventClass WaitForEvent { get; set; }

        // this is a list of the events captured by this trace watcher
        public List<DaxStudioTraceEventArgs> Events
        {
            get { return _events ?? (_events = new List<DaxStudioTraceEventArgs>()); }
        }

        protected abstract List<TraceEventClass> GetMonitoredEvents();

        // This method is called after the WaitForEvent is seen (usually the QueryEnd event)
        // This is where you can do any processing of the events before displaying them to the UI
        protected abstract void ProcessResults();

        public void ProcessAllEvents(IList<DaxStudioTraceEventArgs> capturedEvents)
        {
            foreach (var e in capturedEvents)
            {
                if (MonitoredEvents.Contains((TraceEventClass)e.EventClass))
                {
                    Events.Add(e);
                }
            }
            ProcessResults();
            IsBusy = false;
        }

        // This method is called before a trace starts which gives you a chance to 
        // reset any stored state
        public void Reset()
        {
            Events.Clear();
            OnReset();
        }

        public abstract void OnReset();
       
        // IToolWindow interface
        public abstract string Title { get; set; }

        public abstract string ToolTipText { get; set; }

        public virtual string DefaultDockingPane
        {
            get { return "DockBottom"; }
            set { }
        }

        public bool CanClose
        {
            get { return false; }
            set { }
        }
        public bool CanHide
        {
            get { return false; }
            set { }
        }
        public int AutoHideMinHeight { get; set; }
        public bool IsSelected { get; set; }

        private bool _isEnabled ;
        public bool IsEnabled { get { return _isEnabled; }
            set { _isEnabled = value;
            NotifyOfPropertyChange("IsEnabled");} 
        }

        public bool IsActive { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyOfPropertyChange(() => IsChecked);
                    _eventAggregator.PublishOnUIThread(new TraceWatcherToggleEvent(this, value));
                    Log.Verbose("{Class} {Event} IsChecked:{IsChecked}", "TraceWatcherBaseViewModel", "IsChecked", value);
                }
            }
        }

        public void Handle(DocumentConnectionUpdateEvent message)
        {
            CheckEnabled(message.Connection);
        }

        public void CheckEnabled(IConnection _connection)
        {
            if (_connection == null) {
                IsEnabled = false;
                IsChecked = false;
                return; 
            }
            if (!_connection.IsConnected)
            {
                // if connection has been closed or broken then uncheck and disable
                IsEnabled = false;
                IsChecked = false;
                return;
            }

            //IsEnabled = (!_connection.IsPowerPivot && _connection.IsAdminConnection && _connection.IsConnected);
            IsEnabled = (_connection.IsAdminConnection && _connection.IsConnected);
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value;
            NotifyOfPropertyChange(() => IsBusy);
            }
        }

        public void Handle(QueryStartedEvent message)
        {
            IsBusy = true;
            Reset();
        }

        public void Handle(CancelQueryEvent message)
        {
            IsBusy = false;
            Reset();
        }

    }
}