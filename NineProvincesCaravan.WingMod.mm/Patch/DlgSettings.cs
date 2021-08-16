using Framework;
using MonoMod;

public class patch_DlgSettings : DlgSettings
{
    public patch_DlgSettings(UIDlg _dlg, DlgMgr.EnumDlg _dlgtype) : base(_dlg, _dlgtype)
    {
    }

    [MonoModIgnore]
    [PatchDlgSettingsAttribute]
    public new extern void UpdateFps();
}