Imports System.Data.SqlClient
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>What EnsureReady found and did. Nothing in it is ever thrown: the app keeps running whatever happens.</summary>
Public Class SetupResult
    ''' <summary>False when SQL Server could not be reached at all (it may still be starting): try again later.</summary>
    Public Connected As Boolean = False
    Public DatabaseCreated As Boolean = False
    Public TablesCreated As New List(Of String)
    ''' <summary>The SQL logins made just now, and the file holding each one's new password (same order).</summary>
    Public NewLogins As New List(Of String)
    Public PasswordFiles As New List(Of String)
    Public Errors As New List(Of String)

    Public ReadOnly Property LoginCreated As Boolean
        Get
            Return NewLogins.Count > 0
        End Get
    End Property

    Public ReadOnly Property Changed As Boolean
        Get
            Return DatabaseCreated OrElse TablesCreated.Count > 0 OrElse LoginCreated
        End Get
    End Property

    Public Function Summary() As String
        Dim S As New StringBuilder
        If DatabaseCreated Then S.AppendLine("The database did not exist, so it was created.")
        If TablesCreated.Count > 0 Then S.AppendLine("Tables created: " & String.Join(", ", TablesCreated) & ".")
        For I As Integer = 0 To NewLogins.Count - 1
            S.AppendLine("SQL login '" & NewLogins(I) & "' was created. Its password is in " & PasswordFiles(I) & "; follow the instructions in that file, then delete it.")
        Next
        For Each E As String In Errors
            S.AppendLine("Problem: " & E)
        Next
        Return S.ToString().TrimEnd()
    End Function
End Class

