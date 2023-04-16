const fs = require('fs')
const path = require('path')
const cp = require('child_process');

function main() {
    // const steamDir = "d:\\Program Files (x86)\\Steam\\steamapps\\common\\"
    const steamDir = "e:\\Program Files (x86)\\Steam\\steamapps\\common\\"
    fs.copyFileSync(path.join(__dirname, 'WingModSourcePatcher.cs'), steamDir + 'JiangCity\\WingMod\\WingModSourcePatcher.cs')
    fs.copyFileSync(path.join(__dirname, 'Export.cs'), steamDir + 'JiangCity\\WingMod\\Export.cs')
    cp.execFileSync(steamDir + "JiangCity\\jcGame.exe", null,
        {cwd: steamDir + 'JiangCity'})
}

main()