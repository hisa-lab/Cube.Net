﻿/* ------------------------------------------------------------------------- */
///
/// Copyright (c) 2010 CubeSoft, Inc.
/// 
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///  http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
///
/* ------------------------------------------------------------------------- */
using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Cube.Net.Tests
{
    /* --------------------------------------------------------------------- */
    ///
    /// NtpMonitorTest
    ///
    /// <summary>
    /// Ntp.Monitor のテスト用クラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    [Parallelizable]
    [TestFixture]
    class NtpMonitorTest : NetworkResource
    {
        /* ----------------------------------------------------------------- */
        ///
        /// Start
        /// 
        /// <summary>
        /// NTP サーバを監視するテストを行います。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        [Ignore("NUnit for .NET 3.5 does not support async/await")]
        public void Start()
        {
            var result = TimeSpan.Zero;
            Assert.That(
                async () =>
                {
                    var cts     = new CancellationTokenSource();
                    var monitor = new Ntp.Monitor();
                    monitor.Subscribe(x =>
                    {
                        result = x;
                        cts.Cancel();
                    });
                    monitor.Timeout = TimeSpan.FromSeconds(2);
                    monitor.Start();
                    await TaskEx.Delay((int)(monitor.Timeout.TotalMilliseconds * 2), cts.Token);
                },
                Throws.TypeOf<TaskCanceledException>()
            );
            Assert.That(result, Is.Not.EqualTo(TimeSpan.Zero));
        }
    }
}