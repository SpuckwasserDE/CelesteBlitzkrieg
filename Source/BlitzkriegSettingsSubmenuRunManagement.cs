using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Blitzkrieg;

[SettingSubMenu]
public class BlitzkriegSettingsSubmenuRunManagement
{
    [YamlIgnore]
    public bool SubMenuButtonDummy { get; set; }

    public void CreateSubMenuButtonDummyEntry(TextMenuExt.SubMenu subMenu, bool inGame)
    {
        if (!inGame || !BlitzkriegModule.Settings.UseBlitzkrieg)
        {
            return;
        }

        TextMenu.Button completeRun = new("Complete current Run");
        TextMenu.Button uncompleteRun = new("Reset last completed Run");
        TextMenu.Button skipRun = new("Switch Recommended Run");
        TextMenu.Button completeStage = new("Skip current Stage");
        TextMenu.Button resetStage = new("Reset current Stage Progress");

        completeRun.Pressed(() => CompleteRun());
        uncompleteRun.Pressed(() => UncompleteRun());
        skipRun.Pressed(() => BlitzkriegModule.SwitchRecommendedRun());
        completeStage.Pressed(() => BlitzkriegModule.CompleteStage());
        resetStage.Pressed(() => BlitzkriegModule.ResetStageProgress());

        

        subMenu.Add(completeRun);
        subMenu.Add(uncompleteRun);
        subMenu.Add(skipRun);
        subMenu.Add(completeStage);
        subMenu.Add(resetStage);
    }

    private void CompleteRun()
    {
        int profileIndex = BlitzkriegModule.currentProfileIndex;
        if (profileIndex >= 0 && !BlitzkriegModule.isRecording && BlitzkriegModule.Settings.UseBlitzkrieg)
        {
            BlitzkriegProfile profile = BlitzkriegModule.SaveData.blitzkriegProfiles[profileIndex];      
            int run = BlitzkriegModule.GetRecommendedRunStartIndex();
            profile.runsCompleted[run] = true;
            profile.comletedRunsHistory.Add(run);
            
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
                    foreach (int backloggedRun in profile.runBacklog)
                    {
                        profile.runsCompleted[backloggedRun] = false;
                    }
                    profile.runBacklog.Clear();
                }
                else if (profile.blitzkriegStage < profile.respawnPointsPath.Count)
                {                        
                    profile.blitzkriegStage++;
                    BlitzkriegModule.SetupRunsCompleted();
                }
            }
        }        
    }

    private void UncompleteRun()
    {
        if (BlitzkriegModule.Settings.UseBlitzkrieg)
        {
            int profileIndex = BlitzkriegModule.currentProfileIndex;
            if (profileIndex >= 0)
            {
                List<int> history = BlitzkriegModule.SaveData.blitzkriegProfiles[profileIndex].comletedRunsHistory; 
                int count = history.Count - 1;   
                if (count >= 0)
                {
                    BlitzkriegModule.UncompleteRun(history[count]);
                    history.RemoveAt(count);
                }                   
            }     
        }           
    }
}