﻿#pragma checksum "C:\Users\Andrew\Sync\Files\Code\vs\Projects\OmegleITM\OmegleITM\ChatPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "A727A035FE93A89A85610CFEC701BFE1"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
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


namespace Omeddle {
    
    
    public partial class ChatPage : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal Microsoft.Phone.Shell.ProgressIndicator TypingIndicator;
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.TextBlock titleBar;
        
        internal System.Windows.Controls.Grid ContentPanel;
        
        internal Microsoft.Phone.Controls.LongListSelector messageLLS;
        
        internal System.Windows.Controls.ContentControl inputPanelControl;
        
        internal System.Windows.Controls.StackPanel inputPanel;
        
        internal Coding4Fun.Toolkit.Controls.ChatBubbleTextBox messageEntryBox;
        
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
            System.Windows.Application.LoadComponent(this, new System.Uri("/OmegleITM;component/ChatPage.xaml", System.UriKind.Relative));
            this.TypingIndicator = ((Microsoft.Phone.Shell.ProgressIndicator)(this.FindName("TypingIndicator")));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.titleBar = ((System.Windows.Controls.TextBlock)(this.FindName("titleBar")));
            this.ContentPanel = ((System.Windows.Controls.Grid)(this.FindName("ContentPanel")));
            this.messageLLS = ((Microsoft.Phone.Controls.LongListSelector)(this.FindName("messageLLS")));
            this.inputPanelControl = ((System.Windows.Controls.ContentControl)(this.FindName("inputPanelControl")));
            this.inputPanel = ((System.Windows.Controls.StackPanel)(this.FindName("inputPanel")));
            this.messageEntryBox = ((Coding4Fun.Toolkit.Controls.ChatBubbleTextBox)(this.FindName("messageEntryBox")));
        }
    }
}
