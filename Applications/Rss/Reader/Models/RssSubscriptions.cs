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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Cube.FileSystem;
using Cube.Net.Rss;
using Cube.Settings;
using Cube.Xui;

namespace Cube.Net.App.Rss.Reader
{
    /* --------------------------------------------------------------------- */
    ///
    /// RssSubscriptions
    ///
    /// <summary>
    /// 購読フィード一覧を管理するクラスです。
    /// </summary>
    /// 
    /* --------------------------------------------------------------------- */
    public sealed class RssSubscriptions
        : IEnumerable<RssEntryBase>, INotifyCollectionChanged, IDisposable
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// RssSubscribeCollection
        /// 
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public RssSubscriptions()
        {
            _monitor = new RssMonitor(_feeds) { Interval = TimeSpan.FromHours(1) };

            _items.Synchronously = true;
            _items.CollectionChanged += (s, e) => CollectionChanged?.Invoke(this, e);
        }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// IO
        /// 
        /// <summary>
        /// 入出力用のオブジェクトを取得または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public Operator IO
        {
            get => _feeds.IO;
            set => _feeds.IO = value;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// CacheDirectory
        /// 
        /// <summary>
        /// キャッシュ用ディレクトリのパスを取得または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public string CacheDirectory
        {
            get => _feeds.Directory;
            set => _feeds.Directory = value;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Categories
        /// 
        /// <summary>
        /// カテゴリ一覧を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public IEnumerable<RssCategory> Categories => this.OfType<RssCategory>();

        /* ----------------------------------------------------------------- */
        ///
        /// Entries
        /// 
        /// <summary>
        /// どのカテゴリにも属さないエントリ一覧を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public IEnumerable<RssEntry> Entries => this.OfType<RssEntry>();

        #endregion

        #region Events

        /* ----------------------------------------------------------------- */
        ///
        /// CollectionChanged
        /// 
        /// <summary>
        /// コレクション変更時に発生するイベントです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /* ----------------------------------------------------------------- */
        ///
        /// SubCollectionChanged
        /// 
        /// <summary>
        /// サブコレクション変更時に発生するイベントです。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public event EventHandler SubCollectionChanged;

        #endregion

        #region Methods

        /* ----------------------------------------------------------------- */
        ///
        /// Register
        /// 
        /// <summary>
        /// 新規 URL を登録します。
        /// </summary>
        /// 
        /// <param name="uri">URL オブジェクト</param>
        /// 
        /* ----------------------------------------------------------------- */
        public Task Register(Uri uri)
        {
            return Task.FromResult(0);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Remove
        /// 
        /// <summary>
        /// RSS フィードを削除します。
        /// </summary>
        /// 
        /// <param name="item">削除する RSS フィード</param>
        ///
        /* ----------------------------------------------------------------- */
        public void Remove(object item)
        {
            if (item is RssEntry entry)
            {
                var parent = entry.Parent;
                if (parent != null)
                {
                    var prev = parent.Items.Count;
                    parent.Items.Remove(entry);
                }
                else _items.Remove(entry);
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Move
        /// 
        /// <summary>
        /// 項目を移動します。
        /// </summary>
        /// 
        /// <param name="src">移動元の項目</param>
        /// <param name="dest">移動先のカテゴリ</param>
        /// <param name="index">カテゴリ中の挿入場所</param>
        ///
        /* ----------------------------------------------------------------- */
        public void Move(RssEntryBase src, RssEntryBase dest, int index)
        {
            if (src.Parent is RssCategory c) c.Items.Remove(src);
            else _items.Remove(src);

            src.Parent = dest as RssCategory;
            var items = src.Parent?.Items ?? _items;
            if (index < 0 || index >= items.Count) items.Add(src);
            else items.Insert(index, src);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Clear
        /// 
        /// <summary>
        /// コレクションの内容をクリアします。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Clear()
        {
            _monitor.Stop();
            if (_items.Count > 0) _items.Clear();
            _feeds.Clear();
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Load
        /// 
        /// <summary>
        /// JSON ファイルを読み込みます。
        /// </summary>
        /// 
        /// <param name="json">JSON ファイルのパス</param>
        ///
        /* ----------------------------------------------------------------- */
        public void Load(string json)
        {
            Clear();

            using (var s = IO.OpenRead(json))
            {
                var src = SettingsType.Json.Load<List<RssCategory.Json>>(s);
                foreach (var item in src.Select(e => e.Convert(null)))
                {
                    MakeFeed(item);
                    if (!string.IsNullOrEmpty(item.Title)) _items.Add(item);
                    else foreach (var entry in item.Items)
                    {
                        System.Diagnostics.Debug.Assert(entry is RssEntry);
                        entry.Parent = null;
                        _items.Add(entry);
                    }
                }
            }

            _monitor.Start();
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Save
        /// 
        /// <summary>
        /// JSON ファイルに保存します。
        /// </summary>
        /// 
        /// <param name="json">JSON ファイルのパス</param>
        ///
        /* ----------------------------------------------------------------- */
        public void Save(string json)
        {
            using (var s = IO.Create(json))
            {
                var root = new RssCategory
                {
                    Title = string.Empty,
                    Items = new BindableCollection<RssEntryBase>(_items.OfType<RssEntry>()),
                };

                var data = _items
                    .OfType<RssCategory>()
                    .Concat(new[] { root })
                    .Select(e => new RssCategory.Json(e));

                SettingsType.Json.Save(s, data);
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Lookup
        /// 
        /// <summary>
        /// URL に対応する RSS フィードを取得します。
        /// </summary>
        /// 
        /// <param name="uri">URL</param>
        /// 
        /// <returns>RssFeed オブジェクト</returns>
        ///
        /* ----------------------------------------------------------------- */
        public RssFeed Lookup(Uri uri) => _feeds.ContainsKey(uri) ? _feeds[uri] : null;

        #region IEnumerable

        /* ----------------------------------------------------------------- */
        ///
        /// GetEnumerator
        /// 
        /// <summary>
        /// 反復用オブジェクトを取得します。
        /// </summary>
        /// 
        /// <returns>反復用オブジェクト</returns>
        /// 
        /// <remarks>
        /// GetEnumerator() メソッドは無名カテゴリの場合、カテゴリ中の
        /// エントリーを直接返します。全てのカテゴリを取得する場合は
        /// Categories プロパティを利用して下さい。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        public IEnumerator<RssEntryBase> GetEnumerator() => _items.GetEnumerator();

        /* ----------------------------------------------------------------- */
        ///
        /// GetEnumerator
        /// 
        /// <summary>
        /// 反復用オブジェクトを取得します。
        /// </summary>
        /// 
        /// <returns>反復用オブジェクト</returns>
        /// 
        /* ----------------------------------------------------------------- */
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IDisposable

        /* ----------------------------------------------------------------- */
        ///
        /// ~RssSubscribeCollection
        /// 
        /// <summary>
        /// オブジェクトを破棄します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        ~RssSubscriptions() { Dispose(false); }

        /* ----------------------------------------------------------------- */
        ///
        /// Dispose
        /// 
        /// <summary>
        /// リソースを解放します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Dispose
        /// 
        /// <summary>
        /// リソースを解放します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            if (disposing) _monitor.Dispose();
            _feeds.Save();
        }

        #endregion

        #endregion

        #region Implementations

        /* ----------------------------------------------------------------- */
        ///
        /// MakeFeed
        /// 
        /// <summary>
        /// RSS フィード用のオブジェクトを初期化します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void MakeFeed(RssCategory src)
        {
            src.PropertyChanged -= WhenPropertyChanged;
            src.PropertyChanged += WhenPropertyChanged;
            src.Items.CollectionChanged += (s, e) =>
            {
                src.Count = src.Items.Aggregate(0, (x, i) => x + i.Count);
                SubCollectionChanged?.Invoke(this, e);
            };

            foreach (var entry in src.Entries)
            {
                if (_feeds.ContainsKey(entry.Uri)) continue;

                var items = new BindableCollection<RssItem>();
                items.CollectionChanged += (s, e) => entry.Count = _feeds[entry.Uri].UnreadItems.Count();

                _feeds.Add(entry.Uri, new RssFeed
                {
                    Title = entry.Title,
                    Uri   = entry.Uri,
                    Items = items,
                });

                entry.PropertyChanged -= WhenPropertyChanged;
                entry.PropertyChanged += WhenPropertyChanged;
                entry.Count = _feeds[entry.Uri].UnreadItems.Count();
            }

            if (src.Categories == null) return;
            foreach (var category in src.Categories) MakeFeed(category);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// WhenPropertyChanged
        /// 
        /// <summary>
        /// RssEntryBase のプロパティ変更時に実行されるハンドラです。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void WhenPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(RssEntryBase.Count)) return;
            UpdateCount(sender as RssEntryBase);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// UpdateCount
        /// 
        /// <summary>
        /// 未読記事数を更新します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void UpdateCount(RssEntryBase src)
        {
            if (src == null || src.Parent == null) return;
            var parent = src.Parent;
            parent.Count = parent.Items.Aggregate(0, (x, e) => x + e.Count);
        }

        #region Fields
        private bool _disposed = false;
        private BindableCollection<RssEntryBase> _items = new BindableCollection<RssEntryBase>();
        private RssCacheCollection _feeds = new RssCacheCollection();
        private RssMonitor _monitor;
        #endregion

        #endregion
    }
}
