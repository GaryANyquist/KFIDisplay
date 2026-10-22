# Post-build step of KFIDisplaySetup (2026-10-22): the setup project picks up axvlc.dll (VLC's ActiveX control) as a
# dependency of the AXVLC COM reference and self-registers that copy from the program folder, where VLC's libvlc.dll
# isn't, so registration fails (HRESULT 0x8007007E) and the whole install rolls back (error 1904 / 1603).
# KFIDisplay uses the control VLC's own installer registers (C:\Program Files (x86)\VideoLAN\VLC\axvlc.dll), so this
# removes axvlc.dll from the MSI's SelfReg table. VLC media player (32-bit) must be installed on the PC.
param([Parameter(Mandatory = $true)][string]$Msi)

$ErrorActionPreference = 'Stop'
$installer = New-Object -ComObject WindowsInstaller.Installer
function Call($obj, $name, $kind, $argv) { $obj.GetType().InvokeMember($name, $kind, $null, $obj, $argv) }

$db = Call $installer 'OpenDatabase' 'InvokeMethod' @($Msi, 1)  # 1 = transact
$find = Call $db 'OpenView' 'InvokeMethod' @(
  "SELECT ``SelfReg``.``File_``, ``File``.``FileName`` FROM ``SelfReg``, ``File`` WHERE ``SelfReg``.``File_`` = ``File``.``File``")
Call $find 'Execute' 'InvokeMethod' $null | Out-Null
$keys = @()
while ($row = Call $find 'Fetch' 'InvokeMethod' $null) {
  if ((Call $row 'StringData' 'GetProperty' 2) -match 'axvlc\.dll') { $keys += (Call $row 'StringData' 'GetProperty' 1) }
}
Call $find 'Close' 'InvokeMethod' $null | Out-Null

foreach ($key in $keys) {
  $del = Call $db 'OpenView' 'InvokeMethod' @("DELETE FROM ``SelfReg`` WHERE ``File_`` = '$key'")
  Call $del 'Execute' 'InvokeMethod' $null | Out-Null
  Call $del 'Close' 'InvokeMethod' $null | Out-Null
}
Call $db 'Commit' 'InvokeMethod' $null | Out-Null
Write-Output "no-axvlc-selfreg: removed $($keys.Count) axvlc.dll self-registration(s) from $Msi"
