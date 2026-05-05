using YamlDotNet.Serialization;

namespace Celeste.Mod.Blitzkrieg;

[SettingSubMenu]
public class BlitzkriegSettingsSubmenuExtraButtons
{
    [YamlIgnore]
    public bool SubMenuButtonDummy { get; set; }

    public void CreateSubMenuButtonDummyEntry(TextMenuExt.SubMenu subMenu, bool inGame)
    {
        if (!inGame)
        {
            return;
        }

        TextMenu.Button placeCheckpointButton = new("Place Checkpoint");
        TextMenu.Button deleteCheckpointButton = new("Delete Checkpoint");
        TextMenu.Button switchRightButton = new("Go To Next Checkpoint");
        TextMenu.Button switchLeftButton = new("Go To Previous Checkpoint");
        TextMenu.Button switchRecommendedButton = new("Go To Recommended Checkpoint");
        TextMenu.Button switchCoreMode = new("Switch Core Mode");

        placeCheckpointButton.Pressed(() => BlitzkriegModule.placeCheckoint = true);
        deleteCheckpointButton.Pressed(() => BlitzkriegModule.deleteCheckoint = true);
        switchRightButton.Pressed(() => BlitzkriegModule.switchRight = true);
        switchLeftButton.Pressed(() => BlitzkriegModule.switchLeft = true);
        switchRecommendedButton.Pressed(() => BlitzkriegModule.switchRecommended = true);
        switchCoreMode.Pressed(() => BlitzkriegModule.switchCore = true);

        subMenu.Add(placeCheckpointButton);
        subMenu.Add(deleteCheckpointButton);
        subMenu.Add(switchRightButton);
        subMenu.Add(switchLeftButton);
        subMenu.Add(switchRecommendedButton);
        subMenu.Add(switchCoreMode);
    }
}