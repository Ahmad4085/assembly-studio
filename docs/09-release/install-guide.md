# Assembly Studio: Install Guide

| Field | Value |
|---|---|
| Applies to | Assembly Studio 1.0.0 (`AssemblyStudio-1.0.0-setup.exe`) |
| Last updated | 2026-11-04 |

## 1. Before you install
- **Windows:** 64-bit Windows 11, or Windows 10 version 21H2 (build 19044) or later. Release 1.0.0 was tested on Windows 11 only.
- **Disk:** about 190 MB free.
- **Nothing else is needed.** The .NET runtime and NASM are included, nothing is downloaded, and no administrator rights are
  required.

## 2. Install
1. Run `AssemblyStudio-1.0.0-setup.exe`.
2. **If Windows SmartScreen says "Windows protected your PC":** the installer is not code-signed. Choose **More info**, check
   that the publisher field reads "Unknown publisher" and the file name is right, then choose **Run anyway**.
   - To check the download first, compare its SHA-256 hash with the one published with the release. In PowerShell, run
     `Get-FileHash .\AssemblyStudio-1.0.0-setup.exe`.
3. **Read and accept the license page.** It shows Assembly Studio's MIT License, followed by the Microsoft terms for six bundled
   .NET files.
4. **Choose the options:**
   - **Create a desktop shortcut:** off by default.
   - **Open .asm files with Assembly Studio:** off by default.
5. Choose **Install**. It takes under a minute (about 20 seconds was measured). Assembly Studio is installed for your Windows
   account in `%LOCALAPPDATA%\Programs\Assembly Studio` and added to the Start menu.

## 3. First run
Start **Assembly Studio** from the Start menu. To check the installation:
1. Open the **Learn** sidebar (View → Learn Sidebar, or Ctrl+Alt+L).
2. Open the sample "Hello, World".
3. Press **F5**. The Console panel shows the program's output.

Nothing needs to be configured, and no network connection is used.

## 4. Upgrade
Run the newer installer; it replaces the older version. Your settings (`%APPDATA%\AssemblyStudio\settings.json`) and your own
source files are kept.

## 5. Uninstall
Use **Settings → Apps → Installed apps → Assembly Studio → Uninstall** (or Control Panel → Programs). This removes the program,
the shortcuts and the optional `.asm` association. These are kept:
- your source files;
- your settings (`%APPDATA%\AssemblyStudio`);
- your crash-recovery files and logs (`%LOCALAPPDATA%\AssemblyStudio`).

Delete those two folders yourself if you want them gone.

## 6. Silent install (for labs)
`AssemblyStudio-1.0.0-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART` installs with the defaults. A silent install skips the
license page, so whoever deploys it accepts the license terms on behalf of the users. To associate `.asm` files, add
`/MERGETASKS="associateasm"`.

## 7. Troubleshooting
- **Help → About → Diagnostics** shows the application, assembler and .NET versions and the log folder. It can copy them for a
  bug report.
- If the bundled assembler is missing or damaged, Settings names the problem. Reinstall, or set the path of another `nasm.exe`,
  version 3.02 or later.
