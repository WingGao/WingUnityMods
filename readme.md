# Wing的Unity相关Mod


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
* 所有的mod最终都要生成`WingMod.dll`

### BepInEx
* https://github.com/BepInEx/HarmonyX
* https://harmony.pardeike.net/articles/patching-injections.html