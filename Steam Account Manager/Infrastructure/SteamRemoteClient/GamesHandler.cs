﻿using Steam_Account_Manager.Infrastructure.Models;
using SteamKit2;
using SteamKit2.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Steam_Account_Manager.Infrastructure.SteamRemoteClient
{
    internal sealed class GamesHandler : ClientMsgHandler
    {
        internal const byte MaxGamesPlayed = 32;

        public override void HandleMsg(IPacketMsg PacketMsg)
        {
            if (Client == null)
                throw new InvalidOperationException(nameof(Client));

            switch (PacketMsg.MsgType)
            {
                case EMsg.ClientGetUserStatsResponse:
                    var getAchievementsResponse = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(PacketMsg);
                    Client.PostCallback(new GetAchievementsCallback(PacketMsg.TargetJobID, getAchievementsResponse.Body));
                    break;
                case EMsg.ClientStoreUserStatsResponse:
                    var setAchievementsResponse = new ClientMsgProtobuf<CMsgClientStoreUserStatsResponse>(PacketMsg);
                    Client.PostCallback(new SetAchievementsCallback(PacketMsg.TargetJobID, setAchievementsResponse.Body));
                    break;
            }

        }

        
        internal async Task PlayGames(IReadOnlyCollection<int> GameIDs, string gameName = null)
        {
            if (Client == null)
                throw new InvalidOperationException(nameof(Client));

            if (!Client.IsConnected)
                return;

            var request = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayedWithDataBlob)
            {
                Body =
                {
                    client_os_type = unchecked((uint)SteamRemoteClient.OSType)
                }
            };

            if (!String.IsNullOrEmpty(gameName))
            {
                Client.Send(request);
                await Task.Delay(SteamRemoteClient.CallbackSleep).ConfigureAwait(false);

                request.Body.games_played.Add(
                    new CMsgClientGamesPlayed.GamePlayed
                    {
                        game_extra_info = gameName,
                        game_id = new GameID
                        {
                            AppType = GameID.GameType.Shortcut,
                            ModID = uint.MaxValue
                        }
                    });
            }

            if(GameIDs != null && GameIDs.Count > 0)
            {
                IEnumerable<int> uniqueValidGameIDs = (GameIDs as ISet<int> ?? GameIDs.Distinct()).Where(gameID => gameID > 0);

                foreach (uint gameID in uniqueValidGameIDs)
                {
                    if(request.Body.games_played.Count >= MaxGamesPlayed)
                    {
                        if (String.IsNullOrEmpty(gameName))
                            throw new ArgumentOutOfRangeException(nameof(GameIDs));

                        gameName = null;

                        request.Body.games_played.RemoveAt(0);
                    }

                    request.Body.games_played.Add(
                        new CMsgClientGamesPlayed.GamePlayed
                        {
                            game_id = new GameID(gameID)
                        });
                }
            }
            
            Client.Send(request);
        }

        internal abstract class AchievementsCallback<T> : CallbackMsg
        {
            internal readonly T Response;
            internal readonly bool Success;

            internal AchievementsCallback(JobID jobID, T msg, Func<T, EResult> eresultGetter, string error)
            {
                if (jobID == null)
                {
                    throw new ArgumentNullException(nameof(jobID));
                }

                if (msg == null)
                {
                    throw new ArgumentNullException(nameof(msg));
                }

                JobID = jobID;
                Success = eresultGetter(msg) == EResult.OK;
                Response = msg;
            }

        }
        internal sealed class GetAchievementsCallback : AchievementsCallback<CMsgClientGetUserStatsResponse>
        {
            internal GetAchievementsCallback(JobID jobID, CMsgClientGetUserStatsResponse msg)
                : base(jobID, msg, message => (EResult)msg.eresult, "GetAchievements") { }
        }
        internal sealed class SetAchievementsCallback : AchievementsCallback<CMsgClientStoreUserStatsResponse>
        {
            internal SetAchievementsCallback(JobID jobID, CMsgClientStoreUserStatsResponse msg)
                : base(jobID, msg, message => (EResult)msg.eresult, "SetAchievements") { }
        }

        internal List<StatData> ParseResponse(CMsgClientGetUserStatsResponse Response,ulong gameID)
        {
            List<StatData> result = new List<StatData>();
            KeyValue KeyValues = new KeyValue();

            string iconUrlBase = $"https://cdn.akamai.steamstatic.com/steamcommunity/public/images/apps/{gameID}/";
            if (Response.schema != null)
            {
                using (MemoryStream ms = new MemoryStream(Response.schema))
                {
                    if (!KeyValues.TryReadAsBinary(ms))
                    {
                        return null;
                    };
                }

                foreach (KeyValue stat in KeyValues.Children.Find(Child => Child.Name == "stats")?.Children ?? new List<KeyValue>())
                {
                    if (stat.Children.Find(Child => Child.Name == "type")?.Value == "4")
                    {
                        foreach (KeyValue Achievement in stat.Children.Find(Child => Child.Name == "bits")?.Children ?? new List<KeyValue>())
                        {
                            if (int.TryParse(Achievement.Name, out int bitNum))
                            {
                                if (uint.TryParse(stat.Name, out uint statNum))
                                {
                                    uint? stat_value = Response?.stats?.Find(statElement => statElement.stat_id == statNum)?.stat_value;
                                    bool isSet = stat_value != null && (stat_value & ((uint)1 << bitNum)) != 0;

                                    bool restricted = Achievement.Children.Find(Child => Child.Name == "permission") != null;

                                    string dependencyName = (Achievement.Children.Find(Child => Child.Name == "progress") == null) ? "" : Achievement.Children.Find(Child => Child.Name == "progress")?.Children?.Find(Child => Child.Name == "value")?.Children?.Find(Child => Child.Name == "operand1")?.Value;

                                    uint.TryParse((Achievement.Children.Find(Child => Child.Name == "progress") == null) ? "0" : Achievement.Children.Find(Child => Child.Name == "progress").Children.Find(Child => Child.Name == "max_val")?.Value, out uint dependancyValue);
                                    string lang = CultureInfo.CurrentUICulture.EnglishName.ToLower();
                                    if (lang.IndexOf('(') > 0)
                                    {
                                        lang = lang.Substring(0, lang.IndexOf('(') - 1);
                                    }
                                    if (Achievement.Children.Find(Child => Child.Name == "display")?.Children?.Find(Child => Child.Name == "name")?.Children?.Find(Child => Child.Name == lang) == null)
                                    {
                                        lang = "english";
                                    }

                                    string name = Achievement.Children.Find(
                                        Child => Child.Name == "display")?.Children?.Find(
                                            Child => Child.Name == "name")?.Children?.Find(
                                                Child => Child.Name == lang)?.Value;
                                    
                                    string iconHash = iconUrlBase + Achievement.Children.Find(
                                        Child => Child.Name == "display")?.Children?.Find(
                                            Child => Child.Name == "icon")?.Value;
                                    
                                    string iconHashGray = iconUrlBase + Achievement.Children.Find(
                                        Child => Child.Name == "display")?.Children?.Find(
                                            Child => Child.Name == "icon_gray")?.Value;

                                    string desc = Achievement.Children.Find(
                                        Child => Child.Name == "display")?.Children?.Find(
                                            Child => Child.Name == "desc")?.Children?.Find(
                                                Child => Child.Name == lang)?.Value;

                                    result.Add(new StatData()
                                    {
                                        StatNum = statNum,
                                        BitNum = bitNum,
                                        IsSet = isSet,
                                        Restricted = restricted,
                                        DependencyValue = dependancyValue,
                                        DependencyName = dependencyName,
                                        Dependency = 0,
                                        Name = name,
                                        StatValue = stat_value ?? 0,
                                        IconHash = iconHash,
                                        IconHashGray = iconHashGray,
                                        CurrentIconView = isSet ? iconHash : iconHashGray,
                                        Description = desc
                                    });

                                }
                            }
                        }
                    }
                }
                foreach (KeyValue stat in KeyValues.Children.Find(Child => Child.Name == "stats")?.Children ?? new List<KeyValue>())
                {
                    if (stat.Children.Find(Child => Child.Name == "type")?.Value == "1")
                    {
                        if (uint.TryParse(stat.Name, out uint statNum))
                        {
                            bool restricted = stat.Children.Find(Child => Child.Name == "permission") != null;
                            string name = stat.Children.Find(Child => Child.Name == "name")?.Value;
                            if (name != null)
                            {
                                StatData ParentStat = result.Find(item => item.DependencyName == name);
                                if (ParentStat != null)
                                {
                                    ParentStat.Dependency = statNum;
                                    if (restricted && !ParentStat.Restricted)
                                    {
                                        ParentStat.Restricted = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        internal async Task<List<StatData>> GetAchievements(ulong steamId, ulong gameID)
        {

            GetAchievementsCallback response = await GetAchievementsResponse(76561199051937995, gameID);
            if (!response.Success)
                return null;

            return ParseResponse(response.Response,gameID);

        }

        private async Task<GetAchievementsCallback> GetAchievementsResponse(ulong steamId, ulong gameID)
        {
            if (!Client.IsConnected)
            {
                return null;
            }

            ClientMsgProtobuf<CMsgClientGetUserStats> request = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats)
            {
                SourceJobID = Client.GetNextJobID(),
                Body = {
                    game_id =  gameID,
                    steam_id_for_user = steamId,
                }
            };

            Client.Send(request);

            return await new AsyncJob<GetAchievementsCallback>(Client, request.SourceJobID);
        }

        internal async Task<string> SetAchievements(ulong steamId, ulong appId, HashSet<uint> achievements, bool set = true)
        {
            if (!Client.IsConnected)
            {
                return null;
            }

            List<string> responses = new List<string>();

            GetAchievementsCallback response = await GetAchievementsResponse(steamId, appId);
            if (response == null || !response.Success)
            {
                return "Can't retrieve achievements for " + appId.ToString(); ;
            }

            List<StatData> Stats = ParseResponse(response.Response);
            if (Stats == null)
            {
                responses.Add(Strings.WarningFailed);
                return "\u200B\n" + string.Join(Environment.NewLine, responses);
            }

            List<CMsgClientStoreUserStats2.Stats> statsToSet = new List<CMsgClientStoreUserStats2.Stats>();

            if (achievements.Count == 0)
            {
                foreach (StatData stat in Stats.Where(s => !s.Restricted))
                {
                    statsToSet.AddRange(GetStatsToSet(statsToSet, stat, set));
                }
            }
            else
            {
                foreach (uint achievement in achievements)
                {
                    if (Stats.Count < achievement)
                    {
                        responses.Add("Achievement #" + achievement.ToString() + " is out of range");
                        continue;
                    }

                    if (Stats[(int)achievement - 1].IsSet == set)
                    {
                        responses.Add("Achievement #" + achievement.ToString() + " is already " + (set ? "unlocked" : "locked"));
                        continue;
                    }
                    if (Stats[(int)achievement - 1].Restricted)
                    {
                        responses.Add("Achievement #" + achievement.ToString() + " is protected and can't be switched");
                        continue;
                    }

                    statsToSet.AddRange(GetStatsToSet(statsToSet, Stats[(int)achievement - 1], set));
                }
            }
            if (statsToSet.Count == 0)
            {
                responses.Add(Strings.WarningFailed);
                return "\u200B\n" + string.Join(Environment.NewLine, responses);
            };
            if (responses.Count > 0)
            {
                responses.Add("Trying to switch remaining achievements..."); //if some errors occured
            }
            ClientMsgProtobuf<CMsgClientStoreUserStats2> request = new ClientMsgProtobuf<CMsgClientStoreUserStats2>(EMsg.ClientStoreUserStats2)
            {
                SourceJobID = Client.GetNextJobID(),
                Body = {
                    game_id = (uint) appId,
                    settor_steam_id = (ulong)bot.SteamID,
                    settee_steam_id = (ulong)bot.SteamID,
                    explicit_reset = false,
                    crc_stats = response.Response.crc_stats
                }
            };
            request.Body.stats.AddRange(statsToSet);
            Client.Send(request);

            SetAchievementsCallback setResponse = await new AsyncJob<SetAchievementsCallback>(Client, request.SourceJobID).ConfigureAwait(false);

            return "\u200B\n" + string.Join(Environment.NewLine, responses);
        }
    }

}
