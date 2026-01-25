using LabApi.Events;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassScanner
{
    public class ClassScanner : Plugin
    {
        private Config _config;
        private CancellationTokenSource _scanCancellationToken;
        public static bool ScanInProgress;
        public override string Name => "Scanner";
        public override string Description => "Announces/Scans via CASSIE how many classes are left and how many players are in each class";
        public override string Author => "adasjusk";
        public override Version Version => new Version(2, 0, 0);
        public override Version RequiredApiVersion => new Version(LabApiProperties.CompiledVersion);
        public static ClassScanner Instance { get; private set; }

        public override void Enable()
        {
            Instance = this;
            _config = new Config();
            ConfigurationLoader.LoadConfig<Config>(this, "config", false);
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundEnded += OnRoundEnded;
            WarheadEvents.Detonated += OnWarheadDetonated;
            Logger.Info($"{Name} v{Version} has been enabled!");
        }

        public override void Disable()
        {
            ServerEvents.RoundStarted -= OnRoundStarted;
            ServerEvents.RoundEnded -= OnRoundEnded;
            WarheadEvents.Detonated -= OnWarheadDetonated;
            _scanCancellationToken?.Cancel();
            Instance = null;
            Logger.Info(Name + " has been disabled!");
        }

        private void OnRoundStarted()
        {
            ScanInProgress = false;
            _scanCancellationToken?.Cancel();
            _scanCancellationToken = new CancellationTokenSource();
            if (_config.RegularScanning)
            {
                Task.Run(() => ScanLoop(_scanCancellationToken.Token));
            }
        }

        private void OnRoundEnded(RoundEndedEventArgs _)
        {
            _scanCancellationToken?.Cancel();
            ScanInProgress = false;
        }

        private void OnWarheadDetonated(WarheadDetonatedEventArgs _)
        {
            if (!_config.ScanAfterNuke)
            {
                _scanCancellationToken?.Cancel();
            }
        }

        private async Task ScanLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    while (Player.List.Count((Player p) => p.IsAlive) < 1 && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    if (ScanInProgress)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    ScanInProgress = true;
                    string text = _config.ScanStartMessageCassie.Replace("{LENGTH}", _config.ScanLength.ToString());
                    string text2 = _config.ScanStartMessageCaption.Replace("{LENGTH}", _config.ScanLength.ToString());
                    Announcer.Message(text, text2, true, 0f, 1f);
                    await Task.Delay(TimeSpan.FromSeconds(_config.ScanLength), cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        PerformScan();
                    }
                    await Task.Delay(TimeSpan.FromMinutes(_config.DelayAfterScanMinutes), cancellationToken);
                    ScanInProgress = false;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception arg)
            {
                Logger.Error($"Error in scan loop: {arg}");
                ScanInProgress = false;
            }
        }

        public void PerformScan()
        {
            try
            {
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                int num4 = 0;
                int num5 = 0;
                int num6 = 0;
                foreach (Player item in Player.List)
                {
                    if (!item.IsAlive)
                    {
                        continue;
                    }
                    
                    switch (item.Team)
                    {
                        case Team.SCPs:
                            num++;
                            break;
                        case Team.ClassD:
                            num2++;
                            break;
                        case Team.Scientists:
                            num3++;
                            break;
                        case Team.FoundationForces:
                            if (item.Role == RoleTypeId.FacilityGuard)
                            {
                                num4++;
                            }
                            else
                            {
                                num5++;
                            }
                            break;
                        case Team.ChaosInsurgency:
                            num6++;
                            break;
                    }
                }
                if (num + num2 + num3 + num4 + num5 + num6 == 0)
                {
                    Announcer.Message(_config.ScanNobodyMessageCassie, _config.ScanNobodyMessageCaption, true, 0f, 1f);
                    ScanInProgress = false;
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder("SCANNED FACILITY . FOUND ");
                StringBuilder stringBuilder2 = new StringBuilder("Scanned Facility. Found ");
                List<string> list = new List<string>();
                List<string> list2 = new List<string>();
                if (num > 0)
                {
                    list.Add(string.Format("{0} SCP {1}", num, (num == 1) ? "SUBJECT" : "SUBJECTS"));
                    list2.Add(string.Format("{0} SCP{1}", num, (num == 1) ? "" : "s"));
                }
                if (num2 > 0)
                {
                    list.Add(string.Format("{0} CLASS D {1}", num2, (num2 == 1) ? "PERSONNEL" : "PERSONNEL"));
                    list2.Add($"{num2} Class-D");
                }
                if (num4 > 0)
                {
                    list.Add(string.Format("{0} FACILITY {1}", num4, (num4 == 1) ? "GUARD" : "GUARDS"));
                    list2.Add(string.Format("{0} Facility Guard{1}", num4, (num4 == 1) ? "" : "s"));
                }
                if (num6 > 0)
                {
                    list.Add(string.Format("{0} CHAOS {1}", num6, (num6 == 1) ? "INSURGENT" : "INSURGENTS"));
                    list2.Add(string.Format("{0} Chaos Insurgent{1}", num6, (num6 == 1) ? "" : "s"));
                }
                if (num5 > 0)
                {
                    list.Add(string.Format("{0} MTF {1}", num5, (num5 == 1) ? "UNIT" : "UNITS"));
                    list2.Add(string.Format("{0} NTF{1}", num5, (num5 == 1) ? "" : "s"));
                }
                if (num3 > 0)
                {
                    list.Add(string.Format("{0} {1}", num3, (num3 == 1) ? "SCIENTIST" : "SCIENTISTS"));
                    list2.Add(string.Format("{0} Scientist{1}", num3, (num3 == 1) ? "" : "s"));
                }
                stringBuilder.Append(string.Join(" . ", list));
                stringBuilder2.Append(string.Join(", ", list2));
                Announcer.Message(stringBuilder.ToString(), stringBuilder2.ToString(), true, 0f, 1f);
                ScanInProgress = false;
            }
            catch (Exception arg)
            {
                Logger.Error($"Error during scan: {arg}");
                ScanInProgress = false;
            }
        }

        public void ForceScan()
        {
            if (ScanInProgress)
            {
                Logger.Warn("A scan is already in progress!");
                return;
            }
            Logger.Info("Force scan initiated by external call");
            ScanInProgress = true;
            PerformScan();
        }
    }
}