''' <summary>
''' Makes sure the KFDisplay database is there when KFIDisplay starts. If the database is missing it is created
''' with the register tablet's tables and a SQL login for the sync service (kfdisplay-sync) that can read and
''' write only those tables. If the database exists, any missing table is added. Existing tables, rows, logins
''' and passwords are never changed or dropped. The tables are the ones in kfdisplay-sync\setup\02-replace-schema.sql
''' (money is integer cents, times are UTC); the tablet fills them through the sync service.
''' </summary>
Public Class DatabaseSetup
    Public Const DefaultLogin As String = "register_sync"

    ' (table name, its CREATE statements), in an order where referenced tables come first.
    Private Shared Function TableScripts() As List(Of KeyValuePair(Of String, String))
        Dim L As New List(Of KeyValuePair(Of String, String))
        L.Add(New KeyValuePair(Of String, String)("categories", "
CREATE TABLE dbo.categories (
    id          nvarchar(64)  NOT NULL PRIMARY KEY,
    name        nvarchar(200) NOT NULL,
    color       nvarchar(20)  NOT NULL,
    sort_order  int           NOT NULL DEFAULT 0,
    synced_at   datetime2(3)  NOT NULL DEFAULT SYSUTCDATETIME()
);"))
        L.Add(New KeyValuePair(Of String, String)("items", "
CREATE TABLE dbo.items (
    id                  nvarchar(64)  NOT NULL PRIMARY KEY,
    name                nvarchar(200) NOT NULL,
    price               int           NOT NULL,
    category_id         nvarchar(64)  NOT NULL,
    color               nvarchar(20)  NOT NULL,
    taxable             int           NOT NULL DEFAULT 1,
    archived            int           NOT NULL DEFAULT 0,
    sort_order          int           NOT NULL DEFAULT 0,
    source_key          nvarchar(200) NULL,
    image               nvarchar(1000) NULL,
    show_on_menu_board  int           NOT NULL DEFAULT 0,
    out_of_stock        int           NOT NULL DEFAULT 0,
    description         nvarchar(1000) NULL,
    synced_at           datetime2(3)  NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX idx_items_category ON dbo.items(category_id);"))
        L.Add(New KeyValuePair(Of String, String)("modifier_groups", "
CREATE TABLE dbo.modifier_groups (
    id            nvarchar(64)  NOT NULL PRIMARY KEY,
    name          nvarchar(200) NOT NULL,
    required      int           NOT NULL DEFAULT 0,
    multi_select  int           NOT NULL DEFAULT 1,
    sort_order    int           NOT NULL DEFAULT 0,
    synced_at     datetime2(3)  NOT NULL DEFAULT SYSUTCDATETIME()
);"))
        L.Add(New KeyValuePair(Of String, String)("modifier_options", "
CREATE TABLE dbo.modifier_options (
    id           nvarchar(64)  NOT NULL PRIMARY KEY,
    group_id     nvarchar(64)  NOT NULL,
    name         nvarchar(200) NOT NULL,
    price_delta  int           NOT NULL DEFAULT 0,
    sort_order   int           NOT NULL DEFAULT 0,
    default_on   int           NOT NULL DEFAULT 0
);
CREATE INDEX idx_options_group ON dbo.modifier_options(group_id);"))
        L.Add(New KeyValuePair(Of String, String)("item_modifier_groups", "
CREATE TABLE dbo.item_modifier_groups (
    item_id     nvarchar(64) NOT NULL,
    group_id    nvarchar(64) NOT NULL,
    sort_order  int          NOT NULL DEFAULT 0,
    PRIMARY KEY (item_id, group_id)
);"))
        L.Add(New KeyValuePair(Of String, String)("discounts", "
CREATE TABLE dbo.discounts (
    id         nvarchar(64)  NOT NULL PRIMARY KEY,
    name       nvarchar(200) NOT NULL,
    type       nvarchar(10)  NOT NULL CHECK (type IN (N'percent', N'amount')),
    value      float         NOT NULL DEFAULT 0,
    starts_on  nvarchar(10)  NULL,
    ends_on    nvarchar(10)  NULL,
    synced_at  datetime2(3)  NOT NULL DEFAULT SYSUTCDATETIME()
);"))
        L.Add(New KeyValuePair(Of String, String)("auto_discounts", "
CREATE TABLE dbo.auto_discounts (
    id          nvarchar(64)  NOT NULL PRIMARY KEY,
    name        nvarchar(200) NOT NULL,
    type        nvarchar(10)  NOT NULL CHECK (type IN (N'percent', N'amount')),
    value       float         NOT NULL DEFAULT 0,
    active      int           NOT NULL DEFAULT 1,
    sort_order  int           NOT NULL DEFAULT 0,
    starts_on   nvarchar(10)  NULL,
    ends_on     nvarchar(10)  NULL,
    sale_price  int           NULL,
    synced_at   datetime2(3)  NOT NULL DEFAULT SYSUTCDATETIME()
);"))
        L.Add(New KeyValuePair(Of String, String)("auto_discount_targets", "
CREATE TABLE dbo.auto_discount_targets (
    discount_id  nvarchar(64) NOT NULL,
    target_type  nvarchar(10) NOT NULL CHECK (target_type IN (N'item', N'category')),
    target_id    nvarchar(64) NOT NULL,
    PRIMARY KEY (discount_id, target_type, target_id)
);"))
        L.Add(New KeyValuePair(Of String, String)("shifts", "
CREATE TABLE dbo.shifts (
    id             nvarchar(64)   NOT NULL PRIMARY KEY,
    opened_at      datetime2(3)   NOT NULL,
    starting_cash  int            NOT NULL DEFAULT 0,
    closed_at      datetime2(3)   NULL,
    counted_cash   int            NULL,
    over_short     int            NULL,
    note           nvarchar(1000) NULL,
    prepaid        int            NOT NULL DEFAULT 0,
    synced_at      datetime2(3)   NOT NULL DEFAULT SYSUTCDATETIME()
);"))
        L.Add(New KeyValuePair(Of String, String)("orders", "
CREATE TABLE dbo.orders (
    id               nvarchar(64)   NOT NULL PRIMARY KEY,
    number           int            NOT NULL,
    created_at       datetime2(3)   NOT NULL,
    shift_id         nvarchar(64)   NOT NULL,
    customer_name    nvarchar(200)  NULL,
    discount_id      nvarchar(64)   NULL,
    discount_name    nvarchar(200)  NULL,
    discount_type    nvarchar(10)   NULL,
    discount_value   float          NULL,
    subtotal         int            NOT NULL,
    discount_amount  int            NOT NULL,
    tax              int            NOT NULL,
    tip              int            NOT NULL,
    total            int            NOT NULL,
    payment_type     nvarchar(10)   NOT NULL,
    payment_amount   int            NOT NULL,
    tendered         int            NULL,
    change_due       int            NULL,
    card_brand       nvarchar(40)   NULL,
    last4            nvarchar(8)    NULL,
    payment_note     nvarchar(1000) NULL,
    status           nvarchar(20)   NOT NULL DEFAULT N'completed',
    refunded_at      datetime2(3)   NULL,
    refund_reason    nvarchar(1000) NULL,
    pager_number     nvarchar(20)   NULL,
    tender           nvarchar(20)   NULL,
    processor        nvarchar(20)   NULL,
    processor_ref    nvarchar(200)  NULL,
    order_up_at      datetime2(3)   NULL,
    paged_at         datetime2(3)   NULL,
    completed_at     datetime2(3)   NULL,
    synced_at        datetime2(3)   NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE INDEX idx_orders_created ON dbo.orders(created_at);"))
        L.Add(New KeyValuePair(Of String, String)("order_lines", "
CREATE TABLE dbo.order_lines (
    uid         nvarchar(64)   NOT NULL PRIMARY KEY,
    order_id    nvarchar(64)   NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
    item_id     nvarchar(64)   NOT NULL,
    name        nvarchar(200)  NOT NULL,
    base_price  int            NOT NULL,
    qty         int            NOT NULL,
    taxable     int            NOT NULL DEFAULT 1,
    note        nvarchar(1000) NULL,
    line_index  int            NOT NULL DEFAULT 0,
    auto_discount_id      nvarchar(64)  NULL,
    auto_discount_name    nvarchar(200) NULL,
    auto_discount_type    nvarchar(10)  NULL,
    auto_discount_value   float         NULL,
    auto_discount_amount  int           NOT NULL DEFAULT 0
);
CREATE INDEX idx_lines_order ON dbo.order_lines(order_id);"))
        L.Add(New KeyValuePair(Of String, String)("order_line_modifiers", "
CREATE TABLE dbo.order_line_modifiers (
    id           int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    line_uid     nvarchar(64)  NOT NULL REFERENCES dbo.order_lines(uid) ON DELETE CASCADE,
    group_id     nvarchar(64)  NOT NULL,
    group_name   nvarchar(200) NOT NULL,
    option_id    nvarchar(64)  NOT NULL,
    option_name  nvarchar(200) NOT NULL,
    price_delta  int           NOT NULL DEFAULT 0
);
CREATE INDEX idx_linemods_line ON dbo.order_line_modifiers(line_uid);"))
        ' Not a register table: which ticket lines the Kitchen Display app has ticked off.
        L.Add(New KeyValuePair(Of String, String)(KitchenTable, "
CREATE TABLE dbo.kitchen_line_done (
    line_uid  nvarchar(64) NOT NULL PRIMARY KEY,
    done_at   datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);"))
        Return L
    End Function

    Private Const KitchenTable As String = "kitchen_line_done"
    Public Const DefaultKitchenLogin As String = "kitchen_display"

    Private Shared ReadOnly SafeName As New Regex("^[A-Za-z0-9_]{1,100}$")

    ''' <param name="ConnectionString">The app's normal connection string; its Initial Catalog is the database to make sure of.
    ''' The same server is connected to (as the same Windows user) to create the database and the login.</param>
    ''' <param name="SyncLogin">The SQL login for the sync service (reads and writes the register tables).</param>
    ''' <param name="PasswordFolder">Where each new login's password is written.</param>
    ''' <param name="KitchenLogin">The SQL login for the Kitchen Display app (reads orders and the menu, bumps orders).</param>
    Public Shared Function EnsureReady(ByVal ConnectionString As String,
                                       Optional ByVal SyncLogin As String = DefaultLogin,
                                       Optional ByVal PasswordFolder As String = "C:\KFDisplay",
                                       Optional ByVal KitchenLogin As String = DefaultKitchenLogin) As SetupResult
        Dim R As New SetupResult
        Try
            Dim B As New SqlConnectionStringBuilder(ConnectionString)
            Dim DbName As String = B.InitialCatalog
            If Not SafeName.IsMatch(DbName) OrElse Not SafeName.IsMatch(SyncLogin) OrElse Not SafeName.IsMatch(KitchenLogin) Then
                R.Errors.Add("The database or login name has characters that are not allowed.")
                Return R
            End If
            B.ConnectTimeout = 5 'short: this runs on the screen's thread every 15 s while SQL Server is down
            Dim MasterB As New SqlConnectionStringBuilder(B.ConnectionString)
            MasterB.InitialCatalog = "master"

            ' 1. the database
            Using Master As New SqlConnection(MasterB.ConnectionString)
                Try
                    Master.Open()
                Catch ex As Exception
                    R.Errors.Add("Can't reach SQL Server: " & ex.Message)
                    Return R
                End Try
                R.Connected = True
                Using Cmd As New SqlCommand("SELECT DB_ID(@n)", Master)
                    Cmd.Parameters.AddWithValue("@n", DbName)
                    If IsDBNull(Cmd.ExecuteScalar()) Then
                        Try
                            Using Create As New SqlCommand("CREATE DATABASE [" & DbName & "]", Master)
                                Create.ExecuteNonQuery()
                            End Using
                            R.DatabaseCreated = True
                        Catch ex As Exception
                            R.Errors.Add("Couldn't create the database " & DbName & ": " & ex.Message)
                            Return R
                        End Try
                    End If
                End Using
            End Using

            ' 2. the tables (only the missing ones, all or nothing)
            Try
                Using Db As New SqlConnection(B.ConnectionString)
                    Db.Open()
                    Using Tx As SqlTransaction = Db.BeginTransaction()
                        For Each T In TableScripts()
                            Using Check As New SqlCommand("SELECT OBJECT_ID(@t, N'U')", Db, Tx)
                                Check.Parameters.AddWithValue("@t", "dbo." & T.Key)
                                If IsDBNull(Check.ExecuteScalar()) Then
                                    Using Create As New SqlCommand(T.Value, Db, Tx)
                                        Create.ExecuteNonQuery()
                                    End Using
                                    R.TablesCreated.Add(T.Key)
                                End If
                            End Using
                        Next
                        Tx.Commit()
                    End Using
                End Using
            Catch ex As Exception
                R.TablesCreated.Clear()
                R.Errors.Add("Couldn't create the tables: " & ex.Message)
                Return R
            End Try

            ' 3. the SQL logins and database users, only for a database made just now
            If R.DatabaseCreated Then
                Dim RegisterTables As New List(Of String)
                For Each T In TableScripts()
                    If T.Key <> KitchenTable Then RegisterTables.Add(T.Key)
                Next
                ' The sync service (kfdisplay-sync): read and write the register tables, nothing else.
                Dim SyncGrants As New List(Of String)
                For Each T As String In RegisterTables
                    SyncGrants.Add("GRANT SELECT, INSERT, UPDATE, DELETE ON dbo." & T & " TO [" & SyncLogin & "]")
                Next
                TryLogin(MasterB.ConnectionString, B.ConnectionString, DbName, SyncLogin, PasswordFolder, R,
                         "SQL login for the kfdisplay-sync service:", "KFDISPLAY_USER", "KFDISPLAY_PASSWORD",
                         "Put these two lines in kfdisplay-sync's .env", SyncGrants)
                ' The Kitchen Display app: read the orders and menu, change only the ready / bumped times and its own table.
                Dim KitchenGrants As New List(Of String)
                For Each T As String In {"orders", "order_lines", "order_line_modifiers", "items", "categories"}
                    KitchenGrants.Add("GRANT SELECT ON dbo." & T & " TO [" & KitchenLogin & "]")
                Next
                KitchenGrants.Add("GRANT UPDATE (order_up_at, completed_at) ON dbo.orders TO [" & KitchenLogin & "]")
                KitchenGrants.Add("GRANT SELECT, INSERT, DELETE ON dbo." & KitchenTable & " TO [" & KitchenLogin & "]")
                TryLogin(MasterB.ConnectionString, B.ConnectionString, DbName, KitchenLogin, PasswordFolder, R,
                         "SQL login for the Kitchen Display server:", "DB_USER", "DB_PASSWORD",
                         "Put these two lines in the Kitchen Display's .env", KitchenGrants)
            End If
        Catch ex As Exception
            R.Errors.Add(ex.Message)
        End Try
        Return R
    End Function

    ''' <summary>EnsureLogin, with a failure for this login reported in the result instead of stopping the others.</summary>
    Private Shared Sub TryLogin(ByVal MasterConn As String, ByVal DbConn As String, ByVal DbName As String,
                                ByVal Login As String, ByVal PasswordFolder As String, ByVal R As SetupResult,
                                ByVal Purpose As String, ByVal UserKey As String, ByVal PasswordKey As String,
                                ByVal Where As String, ByVal Grants As List(Of String))
        Try
            EnsureLogin(MasterConn, DbConn, DbName, Login, PasswordFolder, R, Purpose, UserKey, PasswordKey, Where, Grants)
        Catch ex As Exception
            R.Errors.Add("Couldn't set up the SQL login " & Login & ": " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Creates the login with a random password if it doesn't exist (an existing login and its password are left alone),
    ''' maps it to a user in the database, and applies the grants.
    ''' </summary>
    Private Shared Sub EnsureLogin(ByVal MasterConn As String, ByVal DbConn As String, ByVal DbName As String,
                                   ByVal Login As String, ByVal PasswordFolder As String, ByVal R As SetupResult,
                                   ByVal Purpose As String, ByVal UserKey As String, ByVal PasswordKey As String,
                                   ByVal Where As String, ByVal Grants As List(Of String))
        Using Master As New SqlConnection(MasterConn)
            Master.Open()
            Dim Exists As Boolean
            Using Cmd As New SqlCommand("SELECT COUNT(*) FROM sys.server_principals WHERE name = @n", Master)
                Cmd.Parameters.AddWithValue("@n", Login)
                Exists = CInt(Cmd.ExecuteScalar()) > 0
            End Using
            If Not Exists Then
                Dim Password As String = NewPassword()
                Using Cmd As New SqlCommand("CREATE LOGIN [" & Login & "] WITH PASSWORD = N'" & Password & "', CHECK_POLICY = ON, DEFAULT_DATABASE = [" & DbName & "]", Master)
                    Cmd.ExecuteNonQuery()
                End Using
                Directory.CreateDirectory(PasswordFolder)
                Dim PasswordFile As String = Path.Combine(PasswordFolder, Login & "-password.txt")
                File.WriteAllText(PasswordFile,
                    "Created by KFIDisplay on " & Now.ToString("yyyy-MM-dd HH:mm") & " because the " & DbName & " database was missing." & vbCrLf &
                    Purpose & vbCrLf &
                    UserKey & "=" & Login & vbCrLf &
                    PasswordKey & "=" & Password & vbCrLf &
                    Where & ", then delete this file." & vbCrLf)
                R.NewLogins.Add(Login)
                R.PasswordFiles.Add(PasswordFile)
            End If
        End Using
        Using Db As New SqlConnection(DbConn)
            Db.Open()
            Using Cmd As New SqlCommand("IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'" & Login & "') CREATE USER [" & Login & "] FOR LOGIN [" & Login & "]", Db)
                Cmd.ExecuteNonQuery()
            End Using
            For Each Grant As String In Grants
                Using Cmd As New SqlCommand(Grant, Db)
                    Cmd.ExecuteNonQuery()
                End Using
            Next
        End Using
    End Sub

    ''' <summary>24 letters and digits (always some of each case and a digit, which SQL Server's password policy wants).</summary>
    Private Shared Function NewPassword() As String
        Const Upper As String = "ABCDEFGHJKLMNPQRSTUVWXYZ"
        Const Lower As String = "abcdefghijkmnopqrstuvwxyz"
        Const Digits As String = "23456789"
        Dim All As String = Upper & Lower & Digits
        Dim Chars As New List(Of Char)
        Using Rng As New RNGCryptoServiceProvider
            Chars.Add(Pick(Rng, Upper))
            Chars.Add(Pick(Rng, Lower))
            Chars.Add(Pick(Rng, Digits))
            While Chars.Count < 24
                Chars.Add(Pick(Rng, All))
            End While
            ' shuffle (Fisher-Yates)
            For I As Integer = Chars.Count - 1 To 1 Step -1
                Dim J As Integer = CInt(Below(Rng, I + 1))
                Dim Tmp As Char = Chars(I) : Chars(I) = Chars(J) : Chars(J) = Tmp
            Next
        End Using
        Return New String(Chars.ToArray())
    End Function

    Private Shared Function Pick(ByVal Rng As RNGCryptoServiceProvider, ByVal From As String) As Char
        Return From(CInt(Below(Rng, From.Length)))
    End Function

    ''' <summary>A uniformly random number from 0 to Limit - 1 (no modulo bias).</summary>
    Private Shared Function Below(ByVal Rng As RNGCryptoServiceProvider, ByVal Limit As Integer) As UInteger
        Dim Buf(3) As Byte
        Dim Max As UInteger = UInteger.MaxValue - (UInteger.MaxValue Mod CUInt(Limit)) - 1UI
        Dim V As UInteger
        Do
            Rng.GetBytes(Buf)
            V = BitConverter.ToUInt32(Buf, 0)
        Loop While V > Max
        Return V Mod CUInt(Limit)
    End Function
End Class
