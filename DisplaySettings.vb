Imports System.Data.SqlClient
Imports System.IO

''' <summary>
''' Where the KFDisplay database is (Gary, 2026-10-22: KFIDisplay can run on its own PC, with SQL Server and the
''' sync service on another). Read from C:\KFDisplay\display.ini, a plain key=value file:
'''
'''   SqlServer=SQLPC,1433          the SQL Server PC (name or address) and port
'''   SqlUser=kfidisplay_read          the read-only login from kfdisplay-sync setup\08-display-login.sql
'''   SqlPassword=...
'''   SyncAddress=http://SQLPC:8787  the sync service on that PC, which photos are copied from
'''   DisplayKey=...                   DISPLAY_KEY from that sync service's .env
'''   Menu=wxm_...                     the Wix menu the board shows (2.0.18; empty = all menus), set in the menu window
'''   AnnawareKey=crk_...              2.0.25: read the menu straight from annaware.com with this business key (then no
'''                                    SQL Server, sync service or photo copying is used; see AnnawareMenu.vb)
'''
''' Without the file (or with SqlServer empty) everything is on this PC, as before: the local SQL Server with
''' Windows sign-in, and this app sets up the database and starts the services.
''' </summary>
Public Class DisplaySettings
    Public Const FilePath As String = "C:\KFDisplay\display.ini"
    Public Const LocalConnection As String = "Data Source=.\SQLEXPRESS;Initial Catalog=KFDisplay;Integrated Security=True"

    Private Shared Values As Dictionary(Of String, String) = Load()

    Private Shared Function Load() As Dictionary(Of String, String)
        Dim D As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Try
            If File.Exists(FilePath) Then
                For Each Line As String In File.ReadAllLines(FilePath)
                    Dim T As String = Line.Trim()
                    If T = "" OrElse T.StartsWith("#") OrElse T.StartsWith(";") Then Continue For
                    Dim Eq As Integer = T.IndexOf("="c)
                    If Eq > 0 Then D(T.Substring(0, Eq).Trim()) = T.Substring(Eq + 1).Trim()
                Next
            End If
        Catch
            'An unreadable file counts as no file: this PC's own SQL Server.
        End Try
        Return D
    End Function

    Private Shared Function Value(ByVal Name As String) As String
        Dim V As String = Nothing
        Return If(Values.TryGetValue(Name, V), V, "")
    End Function

    ''' <summary>True when the database is on another PC (display.ini names a SqlServer).</summary>
    Public Shared ReadOnly Property IsRemote As Boolean
        Get
            Return Value("SqlServer") <> ""
        End Get
    End Property

    ''' <summary>The connection string: the other PC with the read-only SQL login, or this PC's SQL Server.</summary>
    Public Shared Function ConnectionString() As String
        If Not IsRemote Then Return LocalConnection
        Dim B As New SqlConnectionStringBuilder()
        B.DataSource = Value("SqlServer")
        B.InitialCatalog = "KFDisplay"
        B.UserID = Value("SqlUser")
        B.Password = Value("SqlPassword")
        B.ConnectTimeout = 15
        Return B.ConnectionString
    End Function

    ''' <summary>The sync service to copy photos from ("http://SQLPC:8787"), or "" when not set.</summary>
    Public Shared ReadOnly Property SyncAddress As String
        Get
            Dim A As String = Value("SyncAddress").TrimEnd("/"c)
            If A <> "" AndAlso Not A.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then A = "http://" & A
            If A <> "" AndAlso A.IndexOf(":"c, A.IndexOf("//", StringComparison.Ordinal) + 2) < 0 Then A &= ":8787"
            Return A
        End Get
    End Property

    ''' <summary>The annaware.com business key (2.0.25), or "" to read the KFDisplay SQL tables as before.</summary>
    Public Shared ReadOnly Property AnnawareKey As String
        Get
            Return Value("AnnawareKey").Trim()
        End Get
    End Property

    Public Shared ReadOnly Property DisplayKey As String
        Get
            Return Value("DisplayKey")
        End Get
    End Property

    ''' <summary>The Wix menu the board shows (a menus.id), or "" for all menus (register 2.9.11, Gary 2026-10-26).</summary>
    Public Shared ReadOnly Property MenuId As String
        Get
            Return Value("Menu")
        End Get
    End Property

    ''' <summary>Saves the board's menu in display.ini, keeping every other line as it was.</summary>
    Public Shared Sub SaveMenuId(ByVal Id As String)
        Dim Lines As New List(Of String)
        If File.Exists(FilePath) Then Lines.AddRange(File.ReadAllLines(FilePath))
        Dim Found As Boolean = False
        For k As Integer = 0 To Lines.Count - 1
            Dim T As String = Lines(k).Trim()
            If T.StartsWith("Menu=", StringComparison.OrdinalIgnoreCase) OrElse T.StartsWith("Menu =", StringComparison.OrdinalIgnoreCase) Then
                Lines(k) = "Menu=" & Id
                Found = True
            End If
        Next
        If Not Found Then Lines.Add("Menu=" & Id)
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath))
        File.WriteAllLines(FilePath, Lines)
        Values = Load()
    End Sub
End Class
