﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twitch.Twitter.API;

namespace Twitch.Streaming.Statuses
{
    public class Filter : StreamingBase
    {
        #region Events

        #region StatusUpdatedEvent
        public delegate void StatusUpdatedEventHandler(object sender, StatusUpdatedEventArgs e);

        /// <summary>
        /// ツイートが投稿されました。
        /// </summary>
        public event StatusUpdatedEventHandler StatusUpdated;

        /// <summary>
        /// StatusUpdated イベントを発行します。
        /// </summary>
        protected virtual void OnStatusUpdated(StatusUpdatedEventArgs e)
        {
            if (StatusUpdated != null)
                StatusUpdated(this, e);
        }
        #endregion

        #endregion

        /// <summary>
        /// 受信するユーザーのID。
        /// </summary>
        public List<Int64> Follow
        {
            get;
            set;
        }

        /// <summary>
        /// SiteStream を初期化します。
        /// </summary>
        /// <param name="follow">ツイートを受け取るユーザーのユーザーIDのリスト</param>
        public Filter(TwitterContext twitterContext, params string[] follow)
            : base(twitterContext)
        {
            this.StreamMessaged += new StreamMessagedEventHandler(StreamingCallback);

            this.Url = "https://stream.twitter.com/1.1/statuses/filter.json";
            this.Host = "stream.twitter.com";
            this.Method = Methods.POST;

            StringDictionary query = new StringDictionary();

            query["follow"] = string.Join(",", follow);

            this.Parameter = query;
        }

        private void StreamingCallback(object sender, StreamEventArgs e)
        {
            var json = Twitch.Utility.DynamicJson.Parse(e.Data);

            Twitter.Status status;

            try
            {
                status = new Twitter.Status(json.ToString());
            }
            catch
            {
                if (!this.IsTolerance)
                    throw new ApplicationException(
                        "不明なメッセージが発行されました。\"" + json.ToString() + "\"" + Environment.NewLine +
                        "この種の予期しないメッセージに対して例外を発生させない場合は、このStreamの IsTolerance プロパティを true に設定してください。");
                return;
            }

            this.OnStatusUpdated(
                new StatusUpdatedEventArgs
                {
                    Status = status
                }
            );
        }
    }
}
