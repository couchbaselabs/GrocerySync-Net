//
// FirstPageViewModel.cs
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Couchbase.Lite;
using GrocerySync.Helpers;
using MvvmCross.Core.ViewModels;
using Newtonsoft.Json.Linq;
using Xamarin.Forms;

namespace GrocerySync
{
    public sealed class RootViewModel : BaseViewModel
    {
        private enum TableOperation
        {
            Add,
            Delete,
            Update
        }

        private const string DefaultViewName = "byDate";
        internal const string DocumentDisplayPropertyName = "text";
        internal const string CheckboxPropertyName = "check";
        internal const string CreationDatePropertyName = "created_at";
        internal const string DeletedKey = "_deleted";
        internal const string DbName = "grocery-sync";

        private Database _database;
        private LiveQuery _tableQuery;
        private LiveQuery _doneQuery;
        private Replication _push;
        private Replication _pull;
        private Uri _syncURL;
        private TableOperation _currentOperation;

        public ObservableCollection<TableViewEntry> Items { get; } = new ObservableCollection<TableViewEntry>();

        public ICommand CleanItemCommand
        {
            get
            {
                return new MvxCommand(CleanItems);
            }
        } 

        public ICommand ConfigCommand
        {
            get
            {
                return new MvxCommand(() => ShowViewModel<ConfigViewModel>());
            }
        }

        public ImageSource BGImage
        {
            get
            {
                return ImageSource.FromFile("background.jpg");
            }
        }

