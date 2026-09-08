using System;
using System.Collections.Generic;
using System.Text;

namespace FKP41
{
    public enum WatcherStatus
    {
        Unknown = 0,
        Invalid = 1,
        /// <summary>
        /// リクエスト継続可能
        /// </summary>
        Continue = 100,
        /// <summary>
        /// 処理中
        /// </summary>
        Processing = 102,
        /// <summary>
        /// リクエストを受け取ったが処理はされていない
        /// </summary>
        Accepted = 202,
        /// <summary>
        /// 権限が無いファイルやフォルダ
        /// </summary>
        Forbidden = 403,
        /// <summary>
        /// ファイルやフォルダが見つからない
        /// </summary>
        NotFound = 404,
        /// <summary>
        /// ティーポットでコーヒーを淹れようとする試みを拒否
        /// </summary>
        I_am_a_teapot = 418,
    }
}
