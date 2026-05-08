using System;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Blitzkrieg.Entities;
using static Celeste.Session;
using System.Linq;
using System.Collections.Generic;
using Iced.Intel;
using System.Threading;
using Microsoft.VisualBasic;

namespace Celeste.Mod.Blitzkrieg;

public class BlitzkriegModule : EverestModule {
    public static BlitzkriegModule Instance { get; private set; }

    public override Type SettingsType => typeof(BlitzkriegModuleSettings);
    public static BlitzkriegModuleSettings Settings => (BlitzkriegModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(BlitzkriegModuleSession);
    public static BlitzkriegModuleSession Session => (BlitzkriegModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(BlitzkriegModuleSaveData);
    public static BlitzkriegModuleSaveData SaveData => (BlitzkriegModuleSaveData) Instance._SaveData;

    public static bool isRecording = false;
    private static bool reloadOverlay = false;
    public static bool startRecording = false;
    public static bool endRecording = false;
    public static bool placeCheckoint = false;
    public static bool deleteCheckoint = false;
    public static bool switchRight = false;
    public static bool switchLeft = false;
    public static bool switchRecommended = false;
    public static bool switchCore = false;
    private static bool delayedRespawn = false;
    private static int roomCheck = -1;
    private static int currentRespawnPointIndex = 0;
    private static int currentRunProgressIndex = 0;
    public static int currentProfileIndex = -1;
    private static string coreLockRoom = null;
    private static CoreModes coreLockMode = CoreModes.None;
    private static BlitzkriegTextOverlay textOverlay;


    public BlitzkriegModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(BlitzkriegModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(BlitzkriegModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        // TODO: apply any hooks that should always be active
        Everest.Events.Level.OnBeforeUpdate += LevelUpdate;
        Everest.Events.Player.OnDie += PlayerOnDie;
        Everest.Events.Level.OnExit += LevelOnExit;
        Everest.Events.Level.OnComplete += LevelOnComplete;
        Everest.Events.Player.OnSpawn += PlayerOnRespawn;
        Everest.Events.Session.OnSliderChanged += OnSliderChanged;
        Everest.Events.Level.OnEnter += LevelOnEnter;  
    }

    private void LevelUpdate(Level level)
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (level.Tracker.GetEntity<Player>() != null)
            {
                if (startRecording && endRecording)
                {
                    startRecording = false;
                    endRecording = false;
                }
                else if (startRecording)
                {
                    RecordPath(level);
                    startRecording = false;
                }
                else if (endRecording && isRecording)
                {
                    SavePath();
                    endRecording = false;
                }
                else if (isRecording)
                {
                    if (Settings.PlaceCheckpoint.Pressed || placeCheckoint)
                    {
                        PlaceCheckpoint(level);
                        placeCheckoint = false;
                    }
                    else if ((Settings.DeleteCheckpoint.Pressed || deleteCheckoint) && profile.respawnPointsPath.Count > 0)
                    {   
                        DeleteCheckpoint();
                        deleteCheckoint = false;
                    }
                }
                else if (!isRecording && profile.respawnPointsPath.Count > 0 && level.Session.Area == profile.blitzkriegLevel)
                {
                    if (Settings.RecommendedCheckpoint.Pressed || switchRecommended)
                    {
                        if (Settings.UseBlitzkrieg)
                        {
                            currentRespawnPointIndex = GetRecommendedRunStartIndex();
                        }
                        GoToCheckpoint(currentRespawnPointIndex, level);
                        switchRecommended = false;
                    }
                    else if (Settings.PreviousCheckpoint.Pressed || switchLeft)
                    {
                        GoToPreviousCheckpoint(level);
                        switchLeft = false;
                    }
                    else if (Settings.NextCheckpoint.Pressed || switchRight)
                    {
                        GoToNextCheckpoint(level);
                        switchRight = false;
                    }
                    else if (Settings.SwitchCoreMode.Pressed || switchCore)
                    {
                        SwitchCoreMode(level);
                        switchCore = false;
                    }
                    else if (currentRespawnPointIndex + currentRunProgressIndex + 1 <= profile.respawnPointsPath.Count - 1 && Settings.UseBlitzkrieg)
                    {
                        if (level.Session.RespawnPoint == profile.respawnPointsPath[currentRespawnPointIndex + currentRunProgressIndex + 1])
                        {
                            currentRunProgressIndex++;
                        }
                    }
                    if (roomCheck != -1)
                    {       
                        if (roomCheck < profile.respawnPointsPath.Count - 1)
                        {
                            if (profile.respawnPointsPath[roomCheck + 1] != level.Session.RespawnPoint)
                            {
                                currentRunProgressIndex = 0;
                                if (profile.respawnPointsPath[currentRespawnPointIndex] != level.Session.RespawnPoint)
                                {
                                    int newIndex = profile.respawnPointsPath.IndexOf((Vector2)level.Session.RespawnPoint);
                                    if (newIndex >= 0)
                                    {
                                        currentRespawnPointIndex = newIndex;
                                    }                  
                                }
                            }
                        }
                        else
                        {
                            currentRunProgressIndex = 0;
                            if (profile.respawnPointsPath[currentRespawnPointIndex] != level.Session.RespawnPoint)
                            {
                                int newIndex = profile.respawnPointsPath.IndexOf((Vector2)level.Session.RespawnPoint);
                                if (newIndex >= 0)
                                {
                                    currentRespawnPointIndex = newIndex;
                                }                   
                            }
                        } 
                        roomCheck = -1;
                    }
                }
            }

            if (Settings.EnableTextOverlay && level.Session.Area == profile.blitzkriegLevel)
            {
                if (textOverlay == null)
                {
                    textOverlay = new BlitzkriegTextOverlay(new Vector2(5, 125), "", Settings.TextSize, Dialog.Language.Font);
                    level.Add(textOverlay);
                }

                if (isRecording)
                {
                    textOverlay.SetText(textOverlay.GetRecordingText(profile.roomNamesPath, level.Session.Level));
                }
                else if (profile.respawnPointsPath.Count > 0 && Settings.UseBlitzkrieg)
                {
                    int startIndex = GetRecommendedRunStartIndex();
                    string endRoomName = "End";
                    int endIndex = profile.respawnPointsPath.Count - 1;
                    if (startIndex + profile.blitzkriegStage < profile.roomNamesPath.Count)
                    {
                        endRoomName = profile.roomNamesPath[startIndex + profile.blitzkriegStage];
                        endIndex = startIndex + profile.blitzkriegStage;
                    }

                    textOverlay.SetText(textOverlay.GetRecommendedText(level.Session.Level, profile.roomNamesPath[startIndex], endRoomName,
                        profile.respawnPointsPath.Count, startIndex + 1, endIndex + 1, currentRespawnPointIndex + currentRunProgressIndex + 1, profile.blitzkriegStage, currentRunProgressIndex));

                    if (reloadOverlay)
                    {
                        UpdateTextOverlay(level);
                        reloadOverlay = false;
                    }
                }
                else
                {
                    textOverlay.SetText("");
                }            
            }
            else if ((!Settings.EnableTextOverlay || !Settings.UseBlitzkrieg) && textOverlay != null)
            {
                level.Remove(textOverlay);
                textOverlay = null;
            }



            if (profile.blitzkriegLevel != level.Session.Area)
            {
                BlitzkriegProfile profileAlt = GetProfile(level.Session.Area);
                if (profileAlt != null)
                {
                    currentProfileIndex = SaveData.blitzkriegProfiles.IndexOf(profileAlt);
                }
                else
                {
                    currentProfileIndex = -1;
                }            
            }
        }
        else if (GetProfile(level.Session.Area) != null)
        {
            currentProfileIndex = SaveData.blitzkriegProfiles.IndexOf(GetProfile(level.Session.Area));
        }
        else if (startRecording && endRecording)
        {
            startRecording = false;
            endRecording = false;
        }
        else if (level.Tracker.GetEntity<Player>() != null && startRecording)
        {
            RecordPath(level);
        }
    }

    private void PlayerOnDie(Player player)
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (Settings.UseBlitzkrieg && !isRecording && profile.respawnPointsPath.Count > 0 && player.level.Session.Area == profile.blitzkriegLevel)
            {
                CheckRunComplete();
                if (!player.level.Session.GrabbedGolden)
                {
                    if (!Settings.DisableInstantRespawn)
                    {
                        player.level.Session.Level = profile.roomNamesPath[currentRespawnPointIndex];
                        player.level.Session.RespawnPoint = profile.respawnPointsPath[currentRespawnPointIndex];
                        Engine.Scene = new LevelLoader(player.level.Session, Vector2.Zero);
                    }
                    else
                    {
                        delayedRespawn = true;
                    }
                }
                currentRunProgressIndex = 0;
            }
        }
    }

    private void LevelOnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
    {
        if (currentProfileIndex >= 0)
        {
            if (isRecording)
            {
                DeleteCurrentProfile();
                isRecording = false;
                Logger.Log(LogLevel.Info, nameof(BlitzkriegModule), "Stopped recording path due to level exit.");
            }
            currentRunProgressIndex = 0;
            currentRespawnPointIndex = 0;
            coreLockRoom = "";
        }
        if (textOverlay != null && Settings.EnableTextOverlay)
        {
            textOverlay.RemoveSelf();
            textOverlay = null;
        }
    }

    private void LevelOnComplete(Level level)
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (Settings.UseBlitzkrieg && !isRecording && level.Session.Area == profile.blitzkriegLevel &&
                currentRespawnPointIndex + currentRunProgressIndex == profile.respawnPointsPath.Count - 1)
            {
                currentRunProgressIndex++;
                CheckRunComplete();
            }
        }
    }

    private void PlayerOnRespawn(Player player)
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (Settings.EnableRespawnSwitcher && profile.respawnPointsPath.Count > 0 && player.level.Session.Area == profile.blitzkriegLevel)
            {
                if (Settings.EnableTextOverlay && textOverlay != null)
                {
                    if (textOverlay.Scene != player.level)
                    {
                        player.level.Add(textOverlay);
                    }  
                }
                if (!isRecording)
                {
                    roomCheck = currentRespawnPointIndex + currentRunProgressIndex;
                    
                    if (coreLockRoom == player.level.Session.Level)
                    {
                        player.level.CoreMode = coreLockMode;
                    }

                    if (delayedRespawn)
                    {
                        delayedRespawn = false;
                        player.level.Session.Level = profile.roomNamesPath[currentRespawnPointIndex];
                        player.level.Session.RespawnPoint = profile.respawnPointsPath[currentRespawnPointIndex];
                        Engine.Scene = new LevelLoader(player.level.Session, Vector2.Zero);
                    }
                }
            }            
        }
    }

    private void OnSliderChanged(Session session, Session.Slider slider, float? previous)
    {
        if (Settings.UseBlitzkriegToggle != null)
        {   
            if (Settings.UseBlitzkriegToggle.Disabled)
            {
                Settings.UseBlitzkrieg = false;   
                Settings.UseBlitzkriegToggle.Index = 0;             
            }      
            Settings.UseBlitzkriegToggle.Disabled = !Settings.EnableRespawnSwitcher;      
        }
        if (currentProfileIndex >= 0)
        {
            if (Settings.UseBlitzkrieg && Settings.EnableTextOverlay && textOverlay != null && session.Area == SaveData.blitzkriegProfiles[currentProfileIndex].blitzkriegLevel)
            {
                textOverlay.Update(new Vector2(5, 125), "", Settings.TextSize, Dialog.Language.Font);
            }
        }  
    }

    private void LevelOnEnter(Session session, bool fromSavedata)
    {
        if (textOverlay != null)
        {
            textOverlay.RemoveSelf();
            textOverlay = null;
        }        
        reloadOverlay = true;             
    }


    private static void RecordPath(Level level)
    {
        BlitzkriegProfile profile = GetProfile(level.Session.Area);
        if (profile == null)
        {
            profile = new BlitzkriegProfile();
        }
        else
        {
            SaveData.blitzkriegProfiles.Remove(profile);
        }
        profile.blitzkriegLevel = level.Session.Area;
        profile.respawnPointsPath.Clear();
        profile.roomNamesPath.Clear();
        profile.comletedRunsHistory.Clear();
        profile.runBacklog.Clear();
        SaveData.blitzkriegProfiles.Add(profile);
        currentProfileIndex = SaveData.blitzkriegProfiles.Count - 1;

        UpdateTextOverlay(level);

        isRecording = true;
        PlaceCheckpoint(level);
        Logger.Log(LogLevel.Verbose, nameof(BlitzkriegModule), "Started recording path.");
    }

    private static void SavePath()
    {        
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            isRecording = false;
            currentRespawnPointIndex = profile.respawnPointsPath.Count - 1;
            profile.blitzkriegStage = 1;
            SetupRunsCompleted();

            if (!Settings.UseBlitzkrieg && textOverlay != null)
            {
                textOverlay.RemoveSelf();
                textOverlay = null;
            }
            Logger.Log(LogLevel.Verbose, nameof(BlitzkriegModule), $"Saved path ({profile.runsCompleted.Count} checkpoints).");
        }
    }

    private static void PlaceCheckpoint(Level level)
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (profile.respawnPointsPath.Count >= 1)
            {
                if (profile.respawnPointsPath[profile.respawnPointsPath.Count - 1] == level.Session.RespawnPoint)
                {
                    Logger.Log(LogLevel.Verbose, nameof(BlitzkriegModule), "Current respawn point is the same as the last checkpoint. Not placing a new checkpoint.");
                    return;
                }
            }
            profile.respawnPointsPath.Add((Vector2)level.Session.RespawnPoint);
            profile.roomNamesPath.Add(level.Session.Level);
        } 
    }

    private static void DeleteCheckpoint()
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (profile.respawnPointsPath.Count >= 1)
            {
                profile.respawnPointsPath.RemoveAt(profile.respawnPointsPath.Count - 1);
                profile.roomNamesPath.RemoveAt(profile.roomNamesPath.Count - 1);
            }
        }          
    }

    private static void GoToCheckpoint(int index, Level level)
    {
        if (currentProfileIndex >= 0 && Settings.EnableRespawnSwitcher)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (index < 0 || index >= profile.respawnPointsPath.Count)
            {
                Logger.Log(LogLevel.Warn, nameof(BlitzkriegModule), $"Respawn point index {index} is out of bounds.");
                return;
            }        
            else if (!level.Session.GrabbedGolden)
            {      
                Player player = level.Tracker.GetEntity<Player>();
                Vector2 respawnPoint = profile.respawnPointsPath[index];

                level.Session.Level = profile.roomNamesPath[index];
                level.Session.RespawnPoint = respawnPoint;
                Engine.Scene = new LevelLoader(player.level.Session, Vector2.Zero);                
                level.Reload();          
            }
        }
    }

    private static void GoToPreviousCheckpoint(Level level)
    {
        if (currentProfileIndex >= 0 && Settings.EnableRespawnSwitcher && !level.Session.GrabbedGolden)
        {
            if (currentRespawnPointIndex > 0)
            {
                currentRespawnPointIndex--;
            }
            else
            {
                currentRespawnPointIndex = SaveData.blitzkriegProfiles[currentProfileIndex].respawnPointsPath.Count - 1;
            }
            GoToCheckpoint(currentRespawnPointIndex, level);
        }  
    }

    private static void GoToNextCheckpoint(Level level)
    {
        if (currentProfileIndex >= 0 && Settings.EnableRespawnSwitcher && !level.Session.GrabbedGolden)
        {
            if (currentRespawnPointIndex < SaveData.blitzkriegProfiles[currentProfileIndex].respawnPointsPath.Count - 1)
            {
                currentRespawnPointIndex++;
            }
            else
            {
                currentRespawnPointIndex = 0;
            }
            GoToCheckpoint(currentRespawnPointIndex, level);
        }  
    }

    private static void CheckRunComplete()
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (currentRunProgressIndex >= profile.blitzkriegStage)
            {
                bool runIsBacklogged = false;
                int backloggedRun = 0;
                for (int i = 0; i < profile.runBacklog.Count; i++)
                {
                    if (profile.runBacklog[i] == currentRespawnPointIndex)
                    {
                        runIsBacklogged = true;
                        backloggedRun = i;
                    }
                }
                if (runIsBacklogged)
                {
                    profile.runBacklog.RemoveAt(backloggedRun);
                }
                else
                {
                    profile.runsCompleted[currentRespawnPointIndex] = true;
                }                
                profile.comletedRunsHistory.Add(currentRespawnPointIndex);
                currentRunProgressIndex = 0;

                bool allRunsCompleted = true;
                foreach (bool runCompleted in profile.runsCompleted)
                {
                    if (!runCompleted)
                    {
                        allRunsCompleted = false;
                        break;
                    }
                }

                if (allRunsCompleted)
                {
                    if (profile.runBacklog.Count > 0)
                    {
                        foreach (int run in profile.runBacklog)
                        {
                            profile.runsCompleted[run] = false;
                        }
                        profile.runBacklog.Clear();
                    }
                    else if (profile.blitzkriegStage < profile.respawnPointsPath.Count)
                    {                        
                        profile.blitzkriegStage++;
                        SetupRunsCompleted();
                    }
                }
            }
        }
    }

    public static void SetupRunsCompleted()
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            profile.runsCompleted.Clear();
            for (int i = 0; i < profile.respawnPointsPath.Count - profile.blitzkriegStage + 1; i++)
            {
                profile.runsCompleted.Add(false);
            }
        } 
    }

    public static int GetRecommendedRunStartIndex()
    {
        if (currentProfileIndex >= 0)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            for (int i = profile.runsCompleted.Count - 1; i > 0; i--)
            {
                if (!profile.runsCompleted[i])
                {
                    return i;
                }
            }
        }
        return 0;
    }

    private static BlitzkriegProfile GetProfile(AreaKey area)
    {
        foreach (BlitzkriegProfile p in SaveData.blitzkriegProfiles)
        {
            if (p.blitzkriegLevel == area)
            {
                return p;
            }
        }
        return null;
    }

    private static void UpdateTextOverlay(Level level)
    {
        if (currentProfileIndex >= 0)
        {
            if (Settings.UseBlitzkrieg && Settings.EnableTextOverlay && textOverlay != null && level.Session.Area == SaveData.blitzkriegProfiles[currentProfileIndex].blitzkriegLevel)
            {
                //level.Remove(textOverlay);
                textOverlay.Update(new Vector2(5, 125), "", Settings.TextSize, Dialog.Language.Font);
                if (textOverlay.Scene != level)
                {
                    level.Add(textOverlay);
                }                
            }
        }        
    }

    public static void DeleteCurrentProfile()
    {
        if (currentProfileIndex >= 0)
        {
            SaveData.blitzkriegProfiles.Remove(SaveData.blitzkriegProfiles[currentProfileIndex]);
            currentProfileIndex = -1;
            if (textOverlay != null)
            {
                textOverlay.RemoveSelf();
            }            
        }
    }

    private static void SwitchCoreMode(Level level)
    {
        if (level.CoreMode == CoreModes.Hot)
        {
            level.CoreMode = CoreModes.Cold;
            coreLockRoom = level.Session.Level;
            coreLockMode = CoreModes.Cold;
        }
        else if (level.CoreMode == CoreModes.Cold)
        {
            level.CoreMode = CoreModes.Hot;
            coreLockRoom = level.Session.Level;
            coreLockMode = CoreModes.Hot;
        }
    }

    public static void UncompleteRun(int run)
    {
        if (Settings.UseBlitzkrieg && currentProfileIndex >= 0 && !isRecording)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            bool thisStage = false;
            for (int i = 0; i < profile.runsCompleted.Count; i++)    
            {
                if (profile.runsCompleted[i] && !profile.runBacklog.Contains(i))
                {
                    thisStage = true;
                }
            }

            if (thisStage && run < profile.runsCompleted.Count)
            {
                profile.runsCompleted[run] = false;                
            }
            else
            {
                profile.blitzkriegStage--;
                profile.runsCompleted.Clear();
                for (int i = 0; i < profile.respawnPointsPath.Count - profile.blitzkriegStage + 1; i++)
                {
                    profile.runsCompleted.Add(i != run);
                }
            }
        }
    }

    public static void SwitchRecommendedRun()
    {
        if (Settings.UseBlitzkrieg && currentProfileIndex >= 0 && !isRecording)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            int run = GetRecommendedRunStartIndex();
            if (!profile.runsCompleted[run])
            {                
                profile.runsCompleted[run] = true;
                profile.runBacklog.Add(run);
                CheckRunComplete();
            }
        }
    }

    public static void CompleteStage()
    {
        if (Settings.UseBlitzkrieg && currentProfileIndex >= 0 && !isRecording)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];
            if (profile.blitzkriegStage < profile.respawnPointsPath.Count)
            {
                profile.blitzkriegStage++;
                for (int i = 0; i < profile.runsCompleted.Count; i++)
                {
                    if (!profile.runsCompleted[i])
                    {
                        profile.comletedRunsHistory.Add(i);
                    }
                }
                SetupRunsCompleted();
                profile.runBacklog.Clear();
            }               
        }
    }

    public static void ResetStageProgress()
    {
        if (Settings.UseBlitzkrieg && currentProfileIndex >= 0 && !isRecording)
        {
            BlitzkriegProfile profile = SaveData.blitzkriegProfiles[currentProfileIndex];            
            for (int i = profile.comletedRunsHistory.Count - 1; i >= 0; i--)
            {                
                bool isInBacklog = false;
                foreach (int backloggedRun in profile.runBacklog)
                {
                    if (backloggedRun == profile.comletedRunsHistory[i])
                    {
                        isInBacklog = true;
                    }
                }
                bool allRunsReset = true;
                for (int j = 0; j < profile.runsCompleted.Count; j++)    
                {
                    if (profile.runsCompleted[j] && !profile.runBacklog.Contains(j))
                    {
                        allRunsReset = false;
                    }
                }
                if (!allRunsReset)
                {
                    if (profile.runsCompleted[profile.comletedRunsHistory[i]] && !isInBacklog)
                    {
                        profile.runsCompleted[profile.comletedRunsHistory[i]] = false;
                        profile.comletedRunsHistory.RemoveAt(i);
                    }    
                }                           
            }          
            profile.runBacklog.Clear();
            SetupRunsCompleted();
        }
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        Everest.Events.Level.OnBeforeUpdate -= LevelUpdate;
        Everest.Events.Player.OnDie -= PlayerOnDie;
        Everest.Events.Level.OnExit -= LevelOnExit;
        Everest.Events.Level.OnComplete -= LevelOnComplete;
        Everest.Events.Player.OnSpawn -= PlayerOnRespawn;
        Everest.Events.Session.OnSliderChanged -= OnSliderChanged;
        Everest.Events.Level.OnEnter -= LevelOnEnter;
    }
}