        private TableViewEntry _selectedItem;
        public TableViewEntry SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _currentOperation = TableOperation.Update;
                value.IsChecked = !value.IsChecked;
                SetProperty(ref _selectedItem, value);
                SetProperty(ref _selectedItem, null); // No "selection" effect
            }
        }

        private string _newItemText;
        public string NewItemText
        {
            get
            {
                return _newItemText ?? String.Empty;
            }
            set
            {
                SetProperty(ref _newItemText, value);
            }
        }

        private string _doneText;
        public string DoneText
        {
            get
            {
                return _doneText ?? String.Empty;
            }
            set
            {
                SetProperty(ref _doneText, value);
            }
        }

        public RootViewModel()
        {
            InitializeDatabase();
            InitializeCouchbaseSummaryView();
            InitializeDatasource();
        }

        public void AddNewItem()
        {
            _currentOperation = TableOperation.Add;
            var value = NewItemText;
            if (String.IsNullOrWhiteSpace(value))
                return;

            var jsonDate = DateTime.UtcNow.ToString("o"); // ISO 8601 date/time format.
            var vals = new Dictionary<String, Object> {
                {DocumentDisplayPropertyName , value},
                {CheckboxPropertyName , false},
                {CreationDatePropertyName , jsonDate}
            };

            var doc = _database.CreateDocument();
            doc.PutProperties(vals);

            NewItemText = String.Empty;
        }

        public void DeleteItem(TableViewEntry entry)
        {
            _currentOperation = TableOperation.Delete;
            entry.DeleteDocument();
        }

        public void RefreshSync()
        {
            var stored = Settings.SyncURL;
            if (String.IsNullOrEmpty(stored))
            {
                _pull?.Stop();
                _push?.Stop();
            }
            else if(stored != _syncURL?.AbsoluteUri)
            {
                _syncURL = new Uri(stored);
                _pull?.Stop();
                _push?.Stop();
                _pull = _database.CreatePullReplication(_syncURL);
                _push = _database.CreatePushReplication(_syncURL);
                _pull.Continuous = true;
                _push.Continuous = true;
                _pull.Start();
                _push.Start();
            }
        }

        private void InitializeDatabase()
        {
            var opts = new DatabaseOptions();

            //To use this feature, add the Couchbase.Lite.Storage.ForestDB nuget package
            //opts.StorageType = StorageEngineTypes.ForestDB;

            // To use this feature, add either the Couchbase.Lite.Storage.SQLCipher nuget package
            // or uncomment the above line and add the Couchbase.Lite.Storage.ForestDB nuget package
            //opts.EncryptionKey = new SymmetricKey("foo");
            opts.Create = true;
            var db = Manager.SharedInstance.OpenDatabase(DbName, opts);
            if (db == null)
                throw new ApplicationException("Could not create database");

            _database = db;
        }

        private Couchbase.Lite.View InitializeCouchbaseView()
        {
            var view = _database.GetView(DefaultViewName);

            var mapBlock = new MapDelegate((doc, emit) =>
            {
                object date;
                doc.TryGetValue(CreationDatePropertyName, out date);

                object deleted;
                doc.TryGetValue(DeletedKey, out deleted);

                if (date != null && deleted == null)
                   emit(date, doc);
            });

            view.SetMap(mapBlock, "1");

            var validationBlock = new ValidateDelegate((revision, context) =>
            {
                if (revision.IsDeletion)
                   return true;

                object date;
                revision.Properties.TryGetValue(CreationDatePropertyName, out date);
                return (date != null);
            });

            _database.SetValidation(CreationDatePropertyName, validationBlock);

            return view;
        }

        private void InitializeDatasource()
        {

            var view = InitializeCouchbaseView();

            _tableQuery = view.CreateQuery().ToLiveQuery();
            _tableQuery.Descending = true;
            _tableQuery.Changed += UpdateTable;
            _tableQuery.Start();

            var doneView = _database.GetView("Done");
            _doneQuery = doneView.CreateQuery().ToLiveQuery();
            _doneQuery.Changed += (sender, e) =>
            {
                string val;

                if (_doneQuery.Rows.Count == 0) {
                    val = String.Empty;
                }  else {
                    var row = _doneQuery.Rows.ElementAt(0);
                    var doc = (IDictionary<string, string>)row.Value;

                    val = String.Format("{0}: {1}", doc["Label"], doc["Count"]);
                }

                DoneText = val;
            };
            _doneQuery.Start();
        }

        private void InitializeCouchbaseSummaryView()
        {
            var view = _database.GetView("Done");

            var mapBlock = new MapDelegate((doc, emit) =>
           {
               object date;
               doc.TryGetValue(CreationDatePropertyName, out date);

               object checkedOff;
               doc.TryGetValue(CheckboxPropertyName, out checkedOff);

               if (date != null)
               {
                   emit(new[] { checkedOff, date }, null);
               }
           });


            var reduceBlock = new ReduceDelegate((keys, values, rereduce) =>
           {
               var key = keys.Sum(data =>
                   1 - (int)(((JArray)data)[0])
               );

               var result = new Dictionary<string, string>
               {
                    {"Label", "Items Remaining"},
                    {"Count", key.ToString ()}
               };

               return result;
           });

            view.SetMapReduce(mapBlock, reduceBlock, "1");
        }

        private void UpdateTable(object sender, QueryChangeEventArgs args)
        {
            if (_currentOperation == TableOperation.Add)
            {
                int i = 0;
                foreach (var row in args.Rows)
                {
                    if (i >= Items.Count)
                    {
                        Items.Add(new TableViewEntry(row.Document));
                    }
                    else if (!Items[i].Matches(row))
                    {
                        Items.Insert(i, new TableViewEntry(row.Document));
                        break;
                    }

                    i += 1;
                }
            }
            else if (_currentOperation == TableOperation.Delete)
            {
                int i = 0;
                foreach (var row in args.Rows)
                {
                    if (!Items[i].Matches(row))
                    {
                        Items.RemoveAt(i);
                        return;
                    }

                    i += 1;
                }

                for (int j = Items.Count - 1; j >= i; j--)
                {
                    Items.RemoveAt(j);
                }
            }
            else
            {
                int i = 0;
                foreach (var row in args.Rows)
                {
                    Items[i++].IsChecked = (bool)row.Document.UserProperties[CheckboxPropertyName];
                }
            }
        }

        private void CleanItems()
        {
            _currentOperation = TableOperation.Delete;
            _database.RunInTransaction(() =>
            {
                foreach (var toDelete in Items.Where(x => x.IsChecked))
                {
                    toDelete.DeleteDocument();
                }

                return true;
            });
        }
    }

    public sealed class TableViewEntry : INotifyPropertyChanged
    {
        private readonly Document _doc;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Label
        {
            get
            {
                return _doc.UserProperties[RootViewModel.DocumentDisplayPropertyName] as string;
            }
        }

        public bool IsChecked
        {
            get
            {
                return (bool)_doc.UserProperties[RootViewModel.CheckboxPropertyName];
            }
            set
            {
                if (IsChecked == value)
                {
                    return;
                }

                _doc.Update(rev =>
                {
                    var props = rev.UserProperties;
                    props[RootViewModel.CheckboxPropertyName] = value;
                    rev.SetUserProperties(props);
                    return true;
                });

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckboxImage"));
            }
        }

        public ImageSource BackgroundImage
        {
            get
            {
                return ImageSource.FromFile("item_background.png");
            }
        }

        public ImageSource CheckboxImage
        {
            get
            {
                return IsChecked ?
                    ImageSource.FromFile("list_area___checkbox___checked.png") :
                    ImageSource.FromFile("list_area___checkbox___unchecked.png");
            }
        }

        public TableViewEntry(Document doc)
        {
            _doc = doc;
        }

        public bool Matches(QueryRow row)
        {
            return row.Document.Id == _doc.Id;
        }

        public void DeleteDocument()
        {
            _doc.Delete();
        }
 
    }
}

