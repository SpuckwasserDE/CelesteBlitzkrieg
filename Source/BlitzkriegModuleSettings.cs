using System.ComponentModel.DataAnnotations;
using Microsoft.Xna.Framework.Input;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Blitzkrieg;

public class BlitzkriegModuleSettings : EverestModuleSettings 
{
    /*[DefaultButtonBinding(button: Buttons.RightTrigger, key: Keys.D1)]
    public ButtonBinding StartRecordingPath { get; set; }

    [DefaultButtonBinding(button: Buttons.LeftTrigger, key: Keys.D2)]
    public ButtonBinding SavePath { get; set; }*/

    [DefaultButtonBinding(buttons: null, keys: new[] {Keys.D1})]
    public ButtonBinding PlaceCheckpoint { get; set; }

    [DefaultButtonBinding(buttons: null, keys: new[] {Keys.D2})]
    public ButtonBinding DeleteCheckpoint { get; set; }

    [DefaultButtonBinding(buttons: null, keys: new[] {Keys.NumPad4})]
    public ButtonBinding PreviousCheckpoint { get; set; }

    [DefaultButtonBinding(buttons: null, keys: new[] {Keys.NumPad5})]
    public ButtonBinding RecommendedCheckpoint { get; set; }

    [DefaultButtonBinding(buttons: null, keys: new[] {Keys.NumPad6})]
    public ButtonBinding NextCheckpoint { get; set; }

    [DefaultButtonBinding(buttons: null, keys: new[] {Keys.NumPad8})]
    public ButtonBinding SwitchCoreMode { get; set; }

    //[SettingSubHeader("SpuckGD_Blitzkrieg_SettingsHeader_Toggles")]
    [SettingSubText("Enables switching between saved Respawnpoints. Needed for 'Use Blitzkrieg' to be enabled.")]
    public bool EnableRespawnSwitcher { get; set; } = true;
    public bool UseBlitzkrieg { get; set; } = true;    

    [YamlIgnore]
    public bool StartRecordingDummy { get; set; }

    [SettingSubText("This exists to reduce the need for Keybinds on Controller")]
    public BlitzkriegSettingsSubmenuExtraButtons ExtraButtons { get; set; } = new();

    [SettingSubHeader("SpuckGD_Blitzkrieg_SettingsHeader_Overlay")]
    public bool EnableTextOverlay { get; set; } = true;

    [SettingRange(0, 10)]
    public int TextSize { get; set; } = 5;

    [YamlIgnore]
    private static bool enableDeleteButtonAnyways = false;

    [YamlIgnore]
    public TextMenu.OnOff UseBlitzkriegToggle;


    public void CreateStartRecordingDummyEntry(TextMenu menu, bool inGame)
    {
        if (!inGame)
        {
            return;
        }

        string startButtonText = "Start Recording";
        if (BlitzkriegModule.isRecording)
        {
            startButtonText = "End Recording";
        }

        TextMenu.Button startRecordingButton = new(startButtonText);
        TextMenu.Button deleteProfileButton = new("Delete this Levels Profile");

        startRecordingButton.Pressed(() => startRecordingButtonShellFunction(startRecordingButton, deleteProfileButton));
        deleteProfileButton.Pressed(() => deleteProfileButton.Disabled = DeleteProfileButtonOnPressed());

        if (BlitzkriegModule.currentProfileIndex < 0 || BlitzkriegModule.isRecording)
        {
            deleteProfileButton.Disabled = true;
        }
        else
        {
            deleteProfileButton.Disabled = false;
        }
        //startRecordingButton.AddDescription(menu, "Enables Saving Respawnpoints as Checkpoints. Deletes any previous recorded Paths on this Level.");
        //deleteProfileButton.AddDescription(menu, "Deletes all of the Saved Info for this Level");

        menu.Add(startRecordingButton);
        menu.Add(deleteProfileButton);
    }

    public void CreateUseBlitzkriegEntry (TextMenu menu, bool inGame)
    {
        menu.Add(UseBlitzkriegToggle = new TextMenu.OnOff("Use Blitzkrieg", on: UseBlitzkrieg == true));

        UseBlitzkriegToggle.Disabled = !EnableRespawnSwitcher;
        UseBlitzkriegToggle.AddDescription(menu, "Enables the Blitzkrieg Algorithm and everything that comes with it.");

        UseBlitzkriegToggle.Change(newValue => UseBlitzkrieg = newValue ? true : false);
    }

    public string StartRecordingButtonOnPressed()
    {
        if (BlitzkriegModule.isRecording)
        {
            BlitzkriegModule.startRecording = false;
            BlitzkriegModule.endRecording = true;
            enableDeleteButtonAnyways = true;
            return "Start Recording";
        }
        else
        {
            BlitzkriegModule.endRecording = false;
            BlitzkriegModule.startRecording = true;
            return "End Recording";
        }
    }

    public bool DeleteProfileButtonOnPressed()
    {
        BlitzkriegModule.DeleteCurrentProfile();
        return true;
    }

    private void startRecordingButtonShellFunction(TextMenu.Button startButton, TextMenu.Button deleteButton)
    {
        startButton.Label = StartRecordingButtonOnPressed();
        if (enableDeleteButtonAnyways)
        {
            deleteButton.Disabled = false;
            enableDeleteButtonAnyways = false;
        }
        else
        {
            deleteButton.Disabled = true;
        }
    }
}