//
// ViewModelPage.cs
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
using MvvmCross.Core.ViewModels;
using Xamarin.Forms;

namespace GrocerySync
{
    public abstract class ViewModelPage<T> : ContentPage where T : BaseViewModel
    {
        protected T ViewModel
        {
            get
            {
                return (T)BindingContext;
            }
        }

        public ViewModelPage()
        {
            BindingContextChanged += (sender, e) =>
            {
                var newVal = BindingContext as BaseViewModel;
                if (newVal != null)
                {
                    newVal.ErrorEncountered += ShowError;
                }
            };
        }

        private void ShowError(object sender, ErrorEventArgs args)
        {
            DisplayAlert(args.Title, args.Message, "OK");
        }
    }
}

