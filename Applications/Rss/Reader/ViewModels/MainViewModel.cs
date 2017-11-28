﻿/* ------------------------------------------------------------------------- */
//
// Copyright (c) 2010 CubeSoft, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
/* ------------------------------------------------------------------------- */
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Cube.Net.Rss;
using Cube.Xui;
using Cube.Xui.Behaviors;

namespace Cube.Net.App.Rss.Reader
{
    /* --------------------------------------------------------------------- */
    ///
    /// MainViewModel
    ///
    /// <summary>
    /// メイン画面とモデルを関連付けるための ViewModel です。
    /// </summary>
    /// 
    /* --------------------------------------------------------------------- */
    public class MainViewModel : ViewModelBase
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// MainViewModel
        /// 
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public MainViewModel() : this(new SettingsFolder()) { }

        /* ----------------------------------------------------------------- */
        ///
        /// MainViewModel
        /// 
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public MainViewModel(SettingsFolder settings)
            : base(GalaSoft.MvvmLight.Messaging.Messenger.Default)
        {
            _model = new RssFacade(settings);
        }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// Items
        /// 
        /// <summary>
        /// RSS フィード購読サイトおよびカテゴリ一覧を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public IEnumerable<RssEntryBase> Items => _model.Items;

        /* ----------------------------------------------------------------- */
        ///
        /// Feed
        /// 
        /// <summary>
        /// 対象となる Web サイトの RSS フィードを取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public Bindable<RssFeed> Feed { get; } = new Bindable<RssFeed>();

        /* ----------------------------------------------------------------- */
        ///
        /// Content
        /// 
        /// <summary>
        /// 対象とする記事の内容を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public Bindable<string> Content { get; } = new Bindable<string>();

        /* ----------------------------------------------------------------- */
        ///
        /// Messenger
        /// 
        /// <summary>
        /// Messenger オブジェクトを取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public IMessenger Messenger => MessengerInstance;

        #endregion

        #region Commands

        /* ----------------------------------------------------------------- */
        ///
        /// Add
        /// 
        /// <summary>
        /// フィード追加時に実行されるコマンドです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public RelayCommand Add
            => _add = _add ?? new RelayCommand(
                () => Messenger.Send(new AddViewModel(e => _model.Add(e)))
            );

        /* ----------------------------------------------------------------- */
        ///
        /// Remove
        /// 
        /// <summary>
        /// フィード削除時に実行されるコマンドです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public RelayCommand<object> Remove
            => _remove = _remove ?? new RelayCommand<object>(
                e => _model.Remove(e)
            );

        /* ----------------------------------------------------------------- */
        ///
        /// Refresh
        /// 
        /// <summary>
        /// RSS フィード更新時に実行されるコマンドです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public RelayCommand Refresh
            => _refresh = _refresh ?? new RelayCommand(
                () => { },
                () => false
            );

        /* ----------------------------------------------------------------- */
        ///
        /// SelectEntry
        /// 
        /// <summary>
        /// RssEntry 選択時に実行されるコマンドです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public RelayCommand<object> SelectEntry
            => _selectEntry = _selectEntry ?? new RelayCommand<object>(
                e =>
                {
                    _model.Cache(Feed.Value);
                    Feed.Value = _model.Lookup(e as RssEntry);
                },
                e => e is RssEntry
            );

        /* ----------------------------------------------------------------- */
        ///
        /// SelectArticle
        /// 
        /// <summary>
        /// RssArticle 選択時に実行されるコマンドです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public RelayCommand<SelectionList> SelectArticle
            => _selectArticle = _selectArticle ?? new RelayCommand<SelectionList>(
                e => Content.Value = _model.Read(e.SelectedItem as RssItem),
                e => e.SelectedItem is RssItem
            );

        #endregion

        #region Fields
        private RssFacade _model;
        private RelayCommand _add;
        private RelayCommand<object> _remove;
        private RelayCommand _refresh;
        private RelayCommand<object> _selectEntry;
        private RelayCommand<SelectionList> _selectArticle;
        #endregion
    }
}
