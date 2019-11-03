using System;
using UnityEngine;
using UnityModManagerNet;

public class Mod
{
	public Mod()
	{
       
    }
    public void OnGUI(UnityModManager.ModEntry obj)
    {
        GUILayout.Label("God mode");
        var health = GUILayout.TextField("223", GUILayout.Width(100f));
        var ammo = GUILayout.TextField("123", GUILayout.Width(100f));
        if (GUILayout.Button("Apply") && int.TryParse(health, out var h) && int.TryParse(ammo, out var a))
        {
            //Player.health = h;
            //Player.weapon.ammo = a;
        }
    }
}
