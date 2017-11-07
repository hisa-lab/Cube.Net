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
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Cube.Conversions;
using Cube.Log;

namespace Cube.Net.Http
{
    /* --------------------------------------------------------------------- */
    ///
    /// HttpMonitor(TValue)
    ///
    /// <summary>
    /// 定期的に HTTP 通信を実行するためのクラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public class HttpMonitor<TValue> : HttpMonitorBase<TValue>
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// HttpMonitor
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="handler">HTTP 通信用ハンドラ</param>
        ///
        /* ----------------------------------------------------------------- */
        public HttpMonitor(ContentHandler<TValue> handler) : base(handler) { }

        /* ----------------------------------------------------------------- */
        ///
        /// HttpMonitor
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="converter">変換用オブジェクト</param>
        ///
        /* ----------------------------------------------------------------- */
        public HttpMonitor(IContentConverter<TValue> converter)
            : this(new ContentHandler<TValue>(converter)) { }

        /* ----------------------------------------------------------------- */
        ///
        /// HttpMonitor
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="func">変換用オブジェクト</param>
        ///
        /* ----------------------------------------------------------------- */
        public HttpMonitor(Func<HttpContent, Task<TValue>> func)
            : this(new ContentConverter<TValue>(func)) { }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// Uri
        ///
        /// <summary>
        /// Uris の最初の項目を取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public Uri Uri { get; set; }

        /* ----------------------------------------------------------------- */
        ///
        /// Version
        ///
        /// <summary>
        /// アプリケーションのバージョンを取得または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public SoftwareVersion Version { get; set; } = new SoftwareVersion();

        #endregion

        #region Methods

        /* ----------------------------------------------------------------- */
        ///
        /// GetRequestUris
        ///
        /// <summary>
        /// リクエスト送信先 URL 一覧を取得します。
        /// </summary>
        /// 
        /// <returns>URL 一覧</returns>
        /// 
        /* ----------------------------------------------------------------- */
        protected override IEnumerable<Uri> GetRequestUris()
            => new[] { Uri.With(Version) };

