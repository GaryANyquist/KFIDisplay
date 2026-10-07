Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation

''' <summary>
''' Makes sure the two Node services that feed and read the KFDisplay database are running: the sync service
''' (receives the register tablet's sales, menu and photos) and the Kitchen Display server. They normally start
''' from Windows scheduled tasks at boot; this starts any that is not running when KFIDisplay starts, using the
''' same start scripts (which log to the service's own logs folder). A service counts as running when something is
''' listening on its port, so one started by a scheduled task is never started twice.
''' </summary>
Public Class ServiceStarter
    ' Where the services live on this PC. Change here if they move.
    Public Const SyncScript As String = "C:\Source\kfdisplay-sync\start-sync.cmd"
    Public Const SyncPort As Integer = 8787
    Public Const KitchenScript As String = "C:\Source\KitchenDisplay\start-kitchen.cmd"
    Public Const KitchenPort As Integer = 8790

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

    ''' <summary>Checks both services; one log line each.</summary>
    Public Shared Function EnsureAllRunning() As List(Of String)
        Dim L As New List(Of String)
        L.Add(EnsureRunning("KFDisplay sync service", SyncScript, SyncPort))
        L.Add(EnsureRunning("Kitchen Display service", KitchenScript, KitchenPort))
        Return L
    End Function
End Class
