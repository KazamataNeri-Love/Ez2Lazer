// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.EzOsuGame.Identity
{
    /// <summary>
    /// 「本地账户名接管成绩」：决定当前生效的身份是谁。
    /// </summary>
    /// <remarks>
    /// 开启接管后，<b>一切「我是谁」都以本地账户名为准</b> —— 成绩署名、HUD 与结算显示名、本地档案分区；
    /// 在线连接只保留下载等只读用途，且不上传成绩、不做在线拉取。
    /// 关闭时完全按登录的在线账号走，与上游行为一致。
    /// <para>
    /// 刻意<b>不</b>影响的地方：API 鉴权与请求身份、在线排行榜、聊天与好友、在线状态上报、联机、收藏。
    /// 那些属于「网络会话身份」，不是「我这个人」；接管它们只会把下载等在线功能弄坏。
    /// </para>
    /// </remarks>
    public static class EzLocalIdentity
    {
        /// <summary>
        /// 本地用户使用的 <c>OnlineID</c>。
        /// 取 1 而不是 0：0 是 <see cref="APIUser.SYSTEM_USER_ID"/>（Guest / system 专用），
        /// 而代码里所有「本地用户」判定都写作 <c>OnlineID &lt;= 1</c>
        /// （见 <c>LocalUserStatisticsProvider</c> 与 <c>ScoreInfoExtensions</c> 的过滤条件）。
        /// </summary>
        public const int LOCAL_USER_ONLINE_ID = 1;

        /// <summary>
        /// 接管是否生效：开关开启，<b>且</b>本地账户名非空（空名回退到在线账号，避免把人署成空白）。
        /// </summary>
        public static bool TakesOver(Ez2ConfigManager config)
            => config.Get<bool>(Ez2Setting.LocalAccountNameTakesOverScore) && getLocalName(config) != null;

        /// <summary>
        /// 当前生效的身份名。接管时为本地账户名，否则为在线账号名。
        /// </summary>
        public static string EffectiveUsername(Ez2ConfigManager config, IAPIProvider api)
            => getLocalName(config) is string localName && TakesOver(config) ? localName : api.LocalUser.Value.Username;

        /// <summary>
        /// 构造当前生效身份的用户：用于成绩署名，也用于头像等界面显示。
        /// </summary>
        /// <remarks>
        /// 接管时返回一个没有在线档案的本地用户（<c>OnlineID = 1</c>）：
        /// <list type="bullet">
        ///   <item><see cref="Scoring.ScoreInfo.User"/> 的 setter 会把它落成 <c>RealmUser</c>，本地档案据此分区；</item>
        ///   <item>
        ///     <c>DrawableAvatar</c> 会先按 <c>Username</c> 去 <c>EzResources/Modify/avatars/{Username}/</c> 找本地头像，
        ///     命中就不再拉在线头像；而 <c>OnlineID = 1</c> 也正好不满足它「<c>OnlineID &gt; 1</c> 才拉在线」的条件。
        ///   </item>
        /// </list>
        /// </remarks>
        public static APIUser CreateEffectiveUser(Ez2ConfigManager config, IAPIProvider api)
        {
            if (getLocalName(config) is not string localName || !TakesOver(config))
                return api.LocalUser.Value;

            return new APIUser
            {
                Id = LOCAL_USER_ONLINE_ID,
                Username = localName,
            };
        }

        private static string? getLocalName(Ez2ConfigManager config)
        {
            string name = config.Get<string>(Ez2Setting.LocalAccountName).Trim();

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        /// <summary>
        /// 接管时返回本地账户名，否则返回 null。供只拿得到配置、拿不到 <see cref="IAPIProvider"/> 的地方使用。
        /// </summary>
        public static string? LocalNameIfTakesOver(Ez2ConfigManager config) => TakesOver(config) ? getLocalName(config) : null;

        /// <summary>
        /// 构造跟随「当前生效身份名」变化的绑定，供 UI 订阅。
        /// 返回值同时持有上游绑定副本，避开框架弱引用导致的订阅静默失效；调用方需持有返回值。
        /// </summary>
        public static Bindable<string> CreateUsernameBindable(Ez2ConfigManager config, IAPIProvider api) => new EffectiveUsernameBindable(config, api);

        private sealed class EffectiveUsernameBindable : Bindable<string>
        {
            /// <summary>
            /// 只为保活上游绑定副本。用 <see cref="object"/> 是因为框架里
            /// <c>IBindable&lt;T&gt;</c> 与 <c>IBindable</c> 是并列接口、互不继承。
            /// </summary>
            private readonly List<object> sources = new List<object>();

            public EffectiveUsernameBindable(Ez2ConfigManager config, IAPIProvider api)
                : base(EffectiveUsername(config, api))
            {
                var localName = config.GetBindable<string>(Ez2Setting.LocalAccountName);
                var takesOver = config.GetBindable<bool>(Ez2Setting.LocalAccountNameTakesOverScore);
                var localUser = api.LocalUser.GetBoundCopy();

                localName.BindValueChanged(_ => refresh());
                takesOver.BindValueChanged(_ => refresh());
                localUser.BindValueChanged(_ => refresh());

                sources.Add(localName);
                sources.Add(takesOver);
                sources.Add(localUser);

                void refresh() => Value = EffectiveUsername(config, api);
            }
        }

        /// <summary>
        /// 构造跟随「当前生效身份」变化的用户绑定，供头像等界面使用。调用方需持有返回值。
        /// </summary>
        public static Bindable<APIUser> CreateUserBindable(Ez2ConfigManager config, IAPIProvider api) => new EffectiveUserBindable(config, api);

        private sealed class EffectiveUserBindable : Bindable<APIUser>
        {
            private readonly List<object> sources = new List<object>();

            public EffectiveUserBindable(Ez2ConfigManager config, IAPIProvider api)
                : base(CreateEffectiveUser(config, api))
            {
                // 用户名绑定已经聚合了「本地账户名 / 接管开关 / 在线账号」三个变化来源，这里只借它当信号。
                var username = CreateUsernameBindable(config, api);
                username.BindValueChanged(_ => Value = CreateEffectiveUser(config, api), true);

                sources.Add(username);
            }
        }
    }
}
