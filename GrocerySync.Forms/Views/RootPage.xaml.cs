//
// FirstPage.xaml.cs
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
using System.Collections.Generic;

using Xamarin.Forms;

namespace GrocerySync
{
    public partial class RootPage : ViewModelPage<RootViewModel>
    {
        public RootPage()
        {
            InitializeComponent();
        }

        void AddNewItem(object sender, EventArgs e)
        {
            var model = (RootViewModel)BindingContext;
            model.AddNewItem();
        }

        async void DeleteCell(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var delete = await DisplayAlert("Delete Item", "Are you sure you want to delete these items?", "OK", "Cancel");
            if (delete)
            {
                ((RootViewModel)BindingContext).DeleteItem((TableViewEntry)mi.CommandParameter);
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.RefreshSync();
        }
    }
}

