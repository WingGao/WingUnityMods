const log4js = require('log4js');
const path = require('path');
const fs = require('fs');
const _ = require('lodash');
const cpy = require('cpy');
const execa = require('execa');
const iconv = require('iconv-lite');

const logger = log4js.getLogger();
logger.level = 'debug';

const SteamDirs = [
  'd:\\Program Files (x86)\\Steam\\steamapps\\common',
  'D:\\Projs\\UnityDemo1\\Temp',
]
const ModConfigs = [
  {
    name: '霓虹深渊',
    gameDir: 'Neon Abyss',
    modTarget: 'NeonAbyss_Data\\Managed\\Assembly-CSharp.dll',
    //d:\Projs\WingUnityMods\NeonAbyss.WingMod.mm\bin\Debug\0Harmony.dll
    modProjectDir: 'NeonAbyss.WingMod.mm'
  },
  {
    name: 'Demo1',
    gameDir: 'UnityDemo1',
    modTarget: 'UnityDemo1_Data\\Managed\\UnityEngine.UI.dll',
    //d:\Projs\WingUnityMods\NeonAbyss.WingMod.mm\bin\Debug\0Harmony.dll
    modProjectDir: 'UnityDemo1.WingMod.mm'
  }
]

const ModLibs = [
  '0Harmony.dll', 'websocket-sharp.dll'
]

const rootDir = path.resolve(__dirname, '../')

let currentGameConfig

function buildGameConfig(cnf) {
  // 找到游戏具体目录
  let gameSteam = SteamDirs.map(sDir => {
    return path.resolve(sDir, cnf.gameDir)
  }).filter(v => fs.existsSync(v))
  if (_.size(gameSteam) == 0) {
    throw new Error('无法找到目录')
  }
  let full = _.merge({}, cnf)
  full.gameDir = gameSteam[0]
  full.targetDll = path.basename(cnf.modTarget)
  full.targetDir = path.resolve(full.gameDir, path.dirname(cnf.modTarget))
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
  logger.info('[MonoMod] 将复制到', cnf.gameDir)
  await cpy(['*'], cnf.gameDir, { cwd: monoDir });
  logger.info('[MonoMod] 复制完毕');
}

// 复制自己的mod
async function copyWingMod(cnf) {
  let debugDir = path.resolve(rootDir, cnf.modProjectDir, 'bin\\Debug')
  logger.info('[WingMod] Debug目录', debugDir)
  let modFiles = ModLibs.concat([cnf.targetDll.replace('.dll', '.mm.dll')])
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

async function patchMod(cnf) {
  try {
    let p = execa('MonoMod.exe', [cnf.modTarget], { encoding: 'binary', cwd: cnf.gameDir });
    p.stdout.pipe(process.stdout);
    p.stderr.pipe(process.stderr);
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

async function main() {
  let selectedMod = ModConfigs[0]
  logger.info('当前游戏', selectedMod.name)
  currentGameConfig = buildGameConfig(selectedMod)
  await copyMonoMod(currentGameConfig)
  await copyWingMod(currentGameConfig)
  await patchMod(currentGameConfig)
}

main().then(() => {
  process.exit(0)
})
