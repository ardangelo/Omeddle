﻿#pragma checksum "C:\Users\Andrew\SkyDrive\Documents\Visual Studio 2013\Projects\OmegleITM\OmegleITM\LogPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "C2F6D69DA1655BF35D0EB30E9DC46CDA"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace OmegleITM {
    
    
    public partial class LogPage : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal Microsoft.Phone.Controls.Pivot mainPivot;
        
        internal Microsoft.Phone.Controls.LongListSelector RecentChatList;
        
        internal Microsoft.Phone.Controls.LongListSelector SavedChatList;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/OmegleITM;component/LogPage.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.mainPivot = ((Microsoft.Phone.Controls.Pivot)(this.FindName("mainPivot")));
            this.RecentChatList = ((Microsoft.Phone.Controls.LongListSelector)(this.FindName("RecentChatList")));
            this.SavedChatList = ((Microsoft.Phone.Controls.LongListSelector)(this.FindName("SavedChatList")));
        }
    }
}

