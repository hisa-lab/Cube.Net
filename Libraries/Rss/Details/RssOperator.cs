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
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cube.Xml;

namespace Cube.Net.Rss.Parsing
{
    /* --------------------------------------------------------------------- */
    ///
    /// RssOperator
    ///
    /// <summary>
    /// RSS 解析時に使用する拡張メソッドを定義するクラスです。
    /// </summary>
    /// 
    /* --------------------------------------------------------------------- */
    internal static class RssOperator
    {
        #region GetTitle

        /* ----------------------------------------------------------------- */
        ///
        /// GetTitle
        /// 
        /// <summary>
        /// タイトルを取得します。
        /// </summary>
        /// 
        /// <param name="e">XML オブジェクト</param>
        /// 
        /// <returns>タイトル</returns>
        /// 
        /// <remarks>
        /// title タグが存在しない場合、link タグの内容で代替します。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        public static string GetTitle(this XElement e)
            => GetTitle(e, string.Empty);

        /* ----------------------------------------------------------------- */
        ///
        /// GetTitle
        /// 
        /// <summary>
        /// タイトルを取得します。
        /// </summary>
        /// 
        /// <param name="e">XML オブジェクト</param>
        /// <param name="ns">名前空間</param>
        /// 
        /// <returns>タイトル</returns>
        /// 
        /// <remarks>
        /// title タグが存在しない場合、link タグの内容で代替します。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        public static string GetTitle(this XElement e, string ns)
        {
            var title = e.GetValue(ns, "title");
            if (!string.IsNullOrEmpty(title)) return title.Trim();

            var link = e.GetUri(ns, "link");
            return link?.ToString()?.Trim() ?? string.Empty;
        }

        #endregion

        #region Strip

        /* ----------------------------------------------------------------- */
        ///
        /// Strip
        /// 
        /// <summary>
        /// HTML タグを除去する等して文字列を正規化します。
        /// </summary>
        /// 
        /// <param name="src">オリジナルの文字列</param>
        /// <param name="n">最大文字長</param>
        /// 
        /// <returns>正規化後の文字列</returns>
        ///
        /* ----------------------------------------------------------------- */
        public static string Strip(this string src, int n)
        {
            var dest = string.IsNullOrEmpty(src) ? 
                       string.Empty :
                       Regex.Replace(src, "<.*?>", string.Empty).Trim();
            return dest.Length <= n ?
                   dest :
                   dest.Substring(0, n);
        }

        #endregion
    }
}