# Wing的Unity相关Mod

* 

## 开发相关
* 创建一个mod工程
* 将游戏`Managed`映射为`_game`, 目录连接
* 添加依赖到 .csproj
```xml
<Reference Include="Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null">
  <HintPath>_game\Assembly-CSharp.dll</HintPath>
</Reference>
<Reference Include="Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null">
  <HintPath>_game\Assembly-CSharp-firstpass.dll</HintPath>
</Reference>
<Reference Include="UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null">
  <HintPath>_game\UnityEngine.CoreModule.dll</HintPath>
</Reference>
 ```
* UMM => 所有的mod最终都要生成`WingMod.dll` 

### UMM
* https://wiki.nexusmods.com/index.php/How_to_create_mod_for_unity_game
* https://wiki.nexusmods.com/index.php/How_to_render_mod_options_(UMM)
#### UI相关
* https://github.com/legendaryhero1981/Dear-Unity-Mod-Manager/blob/master/UnityModManager/UIDraw.cs
* https://docs.unity3d.com/Manual/gui-Layout.html
* https://docs.unity3d.com/Manual/GUIScriptingGuide.html

### BepInEx
* https://github.com/BepInEx/HarmonyX
* https://harmony.pardeike.net/articles/patching-injections.html
* https://docs.bepinex.dev/articles/dev_guide/dev_tools.html
插件版本
* https://github.com/ManlyMarco/RuntimeUnityEditor v3.0


### MonoMod
* https://github.com/MonoMod/MonoMod/blob/master/README-ModInterop.md