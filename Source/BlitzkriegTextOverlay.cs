using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Blitzkrieg.Entities;

public class BlitzkriegTextOverlay : Entity {
    private string text;
    private Color color;
    private Vector2 position;
    private int size;
    private Color OutlineColor = Color.Black;
    private PixelFont font = Dialog.Language.Font;
    private float FontFaceSize = 15f;

    public BlitzkriegTextOverlay(Vector2 position, string text, int size, PixelFont font, Color? color = null) 
    {
        this.AddTag(TagsExt.SubHUD);
        this.AddTag(Tags.Persistent | Tags.Global | Tags.TransitionUpdate | Tags.FrozenUpdate | Tags.PauseUpdate);
        Update(position, text, size, font, color);
        
    }

    public override void Render() 
    {
        font.DrawOutline(FontFaceSize, text, position, Vector2.Zero, Vector2.One * (20 + size) * 0.1f, color, 2f, OutlineColor);
        //Fonts.Get().Draw(1, text, position, color);
    }

    public void SetText(string newText) {
        text = newText;
    }

    public string GetRecommendedText(string currentRoomName, string runStartRoomName, string runEndRoomName,
        int checkpointCount, int runStartIndex, int runEndIndex, int currentRoomIndex, int blitzkriegStage, int runProgress)
    {
        int runLenght = runEndIndex - runStartIndex;
        if (runEndRoomName == "End")
        {
            runLenght++;
        }
        return $"Stage:  {blitzkriegStage}/{checkpointCount}\n" +
               $"Current Room:  {currentRoomName}  ({currentRoomIndex}/{checkpointCount})\n" +
               $"Run Start:  {runStartRoomName}  ({runStartIndex}/{checkpointCount})\n" +
               $"Run End:  {runEndRoomName}  ({runEndIndex}/{checkpointCount})\n" +
               $"Run Progress:  {runProgress}/{runLenght}";
    }

    public string GetRecordingText(List<string> recordedRoomNames, string currentRoomName)
    {
        int i = 0;
        string output = "Current Room: " + currentRoomName + "\nRecorded Path: ";
        foreach (string roomName in recordedRoomNames)
        {
            output += roomName + " | ";
            i += roomName.Length + 3;
            if (i >= 50)
            {
                output.TrimEnd(' ', '|');
                output += "\n           ";
                i = 0;
            }
        }
        output = output.TrimEnd(' ', '|', '\n');
        return output;
    }

    public void Update(Vector2 position, string text, int size, PixelFont font, Color? color = null)
    {
        this.text = text;
        this.color = color ?? Color.White;
        this.position = position;
        this.size = size;
        this.font = font;
    }
}