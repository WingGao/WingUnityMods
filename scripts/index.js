const log4js = require('log4js');
const path = require('path');
const fs = require('fs');
const _ = require('lodash');
const cpy = require('cpy');
const execa = require('execa');
const iconv = require('iconv-lite');

const logger = log4js.getLogger();
logger.level = 'debug';

const SteamGameDirs = [
  'e:\\Program Files (x86)\\Steam\\steamapps\\common',
  'd:\\Program Files (x86)\\Steam\\steamapps\\common',
  'D:\\Projs\\UnityDemo1\\Temp',
]
const ModConfigs = [
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

const ModLibs = [
  '0Harmony.dll', 'websocket-sharp.dll'
]
const MsBuildBin = [
  'd:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\MSBuild\\Current\\Bin\\MSBuild.exe',
  'e:\\Program Files\\Microsoft Visual Studio\\2022\\Preview\\MSBuild\\Current\\Bin\\MSBuild.exe'
].find(v => fs.existsSync(v))
const SteamBin = SteamGameDirs.map(v => path.resolve(v, '../../steam.exe')).find(v => fs.existsSync(v))

const rootDir = path.resolve(__dirname, '../')

let currentGameConfig

function buildGameConfig(cnf) {
  // 找到游戏具体目录
  let gameSteam = SteamGameDirs.map(sDir => {
    return path.resolve(sDir, cnf.gameDir)
  }).filter(v => fs.existsSync(v))
  if (_.size(gameSteam) == 0) {
    throw new Error('无法找到目录')
  }
  let full = _.merge({}, cnf)
  full.gameDir = gameSteam[0]
  if (cnf.modTarget) {
    full.targetDll = path.basename(cnf.modTarget)
    full.targetDir = path.resolve(full.gameDir, path.dirname(cnf.modTarget))
  }
  full.modProjectDir = path.resolve(rootDir, cnf.modProjectName)
  full.modProjectOutDir = path.resolve(full.modProjectDir, 'bin\\Debug')
  return full
}

function getMonoModDir() {
  let dir = _.last(fs.readdirSync(rootDir).filter(v => {
    return v.startsWith('MonoMod')
  }))
  dir = path.resolve(rootDir, dir)
  logger.debug('MonoMod目录', dir)
  return dir
}

async function copyMonoMod(cnf) {
  let monoDir = getMonoModDir()
  logger.info('[MonoMod] 将复制到', cnf.targetDir)
  await cpy(['*'], cnf.targetDir, { cwd: monoDir });
  logger.info('[MonoMod] 复制完毕');
}


// 复制自己的mod
async function copyWingMod_MM(cnf) {
  let debugDir = cnf.modProjectOutDir
  logger.info('[WingMod] Debug目录', debugDir)
  let modFiles = ModLibs.concat([
    cnf.targetDll.replace('.dll', '.mm.dll'),
    cnf.targetDll.replace('.dll', '.mm.pdb'),
  ])
  logger.info('[WingMod] 目标目录', cnf.targetDir)
  // 备份
  let targetBackUp = path.resolve(cnf.targetDir, cnf.targetDll + '.wingback')
  let targetPath = path.resolve(cnf.targetDir, cnf.targetDll)
  if (!fs.existsSync(targetBackUp)) {
    logger.info(`[WingMod] 备份 ${cnf.targetDll} 到 ${targetBackUp}`)
    fs.copyFileSync(targetPath, targetBackUp)
  } else {
    // 复原
    logger.info(`[WingMod] 恢复 ${targetBackUp} ==> ${cnf.targetDll}`)
    fs.copyFileSync(targetBackUp, targetPath)
  }
  await cpy(modFiles, cnf.targetDir, { cwd: debugDir });
  logger.info('[WingMod] 复制mod完毕', modFiles)
}

async function copyWingMod_UMM(cnf) {
  let debugDir = cnf.modProjectOutDir
  logger.info('[copyWingMod_UMM] Debug目录', debugDir)
  let modFiles = ['Info.json', 'WingMod.dll', 'WingUtil.dll', 'WingUtil.Harmony.dll','ReadMe.*']
  let targetDir = path.resolve(cnf.gameDir, cnf.ummTargetDir)
  if (cnf.ummCreateDir) targetDir = path.resolve(targetDir, 'WingMod')
  if (!fs.existsSync(targetDir)) fs.mkdirSync(targetDir)
  logger.info('[copyWingMod_UMM] 目标目录', targetDir)
  fs.readdirSync(targetDir).forEach(file => {
    if (file.startsWith('WingMod.')) { // 删除缓存
      fs.unlinkSync(path.resolve(targetDir, file))
    }
  })
  await cpy(modFiles, targetDir, { cwd: debugDir });
  logger.info('[copyWingMod_UMM] 复制mod完毕', modFiles)
}

async function patchMod(cnf) {
  try {
    let p = execa('MonoMod.exe', [path.basename(cnf.modTarget)], { encoding: 'binary', cwd: cnf.targetDir });
    p.stdout.pipe(process.stdout);
    // p.stderr.pipe(process.stderr);
    await p
    let patchedDll = path.resolve(cnf.targetDir, 'MONOMODDED_' + cnf.targetDll)
    logger.info('完成Patch', patchedDll)
    // 替换
    fs.renameSync(patchedDll, path.resolve(cnf.gameDir, cnf.modTarget))
    logger.info('完成替换', cnf.modTarget)
  } catch (error) {
    logger.error(error)
    logger.error(iconv.decode(Buffer.from(error.message, 'binary'), 'cp936'))
  }
}

// 编译当前项目
async function buildProject(cnf) {
  try {
    let p = execa(MsBuildBin, [], { encoding: 'binary', cwd: cnf.modProjectDir });
    p.stdout.on('data', s => {
      console.log(iconv.decode(Buffer.from(s, 'binary'), 'gbk'))
    })
    // p.stderr.pipe(process.stderr);
    await p
    // let patchedDll = path.resolve(cnf.targetDir, 'MONOMODDED_' + cnf.targetDll)
    // logger.info('完成Patch', patchedDll)
    // // 替换
    // fs.renameSync(patchedDll, path.resolve(cnf.gameDir, cnf.modTarget))
    // logger.info('完成替换', cnf.modTarget)
  } catch (error) {
    logger.error(error)
    logger.error(iconv.decode(Buffer.from(error.message, 'binary'), 'cp936'))
    throw error
  }
}

async function main() {
  let selectedMod = ModConfigs[0]
  logger.info('当前游戏', selectedMod.name)
  currentGameConfig = buildGameConfig(selectedMod)
  await buildProject(currentGameConfig)
  if (currentGameConfig.ummTargetDir == null) { // MonoMod
    await copyMonoMod(currentGameConfig)
    await copyWingMod_MM(currentGameConfig)
    await patchMod(currentGameConfig)
  } else { // UnityModManger
    await copyWingMod_UMM(currentGameConfig)
  }
  if (currentGameConfig.steamId) {
    // let steamUrl = 'steam://rungameid/908100'
    let steamUrl = `steam://rungameid/${currentGameConfig.steamId}`
    await execa(SteamBin, [steamUrl])
  }
}

main().then(() => {
  process.exit(0)
})
