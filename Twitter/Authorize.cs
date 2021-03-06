﻿using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Twitch.Twitter
{
    /// <summary>
    /// OAuthを利用した一連のアカウント認証処理を行うクラスです。
    /// </summary>
    public class Authorize
    {
        /// <summary>
        /// アプリケーションのConsumerKey。
        /// </summary>
        private string ConsumerKey
        {
            get;
            set;
        }

        /// <summary>
        /// アプリケーションのConsumerSecret。
        /// </summary>
        private string ConsumerSecret
        {
            get;
            set;
        }

        /// <summary>
        /// RequestToken
        /// </summary>
        private string OAuthToken
        {
            get;
            set;
        }

        /// <summary>
        /// RequestTokenSecret
        /// </summary>
        private string OAuthTokenSecret
        {
            get;
            set;
        }

        public Authorize(string consumerKey, string consumerSecret)
        {
            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
        }

        /// <summary>
        /// RequestToken,RequestTokenSecretを取得します。
        /// </summary>
        /// <returns>正常に取得できた場合はtrueを、それ以外の場合はfalseを返します。</returns>
        public async Task<bool> GetRequestToken()
        {
            var tw = new Twitch.TwitterContext(this.ConsumerKey, this.ConsumerSecret);

            //try
            //{
            string res = await Twitch.Twitter.APIs.REST.Oauth.RequestToken(tw);

            if (!string.IsNullOrEmpty(res))
            {
                this.OAuthToken = Utility.AnalyzeUrlQuery.Analyze(res, "oauth_token");
                this.OAuthTokenSecret = Utility.AnalyzeUrlQuery.Analyze(res, "oauth_token_secret");

                return true;
            }
            else
                return false;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        /// <summary>
        /// Authorize URLを取得します。
        /// </summary>
        /// <returns>URL</returns>
        public Uri GetAuthorizeUrl()
        {
            if (this.OAuthToken != null)
                return new Uri(Twitch.Twitter.API.Urls.Oauth_Authorize + "?oauth_token=" + this.OAuthToken);
            else
                throw new NullReferenceException("リクエスト トークンが設定されていません。");
        }

        /// <summary>
        /// Authorizeページを既定のウェブ ブラウザーで表示します。
        /// </summary>
        public void ShowAuthorizeBrowser()
        {
            Uri url = GetAuthorizeUrl();

            System.Diagnostics.Process.Start(url.ToString());
        }

        /// <summary>
        /// 認証ページをスクレイピングし、ScreenName,PasswordからAccessToken,AccessTokenSecretを取得します。<para />
        /// ただし、このメソッドは、恒常的に使用できることを保証するものではありません。
        /// Twitterの仕様変更によって、今後使用できなくなる可能性があります。
        /// </summary>
        /// <returns>TwitterContext。失敗した場合はNull</returns>
        public async Task<TwitterContext> GetAccessTokenFromScreenNameAndPassword(string ScreenName, string Password)
        {
            if (this.OAuthToken == null)
                throw new ApplicationException("リクエスト トークンが設定されていません。");

            try
            {
                using (var tws = new System.Net.WebClient())
                {
                    string sorce = tws.DownloadString(Twitch.Twitter.TwitterBase.URL);

                    Regex reg = new Regex("<input type=\"hidden\" name=\"authenticity_token\" value=\"(?<token>.*?)\">",
                                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    Match m = reg.Match(sorce);
                    Debug.WriteLine("authenticity_token: " + m.Groups["token"].Value);

                    var ps = new System.Collections.Specialized.NameValueCollection();
                    ps.Add("authenticity_token", m.Groups["token"].Value);
                    ps.Add("oauth_token", OAuthToken);
                    ps.Add("session[username_or_email]", ScreenName);
                    ps.Add("session[password]", Password);

                    using (var wc = new System.Net.WebClient())
                    {
                        byte[] resData = wc.UploadValues(Twitch.Twitter.API.Urls.Oauth_Authorize, ps);

                        string resText = System.Text.Encoding.UTF8.GetString(resData);

                        reg = new Regex("<code>(?<pin>.*?)</code>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline);

                        m = reg.Match(resText);
                        System.Diagnostics.Debug.WriteLine("pin: " + m.Groups["pin"].Value);

                        return await GetAccessTokenFromPinCode(m.Groups["pin"].Value);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// PINコードからAccessToken,AccessTokenSecretを取得します。
        /// </summary>
        /// <param name="PIN">PINコード</param>
        /// <returns>TwitterContext。失敗した場合はNull</returns>
        public async Task<TwitterContext> GetAccessTokenFromPinCode(string PIN)
        {
            var tw = new Twitch.TwitterContext(this.ConsumerKey, this.ConsumerSecret, this.OAuthToken, this.OAuthTokenSecret);
            string res = await Twitch.Twitter.APIs.REST.Oauth.AccessToken(tw, PIN);

            if (!string.IsNullOrEmpty(res))
            {
                string access_token = Utility.AnalyzeUrlQuery.Analyze(res, "oauth_token");
                string access_token_secret = Utility.AnalyzeUrlQuery.Analyze(res, "oauth_token_secret");

                return new Twitch.TwitterContext(this.ConsumerKey, this.ConsumerSecret, access_token, access_token_secret);
            }
            else
                return null;
        }
        
        /// <summary>
        /// xAuthによってAccessToken,AccessTokenSecretを取得します。これはxAuthが許可されたトークンでのみ使用する事が出来ます。
        /// </summary>
        /// <returns>TwitterContext。失敗した場合はNull</returns>
        public async Task<TwitterContext> GetAccessTokenFromXAuth(string ScreenName, string Password)
        {
            var tw = new Twitch.TwitterContext(this.ConsumerKey, this.ConsumerSecret);
            string res = await Twitch.Twitter.APIs.REST.Oauth.AccessToken(tw, ScreenName, Password);

            if (!string.IsNullOrEmpty(res))
            {
                string access_token = Utility.AnalyzeUrlQuery.Analyze(res, "oauth_token");
                string access_token_secret = Utility.AnalyzeUrlQuery.Analyze(res, "oauth_token_secret");

                return new Twitch.TwitterContext(this.ConsumerKey, this.ConsumerSecret, access_token, access_token_secret);
            }
            else
                return null;
        }
    }
}
