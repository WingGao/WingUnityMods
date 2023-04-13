const fs = require('fs')
const path = require('path')
const cp = require('child_process');

function main() {
    fs.copyFileSync(path.join(__dirname, 'Patch.cs'), 'd:\\Program Files (x86)\\Steam\\steamapps\\common\\JiangCity\\WingModSourcePatcher.cs')
    cp.execFileSync("d:\\Program Files (x86)\\Steam\\steamapps\\common\\JiangCity\\jcGame.exe",null,
        {cwd:'d:\\Program Files (x86)\\Steam\\steamapps\\common\\JiangCity'})
}

main()