using ClassScanner.Langs;
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassScanner
{
    public class ClassScanner : Plugin<Config>
    {
        private CancellationTokenSource _scanCancellationToken;
        public static bool ScanInProgress;
        public override string Name => "Scanner";
        public override string Description => "Announces/Scans via CASSIE how many classes are left and how many players are in each class";
        public override string Author => "adasjusk";
        public override Version Version => new Version(2, 0, 0);
        public override Version RequiredApiVersion => new Version(LabApiProperties.CompiledVersion);
        public static ClassScanner Instance { get; private set; }

        private EN _langEN;
        private DE _langDE;
        private PL _langPL;
        private LT _langLT;
        private FR _langFR;

        private string _scanStartCassie;
        private string _scanStartCaption;
        private string _scanNobodyCassie;
        private string _scanNobodyCaption;
        private string _scanCompleteCassie;
        private string _scanCompleteCaption;
        private string _scpSingularCassie;
        private string _scpPluralCassie;
        private string _scpSingularCaption;
        private string _scpPluralCaption;
        private string _classDSingularCassie;
        private string _classDPluralCassie;
        private string _classDSingularCaption;
        private string _classDPluralCaption;
        private string _guardSingularCassie;
        private string _guardPluralCassie;
        private string _guardSingularCaption;
        private string _guardPluralCaption;
        private string _chaosSingularCassie;
        private string _chaosPluralCassie;
        private string _chaosSingularCaption;
        private string _chaosPluralCaption;
        private string _mtfSingularCassie;
        private string _mtfPluralCassie;
        private string _mtfSingularCaption;
        private string _mtfPluralCaption;
        private string _scientistSingularCassie;
        private string _scientistPluralCassie;
        private string _scientistSingularCaption;
        private string _scientistPluralCaption;

        public override void Enable()
        {
            Instance = this;
            
            LoadLanguageConfigs();
            
            if (Config.AutoDetectLanguage)
            {
                string detectedLang = DetectLanguageFromGame();
                if (detectedLang != null)
                {
                    Config.Language = detectedLang;
                    Logger.Info($"Auto-detected language: {detectedLang}");
                }
                else
                {
                    Config.AutoDetectLanguage = false;
                    Config.Language = "EN";
                    Logger.Warn("Failed to auto-detect language, defaulting to English");
                }
            }
            
            ApplyLanguage(Config.Language);
            
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundEnded += OnRoundEnded;
            WarheadEvents.Detonated += OnWarheadDetonated;
            Logger.Info($"{Name} v{Version} has been enabled!");
        }

        private void LoadLanguageConfigs()
        {
            _langEN = new EN();
            _langDE = new DE();
            _langPL = new PL();
            _langLT = new LT();
            _langFR = new FR();

            try
            {
                var loadedEN = ConfigurationLoader.LoadConfig<EN>(this, "Langs_EN", false);
                if (loadedEN != null)
                    _langEN = loadedEN;
                else
                    ConfigurationLoader.SaveConfig(this, _langEN, "Langs_EN", false);
            }
            catch
            {
                ConfigurationLoader.SaveConfig(this, _langEN, "Langs_EN", false);
            }

            try
            {
                var loadedDE = ConfigurationLoader.LoadConfig<DE>(this, "Langs_DE", false);
                if (loadedDE != null)
                    _langDE = loadedDE;
                else
                    ConfigurationLoader.SaveConfig(this, _langDE, "Langs_DE", false);
            }
            catch
            {
                ConfigurationLoader.SaveConfig(this, _langDE, "Langs_DE", false);
            }

            try
            {
                var loadedPL = ConfigurationLoader.LoadConfig<PL>(this, "Langs_PL", false);
                if (loadedPL != null)
                    _langPL = loadedPL;
                else
                    ConfigurationLoader.SaveConfig(this, _langPL, "Langs_PL", false);
            }
            catch
            {
                ConfigurationLoader.SaveConfig(this, _langPL, "Langs_PL", false);
            }

            try
            {
                var loadedLT = ConfigurationLoader.LoadConfig<LT>(this, "Langs_LT", false);
                if (loadedLT != null)
                    _langLT = loadedLT;
                else
                    ConfigurationLoader.SaveConfig(this, _langLT, "Langs_LT", false);
            }
            catch
            {
                ConfigurationLoader.SaveConfig(this, _langLT, "Langs_LT", false);
            }

            try
            {
                var loadedFR = ConfigurationLoader.LoadConfig<FR>(this, "Langs_FR", false);
                if (loadedFR != null)
                    _langFR = loadedFR;
                else
                    ConfigurationLoader.SaveConfig(this, _langFR, "Langs_FR", false);
            }
            catch
            {
                ConfigurationLoader.SaveConfig(this, _langFR, "Langs_FR", false);
            }
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

        private string DetectLanguageFromGame()
        {
            try
            {
                string[] possiblePaths = new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "Translations"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SCP Secret Laboratory", "Translations"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations")
                };

                foreach (string basePath in possiblePaths)
                {
                    if (!Directory.Exists(basePath))
                        continue;

                    string[] langFiles = Directory.GetFiles(basePath, "*.txt");
                    foreach (string file in langFiles)
                    {
                        string content = File.ReadAllText(file);
                        if (content.Contains("menus.general.") || content.Contains("gameplay.") || content.Contains("interface."))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file).ToUpperInvariant();
                            
                            if (fileName == "ENGLISH" || fileName == "EN") return "EN";
                            if (fileName == "GERMAN" || fileName == "DE" || fileName == "DEUTSCH") return "DE";
                            if (fileName == "POLISH" || fileName == "PL" || fileName == "POLSKI") return "PL";
                            if (fileName == "LITHUANIAN" || fileName == "LT" || fileName == "LIETUVIU") return "LT";
                            if (fileName == "FRENCH" || fileName == "FR" || fileName == "FRANCAIS") return "FR";
                            
                            return fileName.Length == 2 ? fileName : "EN";
                        }
                    }
                }
                
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "config_gameplay.txt");
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("language=") || line.StartsWith("Language="))
                        {
                            string lang = line.Split('=')[1].Trim().ToUpperInvariant();
                            if (lang.Length >= 2)
                                return lang.Substring(0, 2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Language detection error: {ex.Message}");
            }
            
            return null;
        }

        private void ApplyLanguage(string lang)
        {
            switch (lang.ToUpperInvariant())
            {
                case "DE":
                    _scanStartCassie = _langDE.ScanStartMessageCassie;
                    _scanStartCaption = _langDE.ScanStartMessageCaption;
                    _scanNobodyCassie = _langDE.ScanNobodyMessageCassie;
                    _scanNobodyCaption = _langDE.ScanNobodyMessageCaption;
                    _scanCompleteCassie = _langDE.ScanCompleteCassie;
                    _scanCompleteCaption = _langDE.ScanCompleteCaption;
                    _scpSingularCassie = _langDE.ScpSingularCassie;
                    _scpPluralCassie = _langDE.ScpPluralCassie;
                    _scpSingularCaption = _langDE.ScpSingularCaption;
                    _scpPluralCaption = _langDE.ScpPluralCaption;
                    _classDSingularCassie = _langDE.ClassDSingularCassie;
                    _classDPluralCassie = _langDE.ClassDPluralCassie;
                    _classDSingularCaption = _langDE.ClassDSingularCaption;
                    _classDPluralCaption = _langDE.ClassDPluralCaption;
                    _guardSingularCassie = _langDE.GuardSingularCassie;
                    _guardPluralCassie = _langDE.GuardPluralCassie;
                    _guardSingularCaption = _langDE.GuardSingularCaption;
                    _guardPluralCaption = _langDE.GuardPluralCaption;
                    _chaosSingularCassie = _langDE.ChaosSingularCassie;
                    _chaosPluralCassie = _langDE.ChaosPluralCassie;
                    _chaosSingularCaption = _langDE.ChaosSingularCaption;
                    _chaosPluralCaption = _langDE.ChaosPluralCaption;
                    _mtfSingularCassie = _langDE.MtfSingularCassie;
                    _mtfPluralCassie = _langDE.MtfPluralCassie;
                    _mtfSingularCaption = _langDE.MtfSingularCaption;
                    _mtfPluralCaption = _langDE.MtfPluralCaption;
                    _scientistSingularCassie = _langDE.ScientistSingularCassie;
                    _scientistPluralCassie = _langDE.ScientistPluralCassie;
                    _scientistSingularCaption = _langDE.ScientistSingularCaption;
                    _scientistPluralCaption = _langDE.ScientistPluralCaption;
                    break;
                case "PL":
                    _scanStartCassie = _langPL.ScanStartMessageCassie;
                    _scanStartCaption = _langPL.ScanStartMessageCaption;
                    _scanNobodyCassie = _langPL.ScanNobodyMessageCassie;
                    _scanNobodyCaption = _langPL.ScanNobodyMessageCaption;
                    _scanCompleteCassie = _langPL.ScanCompleteCassie;
                    _scanCompleteCaption = _langPL.ScanCompleteCaption;
                    _scpSingularCassie = _langPL.ScpSingularCassie;
                    _scpPluralCassie = _langPL.ScpPluralCassie;
                    _scpSingularCaption = _langPL.ScpSingularCaption;
                    _scpPluralCaption = _langPL.ScpPluralCaption;
                    _classDSingularCassie = _langPL.ClassDSingularCassie;
                    _classDPluralCassie = _langPL.ClassDPluralCassie;
                    _classDSingularCaption = _langPL.ClassDSingularCaption;
                    _classDPluralCaption = _langPL.ClassDPluralCaption;
                    _guardSingularCassie = _langPL.GuardSingularCassie;
                    _guardPluralCassie = _langPL.GuardPluralCassie;
                    _guardSingularCaption = _langPL.GuardSingularCaption;
                    _guardPluralCaption = _langPL.GuardPluralCaption;
                    _chaosSingularCassie = _langPL.ChaosSingularCassie;
                    _chaosPluralCassie = _langPL.ChaosPluralCassie;
                    _chaosSingularCaption = _langPL.ChaosSingularCaption;
                    _chaosPluralCaption = _langPL.ChaosPluralCaption;
                    _mtfSingularCassie = _langPL.MtfSingularCassie;
                    _mtfPluralCassie = _langPL.MtfPluralCassie;
                    _mtfSingularCaption = _langPL.MtfSingularCaption;
                    _mtfPluralCaption = _langPL.MtfPluralCaption;
                    _scientistSingularCassie = _langPL.ScientistSingularCassie;
                    _scientistPluralCassie = _langPL.ScientistPluralCassie;
                    _scientistSingularCaption = _langPL.ScientistSingularCaption;
                    _scientistPluralCaption = _langPL.ScientistPluralCaption;
                    break;
                case "LT":
                    _scanStartCassie = _langLT.ScanStartMessageCassie;
                    _scanStartCaption = _langLT.ScanStartMessageCaption;
                    _scanNobodyCassie = _langLT.ScanNobodyMessageCassie;
                    _scanNobodyCaption = _langLT.ScanNobodyMessageCaption;
                    _scanCompleteCassie = _langLT.ScanCompleteCassie;
                    _scanCompleteCaption = _langLT.ScanCompleteCaption;
                    _scpSingularCassie = _langLT.ScpSingularCassie;
                    _scpPluralCassie = _langLT.ScpPluralCassie;
                    _scpSingularCaption = _langLT.ScpSingularCaption;
                    _scpPluralCaption = _langLT.ScpPluralCaption;
                    _classDSingularCassie = _langLT.ClassDSingularCassie;
                    _classDPluralCassie = _langLT.ClassDPluralCassie;
                    _classDSingularCaption = _langLT.ClassDSingularCaption;
                    _classDPluralCaption = _langLT.ClassDPluralCaption;
                    _guardSingularCassie = _langLT.GuardSingularCassie;
                    _guardPluralCassie = _langLT.GuardPluralCassie;
                    _guardSingularCaption = _langLT.GuardSingularCaption;
                    _guardPluralCaption = _langLT.GuardPluralCaption;
                    _chaosSingularCassie = _langLT.ChaosSingularCassie;
                    _chaosPluralCassie = _langLT.ChaosPluralCassie;
                    _chaosSingularCaption = _langLT.ChaosSingularCaption;
                    _chaosPluralCaption = _langLT.ChaosPluralCaption;
                    _mtfSingularCassie = _langLT.MtfSingularCassie;
                    _mtfPluralCassie = _langLT.MtfPluralCassie;
                    _mtfSingularCaption = _langLT.MtfSingularCaption;
                    _mtfPluralCaption = _langLT.MtfPluralCaption;
                    _scientistSingularCassie = _langLT.ScientistSingularCassie;
                    _scientistPluralCassie = _langLT.ScientistPluralCassie;
                    _scientistSingularCaption = _langLT.ScientistSingularCaption;
                    _scientistPluralCaption = _langLT.ScientistPluralCaption;
                    break;
                case "FR":
                    _scanStartCassie = _langFR.ScanStartMessageCassie;
                    _scanStartCaption = _langFR.ScanStartMessageCaption;
                    _scanNobodyCassie = _langFR.ScanNobodyMessageCassie;
                    _scanNobodyCaption = _langFR.ScanNobodyMessageCaption;
                    _scanCompleteCassie = _langFR.ScanCompleteCassie;
                    _scanCompleteCaption = _langFR.ScanCompleteCaption;
                    _scpSingularCassie = _langFR.ScpSingularCassie;
                    _scpPluralCassie = _langFR.ScpPluralCassie;
                    _scpSingularCaption = _langFR.ScpSingularCaption;
                    _scpPluralCaption = _langFR.ScpPluralCaption;
                    _classDSingularCassie = _langFR.ClassDSingularCassie;
                    _classDPluralCassie = _langFR.ClassDPluralCassie;
                    _classDSingularCaption = _langFR.ClassDSingularCaption;
                    _classDPluralCaption = _langFR.ClassDPluralCaption;
                    _guardSingularCassie = _langFR.GuardSingularCassie;
                    _guardPluralCassie = _langFR.GuardPluralCassie;
                    _guardSingularCaption = _langFR.GuardSingularCaption;
                    _guardPluralCaption = _langFR.GuardPluralCaption;
                    _chaosSingularCassie = _langFR.ChaosSingularCassie;
                    _chaosPluralCassie = _langFR.ChaosPluralCassie;
                    _chaosSingularCaption = _langFR.ChaosSingularCaption;
                    _chaosPluralCaption = _langFR.ChaosPluralCaption;
                    _mtfSingularCassie = _langFR.MtfSingularCassie;
                    _mtfPluralCassie = _langFR.MtfPluralCassie;
                    _mtfSingularCaption = _langFR.MtfSingularCaption;
                    _mtfPluralCaption = _langFR.MtfPluralCaption;
                    _scientistSingularCassie = _langFR.ScientistSingularCassie;
                    _scientistPluralCassie = _langFR.ScientistPluralCassie;
                    _scientistSingularCaption = _langFR.ScientistSingularCaption;
                    _scientistPluralCaption = _langFR.ScientistPluralCaption;
                    break;
                default:
                    _scanStartCassie = _langEN.ScanStartMessageCassie;
                    _scanStartCaption = _langEN.ScanStartMessageCaption;
                    _scanNobodyCassie = _langEN.ScanNobodyMessageCassie;
                    _scanNobodyCaption = _langEN.ScanNobodyMessageCaption;
                    _scanCompleteCassie = _langEN.ScanCompleteCassie;
                    _scanCompleteCaption = _langEN.ScanCompleteCaption;
                    _scpSingularCassie = _langEN.ScpSingularCassie;
                    _scpPluralCassie = _langEN.ScpPluralCassie;
                    _scpSingularCaption = _langEN.ScpSingularCaption;
                    _scpPluralCaption = _langEN.ScpPluralCaption;
                    _classDSingularCassie = _langEN.ClassDSingularCassie;
                    _classDPluralCassie = _langEN.ClassDPluralCassie;
                    _classDSingularCaption = _langEN.ClassDSingularCaption;
                    _classDPluralCaption = _langEN.ClassDPluralCaption;
                    _guardSingularCassie = _langEN.GuardSingularCassie;
                    _guardPluralCassie = _langEN.GuardPluralCassie;
                    _guardSingularCaption = _langEN.GuardSingularCaption;
                    _guardPluralCaption = _langEN.GuardPluralCaption;
                    _chaosSingularCassie = _langEN.ChaosSingularCassie;
                    _chaosPluralCassie = _langEN.ChaosPluralCassie;
                    _chaosSingularCaption = _langEN.ChaosSingularCaption;
                    _chaosPluralCaption = _langEN.ChaosPluralCaption;
                    _mtfSingularCassie = _langEN.MtfSingularCassie;
                    _mtfPluralCassie = _langEN.MtfPluralCassie;
                    _mtfSingularCaption = _langEN.MtfSingularCaption;
                    _mtfPluralCaption = _langEN.MtfPluralCaption;
                    _scientistSingularCassie = _langEN.ScientistSingularCassie;
                    _scientistPluralCassie = _langEN.ScientistPluralCassie;
                    _scientistSingularCaption = _langEN.ScientistSingularCaption;
                    _scientistPluralCaption = _langEN.ScientistPluralCaption;
                    break;
            }
        }

        private void OnRoundStarted()
        {
            ScanInProgress = false;
            _scanCancellationToken?.Cancel();
            _scanCancellationToken = new CancellationTokenSource();
            if (Config.RegularScanning)
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
            if (!Config.ScanAfterNuke)
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
                    string text = _scanStartCassie.Replace("{LENGTH}", Config.ScanLength.ToString());
                    string text2 = _scanStartCaption.Replace("{LENGTH}", Config.ScanLength.ToString());
                    Announcer.Message(text, text2, true, 0f, 1f);
                    await Task.Delay(TimeSpan.FromSeconds(Config.ScanLength), cancellationToken);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        PerformScan();
                    }
                    await Task.Delay(TimeSpan.FromMinutes(Config.DelayAfterScanMinutes), cancellationToken);
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

        private string FormatClassCount(int count, string singularCassie, string pluralCassie)
        {
            string template = count == 1 ? singularCassie : pluralCassie;
            return template.Replace("{COUNT}", count.ToString());
        }

        private string FormatClassCountCaption(int count, string singularCaption, string pluralCaption)
        {
            string template = count == 1 ? singularCaption : pluralCaption;
            return template.Replace("{COUNT}", count.ToString());
        }

        public void PerformScan()
        {
            try
            {
                int scpCount = 0;
                int classDCount = 0;
                int scientistCount = 0;
                int guardCount = 0;
                int mtfCount = 0;
                int chaosCount = 0;
                foreach (Player item in Player.List)
                {
                    if (!item.IsAlive)
                    {
                        continue;
                    }
                    
                    switch (item.Team)
                    {
                        case Team.SCPs:
                            scpCount++;
                            break;
                        case Team.ClassD:
                            classDCount++;
                            break;
                        case Team.Scientists:
                            scientistCount++;
                            break;
                        case Team.FoundationForces:
                            if (item.Role == RoleTypeId.FacilityGuard)
                            {
                                guardCount++;
                            }
                            else
                            {
                                mtfCount++;
                            }
                            break;
                        case Team.ChaosInsurgency:
                            chaosCount++;
                            break;
                    }
                }
                if (scpCount + classDCount + scientistCount + guardCount + mtfCount + chaosCount == 0)
                {
                    Announcer.Message(_scanNobodyCassie, _scanNobodyCaption, true, 0f, 1f);
                    ScanInProgress = false;
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder(_scanCompleteCassie);
                StringBuilder stringBuilder2 = new StringBuilder(_scanCompleteCaption);
                List<string> list = new List<string>();
                List<string> list2 = new List<string>();
                if (scpCount > 0)
                {
                    list.Add(FormatClassCount(scpCount, _scpSingularCassie, _scpPluralCassie));
                    list2.Add(FormatClassCountCaption(scpCount, _scpSingularCaption, _scpPluralCaption));
                }
                if (classDCount > 0)
                {
                    list.Add(FormatClassCount(classDCount, _classDSingularCassie, _classDPluralCassie));
                    list2.Add(FormatClassCountCaption(classDCount, _classDSingularCaption, _classDPluralCaption));
                }
                if (guardCount > 0)
                {
                    list.Add(FormatClassCount(guardCount, _guardSingularCassie, _guardPluralCassie));
                    list2.Add(FormatClassCountCaption(guardCount, _guardSingularCaption, _guardPluralCaption));
                }
                if (chaosCount > 0)
                {
                    list.Add(FormatClassCount(chaosCount, _chaosSingularCassie, _chaosPluralCassie));
                    list2.Add(FormatClassCountCaption(chaosCount, _chaosSingularCaption, _chaosPluralCaption));
                }
                if (mtfCount > 0)
                {
                    list.Add(FormatClassCount(mtfCount, _mtfSingularCassie, _mtfPluralCassie));
                    list2.Add(FormatClassCountCaption(mtfCount, _mtfSingularCaption, _mtfPluralCaption));
                }
                if (scientistCount > 0)
                {
                    list.Add(FormatClassCount(scientistCount, _scientistSingularCassie, _scientistPluralCassie));
                    list2.Add(FormatClassCountCaption(scientistCount, _scientistSingularCaption, _scientistPluralCaption));
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
