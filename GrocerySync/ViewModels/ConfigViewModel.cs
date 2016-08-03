//
// ConfigViewModel.cs
//
// Author:
// 	Jim Borden  <jim.borden@couchbase.com>
//
// Copyright (c) 2016 Couchbase, Inc All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Windows.Input;
using GrocerySync.Helpers;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using Xamarin.Forms;

namespace GrocerySync
{
    public class ConfigViewModel : BaseViewModel
    {
        private Uri _syncUrl;
        public string SyncURL
        {
            get
            {
                return _syncUrl?.OriginalString;
            }
            set
            {
                Uri outUri;
                var valid = Uri.TryCreate(value, UriKind.Absolute, out outUri);
                URLIndicatorColor = valid ? Color.Green : Color.Red;
                SetProperty(ref _syncUrl, outUri);
            }
        }

        private Color _urlIndicatorColor;
        public Color URLIndicatorColor
        {
            get
            {
                return _urlIndicatorColor;
            }
            set
            {
                SetProperty(ref _urlIndicatorColor, value);
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return new MvxCommand(Save);
            }
        }

        public ConfigViewModel()
        {
            SyncURL = Settings.SyncURL;
        }

        private void Save()
        {
            Settings.SyncURL = SyncURL;
        }
    }
}

