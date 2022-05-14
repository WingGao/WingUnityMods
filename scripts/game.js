const ModConfigs = [
    {
        steamId: 736190,
        name: '中国式家长',
        gameDir: 'ChineseParent',
        modProjectName: 'BIE6_ChineseParent',
        bie: 6
    },
    {
        steamId: 1296830,
        name: '暖雪',
        gameDir: 'WarmSnow',
        modProjectName: 'WarmSnow.WingMod.umm',
        ummTargetDir: 'Mods\\WingMod',
        ummCreateDir: false //是否创建上级目录
    },
    {
        steamId: 908100,
        name: '九州商旅',
        gameDir: 'Nine Provinces Caravan',
        modProjectName: 'NineProvincesCaravan.WingMod.umm',
        // ummTargetDir: 'Nine_Data\\StreamingAssets\\Mods'
        ummTargetDir: 'Nine_Data\\StreamingAssets\\workshopAuthor\\2577322910',
        ummCreateDir: false //是否创建上级目录
    },
    {
        name: '霓虹深渊',
        gameDir: 'Neon Abyss',
        modTarget: 'NeonAbyss_Data\\Managed\\Assembly-CSharp.dll',
        //d:\Projs\WingUnityMods\NeonAbyss.WingMod.mm\bin\Debug\0Harmony.dll
        modProjectName: 'NeonAbyss.WingMod.mm'
    },
    {
        name: 'Demo1',
        gameDir: 'UnityDemo1',
        modTarget: 'UnityDemo1_Data\\Managed\\UnityEngine.UI.dll',
        //d:\Projs\WingUnityMods\NeonAbyss.WingMod.mm\bin\Debug\0Harmony.dll
        modProjectName: 'UnityDemo1.WingMod.mm'
    }
]
module.exports = ModConfigs