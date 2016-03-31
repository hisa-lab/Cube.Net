﻿/* ------------------------------------------------------------------------- */
///
/// ClientTest.cs
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
using System.Threading.Tasks;
using NUnit.Framework;

namespace Cube.Tests.Net.News
{
    /* --------------------------------------------------------------------- */
    ///
    /// ClientTest
    ///
    /// <summary>
    /// News.Client のテストを行うためのクラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    [Parallelizable]
    [TestFixture]
    class ClientTest
    {
        /* ----------------------------------------------------------------- */
        ///
        /// GetAsync_Count
        /// 
        /// <summary>
        /// JSON データを非同期で取得するテストを行います。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase(5)]
        public async Task GetAsync_Count(int expected)
        {
            var client = new Cube.Net.News.Client();
            client.Version = new SoftwareVersion
            {
                Number = new Version(1, 0, 0),
                Digit  = 3,
                Suffix = string.Empty
            };

            var results = await client.GetAsync();
            Assert.That(
                results.Count,
                Is.EqualTo(expected)
            );
        }
    }
}
