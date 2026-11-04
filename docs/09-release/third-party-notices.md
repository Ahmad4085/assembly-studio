# Third-Party Notices: Obligations Check (release 1.0.0)

| Field | Value |
|---|---|
| Document | Release licensing checklist (`/assembly-release` step 5) |
| Version | 1.0 |
| Last updated | 2026-11-04 |
| Inventory | `docs/09-release/license-inventory.md` and `docs/09-release/sbom.spdx.json` (generated from the release publish folder by `build/installer/license-inventory.ps1`); the installed notices are `THIRD-PARTY-NOTICES.txt` |
| Decisions | D-007 (permissive open source), D-025 (MIT), D-026 (installer license page) |

Every shipped file belongs to a component below. The generator stops on an unknown file, so no component is missing. Unknown
licenses: **none**.

| Component | License | Obligation | How it is met | Met |
|---|---|---|---|---|
| Assembly Studio | MIT | Keep the copyright and permission notice | `LICENSE` installed; first section of `THIRD-PARTY-NOTICES.txt`; first text of the license page; `AppCopyright` in the installer | Yes |
| .NET runtime (most files) | MIT | Keep the notice | Its `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT` in `THIRD-PARTY-NOTICES.txt` | Yes |
| .NET runtime: coreclr.dll, Microsoft.DiaSymReader.Native.amd64.dll | .NET Library License | Distribute only inside the application; end users agree to protective terms; indemnify Microsoft; no Excluded License | Shipped inside the application only; the installer's license page must be accepted (D-026); MIT is not an Excluded License | Yes, except the **indemnity**, which the distributor (the user) carries and which no file can meet |
| WPF (most files, resource folders) | MIT | Keep the notice | Its `LICENSE` in `THIRD-PARTY-NOTICES.txt` | Yes |
| WPF: PresentationNative_cor3.dll, vcruntime140_cor3.dll, wpfgfx_cor3.dll | .NET Library License | as above | as above | as above |
| WPF: D3DCompiler_47_cor3.dll | Windows SDK License | Add significant functionality; end users agree to protective terms; show a valid copyright notice; indemnify Microsoft | The IDE is the primary functionality; license page (D-026); copyright in About (notices), `LICENSE`, the installer | as above |
| AvalonEdit 6.3.1.120 | MIT | Keep the notice | MIT text with the package's copyright line in `THIRD-PARTY-NOTICES.txt` | Yes |
| CommunityToolkit.Mvvm 8.4.2 | MIT (+ its third-party notices) | Keep the notices | Its `License.md` and `ThirdPartyNotices.txt` in `THIRD-PARTY-NOTICES.txt` | Yes |
| Iced 1.21.0 | MIT | Keep the notice | Its `LICENSE.txt` in `THIRD-PARTY-NOTICES.txt` | Yes |
| NASM 3.02 | BSD-2-Clause | Binary redistribution reproduces the notice, conditions and disclaimer | `tools\nasm\LICENSE` installed; Help → About shows it after the notices | Yes |
| Inno Setup 7.1.0 (in the installer program only) | Inno Setup License | Keep its notices with binary redistributions | Its license text in `THIRD-PARTY-NOTICES.txt` | Yes |

**No source offers are required:** none of the licenses is copyleft.

**Fonts, icons and sample code:**
- **Fonts:** none ship. The app uses Windows' own fonts: Cascadia Mono or Consolas, and Segoe Fluent Icons or Segoe MDL2 Assets
  for the glyphs.
- **Samples, tutorials and help:** written for this project, under its MIT License.

**Evidence:**
- TC-096 and TC-097 (test report 2026-11-04-1) check the license, the notices, the license page, the inventory, the pins, the
  NASM checksums and the SBOM.
- TC-047 checks the notices as Help → About shows them.
