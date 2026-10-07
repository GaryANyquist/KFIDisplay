Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO

''' <summary>
''' The one-time Windows setup the KFDisplay PC needs so everything keeps working, done from KFIDisplay when it
''' starts: inbound firewall rules for the two services (the tablets reach them over Wi-Fi) and Windows scheduled
''' tasks that start them at boot. That needs administrator rights, so KFIDisplay checks first (firewall rules can be
''' read by anyone; a marker file records that the tasks were set up) and only when something is missing asks once
''' (Windows then shows its usual permission prompt) and runs the PowerShell script below. The script only adds what
''' is missing, so running it again changes nothing.
''' </summary>
Public Class StartupSetup
    ''' <summary>Raise this when the script changes, so every PC runs it again.</summary>
    Public Const SetupVersion As String = "1"
    Public Const Folder As String = "C:\KFDisplay"
    Public Shared ReadOnly FirewallRules As String() = {"KFDisplay sync", "Kitchen Display"}

    Public Shared ReadOnly Property MarkerFile As String
        Get
            Return Path.Combine(Folder, "setup-services.done")
        End Get
    End Property

    ''' <summary>The PowerShell script. Single quotes only, so it can live in a VB string. -DryRun reports without changing anything.</summary>
    Public Shared Function Script() As String
        Return "
param([switch]$DryRun)
$ErrorActionPreference = 'Stop'
$folder = '" & Folder & "'
$logFile = Join-Path $folder 'setup-services.log'
New-Item -ItemType Directory -Force -Path $folder | Out-Null
function Log([string]$m) { $line = '{0} {1}' -f (Get-Date -Format 's'), $m; Add-Content -Path $logFile -Value $line; Write-Host $line }
$ok = $true

# The tablets reach the sync service (8787) and the Kitchen Display (8790) over the truck's Wi-Fi.
$rules = @(
    @{ Name = 'KFDisplay sync'; Port = 8787 },
    @{ Name = 'Kitchen Display'; Port = 8790 }
)
foreach ($r in $rules) {
    try {
        if (Get-NetFirewallRule -DisplayName $r.Name -ErrorAction SilentlyContinue) { Log ('firewall rule already there: ' + $r.Name); continue }
        if ($DryRun) { Log ('would add firewall rule: ' + $r.Name + ' (TCP ' + $r.Port + ')'); continue }
        New-NetFirewallRule -DisplayName $r.Name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $r.Port -Profile Domain,Private | Out-Null
        Log ('firewall rule added: ' + $r.Name + ' (TCP ' + $r.Port + ')')
    } catch { Log ('FAILED firewall rule ' + $r.Name + ': ' + $_.Exception.Message); $ok = $false }
}

# Start both services when Windows boots, whether or not anyone logs in. No time limit (the default stops a task after 3 days).
$tasks = @(
    @{ Name = 'KFDisplay sync'; Script = 'C:\Source\kfdisplay-sync\start-sync.cmd' },
    @{ Name = 'Kitchen Display'; Script = 'C:\Source\KitchenDisplay\start-kitchen.cmd' }
)
foreach ($t in $tasks) {
    try {
        if (-not (Test-Path $t.Script)) { Log ('skipped task ' + $t.Name + ': ' + $t.Script + ' not found'); continue }
        $existing = Get-ScheduledTask -TaskName $t.Name -ErrorAction SilentlyContinue
        if ($existing) {
            if ($existing.Settings.ExecutionTimeLimit -ne 'PT0S') {
                if ($DryRun) { Log ('would remove the time limit from task ' + $t.Name); continue }
                $existing.Settings.ExecutionTimeLimit = 'PT0S'
                Set-ScheduledTask -InputObject $existing | Out-Null
                Log ('task ' + $t.Name + ': removed the 72 hour time limit')
            } else { Log ('task already there: ' + $t.Name) }
            continue
        }
        if ($DryRun) { Log ('would add scheduled task: ' + $t.Name); continue }
        $action = New-ScheduledTaskAction -Execute $t.Script
        $trigger = New-ScheduledTaskTrigger -AtStartup
        $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
        $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
        Register-ScheduledTask -TaskName $t.Name -Action $action -Trigger $trigger -Principal $principal -Settings $settings | Out-Null
        Log ('scheduled task added: ' + $t.Name)
    } catch { Log ('FAILED task ' + $t.Name + ': ' + $_.Exception.Message); $ok = $false }
}

if ($ok -and -not $DryRun) { Set-Content -Path (Join-Path $folder 'setup-services.done') -Value '" & SetupVersion & "' -Encoding ascii; Log 'setup finished' }
elseif (-not $ok) { Log 'setup finished with problems; it will be tried again next time KFIDisplay starts' }
if (-not $ok) { exit 1 }
"
    End Function

    ''' <summary>True when the named Windows Firewall rule exists (readable without administrator rights).</summary>
    Public Shared Function FirewallRuleExists(ByVal Name As String) As Boolean
        Try
            Dim Info As New ProcessStartInfo("netsh.exe", "advfirewall firewall show rule name=""" & Name & """")
            Info.UseShellExecute = False
            Info.CreateNoWindow = True
            Info.RedirectStandardOutput = True
            Using P As Process = Process.Start(Info)
                Dim Output As String = P.StandardOutput.ReadToEnd()
                P.WaitForExit(15000)
                Return Output.IndexOf("No rules match", StringComparison.OrdinalIgnoreCase) < 0 AndAlso Output.IndexOf("Rule Name", StringComparison.OrdinalIgnoreCase) >= 0
            End Using
        Catch ex As Exception
            Return True 'can't tell: don't nag
        End Try
    End Function

    ''' <summary>What is missing, as a short sentence, or "" when everything is set up.</summary>
    Public Shared Function WhatIsMissing() As String
        Dim Missing As New List(Of String)
        For Each R As String In FirewallRules
            If Not FirewallRuleExists(R) Then Missing.Add("the firewall rule '" & R & "'")
        Next
        Dim Done As Boolean = False
        Try
            Done = File.Exists(MarkerFile) AndAlso File.ReadAllText(MarkerFile).Trim() = SetupVersion
        Catch
        End Try
        If Not Done Then Missing.Add("the start-at-boot tasks for the two services")
        Return String.Join(" and ", Missing)
    End Function

    ''' <summary>
    ''' If something is missing, asks once and runs the script with administrator rights (Windows shows its permission prompt).
    ''' Returns a line for the log, or "" when nothing needed doing. Call from a background thread: it waits for the user.
    ''' Never throws.
    ''' </summary>
    Public Shared Function RunIfNeeded() As String
        Try
            Dim Missing As String = WhatIsMissing()
            If Missing = "" Then Return ""
            If MessageBox.Show("KFIDisplay needs to set up Windows on this PC so the sync service and the Kitchen Display keep working:" & vbCrLf & vbCrLf &
                               Missing & "." & vbCrLf & vbCrLf &
                               "Windows will ask for administrator permission next. Continue?", "KFIDisplay: Windows setup",
                               MessageBoxButtons.OKCancel, MessageBoxIcon.Question) <> DialogResult.OK Then
                Return "Windows setup: skipped by the user (needed: " & Missing & ")"
            End If
            Directory.CreateDirectory(Folder)
            Dim ScriptFile As String = Path.Combine(Folder, "setup-services.ps1")
            File.WriteAllText(ScriptFile, Script(), New System.Text.UTF8Encoding(True))
            Dim Info As New ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File """ & ScriptFile & """")
            Info.Verb = "runas"
            Info.UseShellExecute = True
            Info.WindowStyle = ProcessWindowStyle.Hidden
            Using P As Process = Process.Start(Info)
                If Not P.WaitForExit(180000) Then Return "Windows setup: still running after 3 minutes (see " & Path.Combine(Folder, "setup-services.log") & ")"
                Return If(P.ExitCode = 0, "Windows setup: done (" & Missing & ")", "Windows setup: finished with problems, see " & Path.Combine(Folder, "setup-services.log"))
            End Using
        Catch ex As Win32Exception When ex.NativeErrorCode = 1223
            Return "Windows setup: administrator permission was declined"
        Catch ex As Exception
            Return "Windows setup: couldn't run (" & ex.Message & ")"
        End Try
    End Function
End Class
