using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Blitzkrieg;

public class BlitzkriegModuleSaveData : EverestModuleSaveData 
{
    /*public List<Vector2> respawnPointsPath { get; set; } = new List<Vector2>();
    public List<string> roomNamesPath { get; set; } = new List<string>();
    public AreaKey blitzkriegLevel { get; set; } = AreaKey.Default;
    public int blitzkriegStage { get; set; } = 1;
    public List<bool> runsCompleted { get; set; } = new List<bool>();*/

    public List<BlitzkriegProfile> blitzkriegProfiles { get; set; } = new List<BlitzkriegProfile>();
}