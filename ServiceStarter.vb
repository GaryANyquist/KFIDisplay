Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation

''' <summary>
''' Makes sure the Node sync service that feeds the KFDisplay database is running (it receives the registers' sales,
''' menu and photos). It normally starts from its Windows scheduled task at boot; this starts it if it is not running
''' when KFIDisplay starts, using the same start script (which logs to the service's own logs folder). It counts as
''' running when something is listening on its port, so one started by the scheduled task is never started twice.
''' (The old Kitchen Display server is retired, 2026-10-22.)
''' </summary>
Public Class ServiceStarter
    ' Where the services live on this PC. Change here if they move.
    Public Const SyncScript As String = "C:\Source\kfdisplay-sync\start-sync.cmd"
    Public Const SyncPort As Integer = 8787

    ''' <summary>True when something on this PC is listening on the TCP port.</summary>
    Public Shared Function IsListening(ByVal Port As Integer) As Boolean
        For Each L In IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            If L.Port = Port Then Return True
        Next
        Return False
    End Function

    ''' <summary>
    ''' Starts the script (hidden, as the logged-in user) unless its port is already listening.
    ''' Returns what happened, for the log: "name: already running", "name: started", "name: script not found (path)"
    ''' or "name: could not start (reason)". Never throws.
    ''' </summary>
    Public Shared Function EnsureRunning(ByVal Name As String, ByVal Script As String, ByVal Port As Integer) As String
        Try
            If IsListening(Port) Then Return Name & ": already running"
            If Not File.Exists(Script) Then Return Name & ": script not found (" & Script & ")"
            Dim Installed As String = InstallPackagesIfMissing(Path.GetDirectoryName(Script))
            If Installed <> "" Then Return Name & ": " & Installed
            Dim Info As New ProcessStartInfo("cmd.exe", "/c """ & Script & """")
            Info.WorkingDirectory = Path.GetDirectoryName(Script)
            Info.UseShellExecute = False
            Info.CreateNoWindow = True
            Info.WindowStyle = ProcessWindowStyle.Hidden
            Process.Start(Info)
            Return Name & ": started"
        Catch ex As Exception
            Return Name & ": could not start (" & ex.Message & ")"
        End Try
    End Function

    ''' <summary>
    ''' A Node service folder with no node_modules (a fresh copy from GitHub) can't run: runs "npm install" there first
    ''' (hidden, up to 5 minutes; needs internet and Node on the PATH). Returns "" when the packages are there or were
    ''' installed, otherwise the reason the service was not started.
    ''' </summary>
    Private Shared Function InstallPackagesIfMissing(ByVal ServiceFolder As String) As String
        If Directory.Exists(Path.Combine(ServiceFolder, "node_modules")) Then Return ""
        If Not File.Exists(Path.Combine(ServiceFolder, "package.json")) Then Return ""
        Try
            Dim Info As New ProcessStartInfo("cmd.exe", "/c npm install --omit=dev --no-audit --no-fund")
            Info.WorkingDirectory = ServiceFolder
            Info.UseShellExecute = False
            Info.CreateNoWindow = True
            Using P As Process = Process.Start(Info)
                If Not P.WaitForExit(300000) Then Return "npm install is still running after 5 minutes, not started"
                If P.ExitCode <> 0 Then Return "npm install failed (exit code " & P.ExitCode & "), not started"
            End Using
            Return ""
        Catch ex As Exception
            Return "npm install could not run (" & ex.Message & "), not started"
        End Try
    End Function

    ''' <summary>Checks the sync service (the old Kitchen Display server is retired, 2026-10-22); one log line.</summary>
    Public Shared Function EnsureAllRunning() As List(Of String)
        Dim L As New List(Of String)
        L.Add(EnsureRunning("KFDisplay sync service", SyncScript, SyncPort))
        Return L
    End Function
End Class
