# Application and folder icon

PrivacySwitch.ico contains transparent 32-bit images at 16, 24, 32, 48, 64, 128 and 256 pixels. PrivacySwitch-icon.png is a 512-pixel preview/export. PrivacySwitch-icon.svg is the editable vector design. The orange shield and switch match the app palette. Build.ps1 embeds the ICO in PrivacySwitch.exe; the main window uses the executable's embedded icon.

To use it on a Windows folder: right-click the folder, choose Properties > Customize > Change Icon > Browse, select PrivacySwitch.ico, then confirm and Apply. Keep the ICO in a permanent location: moving or deleting it can break the folder's icon reference. For a shortcut, use Properties > Shortcut > Change Icon. The executable itself already contains the icon. Windows may temporarily cache an older executable icon; refresh Explorer or use a newly extracted folder if it appears unchanged.

The design is included as project source; it uses no external brand artwork, font, or image dependency. Runtime behavior is unchanged by the icon addition.