        #endregion
    }

    /* --------------------------------------------------------------------- */
    ///
    /// HttpMonitorBase
    ///
    /// <summary>
    /// 定期的に HTTP 通信を実行するための基底クラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public abstract class HttpMonitorBase<TValue> : IDisposable
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// HttpMonitor
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="handler">HTTP 通信用ハンドラ</param>
        ///
        /* ----------------------------------------------------------------- */
        protected HttpMonitorBase(ContentHandler<TValue> handler) : base()
        {
            _dispose = new OnceAction<bool>(Dispose);
            _http    = HttpClientFactory.Create(handler);
            Handler  = handler;

            _core.Subscribe(WhenTick);
        }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// NetworkAvailable
        /// 
        /// <summary>
        /// ネットワークが使用可能な状態かどうかを表す値を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public bool NetworkAvailable => _core.NetworkAvailable;

        /* ----------------------------------------------------------------- */
        ///
        /// State
        /// 
        /// <summary>
        /// オブジェクトの状態を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public TimerState State => _core.State;

        /* ----------------------------------------------------------------- */
        ///
        /// Interval
        /// 
        /// <summary>
        /// 実行間隔を取得または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public TimeSpan Interval
        {
            get => _core.Interval;
            set => _core.Interval = value;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// BurstInterval
        /// 
        /// <summary>
        /// 複数の URL に対して通信を試みる場合、直前の URL が終了して
        /// から次の URL に対してリクエストを送信するまでの間隔を取得
        /// または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public TimeSpan BurstInterval { get; set; } = TimeSpan.FromSeconds(1);

        /* ----------------------------------------------------------------- */
        ///
        /// FailedCount
        /// 
        /// <summary>
        /// サーバとの通信に失敗した回数を取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public int FailedCount { get; protected set; } = 0;

        /* --------------------------------------------------------------------- */
        ///
        /// RetryCount
        /// 
        /// <summary>
        /// 通信失敗時に再試行する最大回数を取得または設定します。
        /// </summary>
        ///
        /* --------------------------------------------------------------------- */
        public int RetryCount { get; set; } = 5;

        /* --------------------------------------------------------------------- */
        ///
        /// RetryInterval
        /// 
        /// <summary>
        /// 通信失敗時に再試行する間隔を取得または設定します。
        /// </summary>
        ///
        /* --------------------------------------------------------------------- */
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(10);

        /* --------------------------------------------------------------------- */
        ///
        /// Timeout
        /// 
        /// <summary>
        /// タイムアウト時間を取得または設定します。
        /// </summary>
        ///
        /* --------------------------------------------------------------------- */
        public TimeSpan Timeout
        {
            get => _http.Timeout;
            set => _http.Timeout = value;
        }

        /* --------------------------------------------------------------------- */
        ///
        /// UserAgent
        /// 
        /// <summary>
        /// ユーザエージェントを取得または設定します。
        /// </summary>
        ///
        /* --------------------------------------------------------------------- */
        public string UserAgent
        {
            get => Handler.UserAgent;
            set => Handler.UserAgent = value;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Handler
        ///
        /// <summary>
        /// HTTP ハンドラを取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        protected ContentHandler<TValue> Handler { get; }

        /* ----------------------------------------------------------------- */
        ///
        /// Subscriptions
        ///
        /// <summary>
        /// 購読者一覧を取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        protected IList<Action<Uri, TValue>> Subscriptions { get; }
            = new List<Action<Uri, TValue>>();

        #endregion

        #region Methods

        /* ----------------------------------------------------------------- */
        ///
        /// Start
        ///
        /// <summary>
        /// 監視を実行します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Start() => Start(TimeSpan.Zero);

        /* ----------------------------------------------------------------- */
        ///
        /// Start
        ///
        /// <summary>
        /// 監視を実行します。
        /// </summary>
        /// 
        /// <param name="delay">初期遅延時間</param>
        /// 
        /* ----------------------------------------------------------------- */
        public void Start(TimeSpan delay)
        {
            var state = _core.State;
            _core.Start(delay);
            if (state != TimerState.Stop) return;
            this.LogDebug($"Start\tInterval:{Interval}\tInitialDelay:{delay}");
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Stop
        ///
        /// <summary>
        /// 監視を停止します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Stop()
        {
            var state = _core.State;
            _core.Stop();
            if (state == TimerState.Stop) return;
            this.LogDebug($"Stop\tLast:{_core.LastExecuted}\tFaild:{FailedCount}");
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Subscribe
        ///
        /// <summary>
        /// データ受信時に実行する処理を登録します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public IDisposable Subscribe(Action<Uri, TValue> action)
        {
            Subscriptions.Add(action);
            return Disposable.Create(() => Subscriptions.Remove(action));
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Reset
        ///
        /// <summary>
        /// リセットします。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public virtual void Reset()
        {
            var current = State;
            _core.Reset();
            if (current == TimerState.Run)
            {
                Stop();
                Start();
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// GetRequestUris
        ///
        /// <summary>
        /// リクエスト送信先 URL 一覧を取得します。
        /// </summary>
        /// 
        /// <returns>URL 一覧</returns>
        /// 
        /* ----------------------------------------------------------------- */
        protected virtual IEnumerable<Uri> GetRequestUris()
            => throw new NotImplementedException();

        /* ----------------------------------------------------------------- */
        ///
        /// Publish
        ///
        /// <summary>
        /// 新しい結果を発行します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        protected virtual void Publish(Uri uri, TValue value)
        {
            foreach (var action in Subscriptions)
            {
                try { action(uri, value); }
                catch (Exception err) { this.LogWarn(err.ToString()); }
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// PublishAsync
        ///
        /// <summary>
        /// HTTP 通信を実行し、変換結果を引数にして Publish メソッドを
        /// 実行します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        protected async Task PublishAsync(Uri uri)
        {
            using (var response = await _http.GetAsync(uri))
            {
                var status = response.StatusCode;
                var code   = (int)status;
                var digit  = code / 100;

                if (response.Content is HttpValueContent<TValue> content) Publish(uri, content.Value); // OK
                else if (digit == 3) this.LogDebug($"HTTP:{code} {status}");
                else if (digit == 4) Fail(uri, $"HTTP:{code} {status}");
                else if (digit == 5) throw new HttpRequestException($"HTTP:{code} {status}");
                else Fail(uri, $"Content is not {nameof(TValue)} ({code})");
            }
        }

        #region IDisposable

        /* ----------------------------------------------------------------- */
        ///
        /// HttpMonitorBase
        ///
        /// <summary>
        /// オブジェクトを破棄します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        ~HttpMonitorBase()
        {
            _dispose.Invoke(false);
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
        public void Dispose()
        {
            _dispose.Invoke(true);
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
        /// <param name="disposing">
        /// マネージリソースを解放するかどうかを示す値
        /// </param>
        /// 
        /* ----------------------------------------------------------------- */
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _http.Dispose();
                Handler.Dispose();
            }
        }

        #endregion

        #endregion

        #region Implementations

        /* ----------------------------------------------------------------- */
        ///
        /// WhenTick
        ///
        /// <summary>
        /// 一定間隔で実行されます。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private async void WhenTick()
        {
            if (State != TimerState.Run || Subscriptions.Count <= 0) return;
            foreach (var uri in GetRequestUris())
            {
                await RetryAsync(uri);
                await Task.Delay(BurstInterval);
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// RetryAsync
        ///
        /// <summary>
        /// 処理を既定回数実行します。
        /// </summary>
        /// 
        /// <remarks>
        /// 処理に成功した時点で終了します。
        /// </remarks>
        /// 
        /* ----------------------------------------------------------------- */
        private async Task RetryAsync(Uri uri)
        {
            for (var i = 0; i < RetryCount; ++i)
            {
                try
                {
                    await PublishAsync(uri);
                    break;
                }
                catch (Exception err)
                {
                    Fail(uri, err.ToString());
                    await Task.Delay(RetryInterval);
                }
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Fail
        ///
        /// <summary>
        /// 通信に失敗した事を伝える処理を実行します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void Fail(Uri uri, string message)
        {
            ++FailedCount;
            this.LogWarn(uri.ToString());
            this.LogWarn(message);
            this.LogWarn($"Failed\tCount:{FailedCount}");
        }

        #region Fields
        private OnceAction<bool> _dispose;
        private HttpClient _http;
        private NetworkAwareTimer _core = new NetworkAwareTimer();
        #endregion

        #endregion
    }
}