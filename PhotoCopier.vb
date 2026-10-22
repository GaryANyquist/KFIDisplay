Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions

''' <summary>
''' When KFIDisplay runs on its own PC (DisplaySettings.IsRemote), the item photos arrive from the register on the
''' SQL Server PC. This copies the ones this PC lacks into its own C:\images, from the sync service there
''' (GET /v1/photos, then GET /v1/photos/name, with the DISPLAY_KEY). Photos are only ever added: a changed photo has
''' a new name. Runs once a minute in the background; problems go to the log once, never to the screen.
''' </summary>
Public Class PhotoCopier
    Public Const ImageFolder As String = "C:\images"
    Private Shared WithEvents Tick As System.Timers.Timer
    Private Shared Busy As Boolean = False
    Private Shared LastProblem As String = ""

    ''' <summary>Starts copying (now, then every minute). Does nothing unless the database is on another PC.</summary>
    Public Shared Sub Start()
        If Not DisplaySettings.IsRemote OrElse Tick IsNot Nothing Then Exit Sub
        Tick = New System.Timers.Timer(60000)
        Tick.AutoReset = True
        Tick.Start()
        System.Threading.Tasks.Task.Run(Sub() CopyNow())
    End Sub

    Private Shared Sub Tick_Elapsed(sender As Object, e As System.Timers.ElapsedEventArgs) Handles Tick.Elapsed
        CopyNow()
    End Sub

    ''' <summary>Copies the photos this PC lacks. Returns how many were copied. Never throws.</summary>
    Public Shared Function CopyNow() As Integer
        If Busy Then Return 0
        Busy = True
        Dim Copied As Integer = 0
        Try
            Dim Base As String = DisplaySettings.SyncAddress
            If Base = "" OrElse DisplaySettings.DisplayKey = "" Then
                Note("Photos: SyncAddress and DisplayKey are not both set in " & DisplaySettings.FilePath)
                Return 0
            End If
            Directory.CreateDirectory(ImageFolder)
            Dim Have As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each F As String In Directory.GetFiles(ImageFolder)
                Have.Add(Path.GetFileName(F))
            Next
            Using Web As New WebClient()
                Web.Headers(HttpRequestHeader.Authorization) = "Bearer " & DisplaySettings.DisplayKey
                Dim Json As String = Web.DownloadString(Base & "/v1/photos")
                'The list is {"photos":["a.jpg",...]} of plain file names (the service only lists safe ones).
                For Each M As Match In Regex.Matches(Json, """([A-Za-z0-9._-]+\.(?:jpe?g|png|gif))""", RegexOptions.IgnoreCase)
                    Dim Name As String = M.Groups(1).Value
                    If Have.Contains(Name) Then Continue For
                    Web.Headers(HttpRequestHeader.Authorization) = "Bearer " & DisplaySettings.DisplayKey
                    Dim Bytes As Byte() = Web.DownloadData(Base & "/v1/photos/" & Uri.EscapeDataString(Name))
                    'Written beside it first, so the menu board never opens half a photo.
                    Dim Temp As String = Path.Combine(ImageFolder, "." & Name & ".tmp")
                    File.WriteAllBytes(Temp, Bytes)
                    File.Move(Temp, Path.Combine(ImageFolder, Name))
                    Have.Add(Name)
                    Copied += 1
                Next
            End Using
            If Copied > 0 Then Note("Photos: copied " & Copied & " from " & Base, True)
            LastProblem = ""
        Catch ex As Exception
            Note("Photos: could not copy from " & DisplaySettings.SyncAddress & " (" & ex.Message & ")")
        Finally
            Busy = False
        End Try
        Return Copied
    End Function

    ''' <summary>Writes a line to the log; the same problem only once until it clears.</summary>
    Private Shared Sub Note(ByVal Text As String, Optional ByVal Always As Boolean = False)
        If Not Always Then
            If Text = LastProblem Then Exit Sub
            LastProblem = Text
        End If
        Try
            My.Computer.FileSystem.WriteAllText("c:\KFDisplay\log.txt", Now.ToString & Chr(9) & Text & vbCrLf, True)
        Catch
        End Try
    End Sub
End Class
