Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.Design
Imports System.Data.Common
Imports System.Data.SqlClient
Imports System.Data.SqlTypes
Imports System.Deployment.Application
Imports System.IO
Imports System.IO.Pipelines
Imports System.Net
Imports System.Net.Security
Imports System.Reflection.Emit
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Security.Policy
Imports System.Xml.XPath
Imports AnnawareServices
Imports System.Diagnostics
Imports System.ComponentModel


Public Class Form1
    Public ConnectionString As String = "Data Source=.\SQLEXPRESS;Initial Catalog=KFDisplay;Integrated Security=True"
    Dim SQLConnection As DbConnection
    Dim SQLCommand As DbCommand
    Dim LastSortSeq As Integer = 0
    Dim DisclaimerCount As Integer = 0

    Function ToInt(ByVal B As Boolean) As Integer
        If B Then
            Return 1
        Else
            Return 0
        End If
    End Function

    'Sub ToggleWifi(enable As Boolean)
    '    Try
    '        ' Create a new process to run netsh command
    '        Dim SettingsDS As DataSet = AnnawareData.ReturnRecords("SELECT * FROM Settings", SQLConnection, "SQL")
    '        Dim process As New Process()
    '        Dim startInfo As New ProcessStartInfo()
    '        startInfo.FileName = "powershell"
    '        startInfo.Arguments = If(enable, "enable-netadapter -Name """ & SettingsDS.Tables(0).Rows(0)("InterfaceName").ToString & """ -Confirm:$false", "disable-netadapter -Name """ & SettingsDS.Tables(0).Rows(0)("InterfaceName").ToString & """ -Confirm:$false")
    '        startInfo.UseShellExecute = False
    '        startInfo.CreateNoWindow = True
    '        startInfo.RedirectStandardOutput = True
    '        startInfo.Verb = "runas" ' Request administrative privileges
    '        process.StartInfo = startInfo

    '        ' Start the process
    '        process.Start()
    '        process.WaitForExit()

    '        'if enabling, wait 10 seconds for wifi to connect
    '        If enable Then
    '            System.Threading.Thread.Sleep(10000)
    '        End If
    '    Catch ex As Exception
    '    End Try
    'End Sub

    Sub UpdateDatabaseSchema()
        'create the database if it is not there
        Try
            SQLConnection = New SqlConnection("Data Source=.\SQLEXPRESS;Initial Catalog=master;Integrated Security=True")
            SQLConnection.Open()
            SQLCommand = New SqlCommand("Create database KFDisplay;", SQLConnection)
            SQLCommand.ExecuteNonQuery()
            SQLConnection.Close()
            SQLConnection.Dispose()
            For delay As Integer = 1 To 5
                System.Threading.Thread.Sleep(1000)
            Next
        Catch ex As Exception
        End Try

        SQLConnection = New SqlConnection(ConnectionString)
        SQLConnection.Open()

        'The menu tables (categories, items, modifier_groups, modifier_options, item_modifier_groups,
        'discounts, auto_discounts, ...) belong to the register tablet, which creates and fills them
        'through the KFDisplay sync service (C:\Source\kfdisplay-sync). Never create or alter them here:
        'SQL Server names are case-insensitive, so the old Categories/MenuItems/Discounts setup would
        'add its columns to the tablet's tables. Only Settings and Gallery are still this app's own.

        'Create Settings table
        Try
            SQLCommand = New SqlCommand("CREATE TABLE Settings (TempField INTEGER NULL)", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Settings ADD ID INTEGER IDENTITY (1,1)", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Settings ADD LastItemUpdate DATETIME", SQLConnection)
            SQLCommand.ExecuteNonQuery()
            SQLCommand = New SqlCommand("INSERT INTO Settings (LastItemUpdate) VALUES ('1/1/2000')", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Settings ADD LastCategoryUpdate DATETIME", SQLConnection)
            SQLCommand.ExecuteNonQuery()
            SQLCommand = New SqlCommand("UPDATE Settings SET LastCategoryUpdate='1/1/2000'", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Settings ADD LastGalleryUpdate DATETIME", SQLConnection)
            SQLCommand.ExecuteNonQuery()
            SQLCommand = New SqlCommand("UPDATE Settings SET LastGalleryUpdate='1/1/2000'", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Settings ADD InterfaceName NVARCHAR(100)", SQLConnection)
            SQLCommand.ExecuteNonQuery()
            SQLCommand = New SqlCommand("UPDATE Settings SET InterfaceName='Wi-Fi'", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Settings ADD LastDiscountUpdate DATETIME", SQLConnection)
            SQLCommand.ExecuteNonQuery()
            SQLCommand = New SqlCommand("UPDATE Settings SET LastDiscountUpdate='1/1/2000'", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try

        'Create Gallery table
        Try
            SQLCommand = New SqlCommand("CREATE TABLE Gallery (TempField INTEGER NULL)", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD ID INTEGER IDENTITY (1,1)", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD WixID NVARCHAR (200) NULL", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD URL NVARCHAR (500) NULL", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD Description NVARCHAR (500) NULL", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD SortOrder Float Default 0", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD DisplayTime INT Default 30", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try
        Try
            SQLCommand = New SqlCommand("ALTER TABLE Gallery ADD DeleteFlag INT Default 0", SQLConnection)
            SQLCommand.ExecuteNonQuery()
        Catch ex As Exception
        End Try

        SQLConnection.Close()

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.DoubleBuffered = True
        Try
            MkDir("c:\KFDisplay")
        Catch ex As Exception
        End Try
        UpdateDatabaseSchema()
        Timer1_Tick(sender, e)
        Timer2_Tick(sender, e)
    End Sub

    ''' <summary>
    ''' The menu board rows, from the register tablet's tables, with the column names FillMenu
    ''' expects: Category, ItemNumber (the item's name), Description, Price, Price2 (the biggest
    ''' size, e.g. Cup/Bowl, or NULL), SoldOut, SortOrder (a running number for paging) and
    ''' SalesPrice (the price after today's automatic discount, or -1; 0 for everything while the
    ''' most recently opened drawer that is still open is a prepaid event, because the tablet rings
    ''' every menu item up at $0.00 then). The tablet stores money in
    ''' cents. Only items switched on with "Show on menu board" (and not hidden) are shown.
    ''' </summary>
    Private Function MenuBoardSql(ByVal AfterSortOrder As Long) As String
        Return "WITH board AS (" &
            " SELECT c.name AS Category, i.name AS ItemNumber, ISNULL(i.description, '') AS Description," &
            "  i.price AS PriceCents, i.out_of_stock AS SoldOut," &
            "  ISNULL((SELECT MAX(o.price_delta) FROM item_modifier_groups l" &
            "    JOIN modifier_groups g ON g.id = l.group_id JOIN modifier_options o ON o.group_id = g.id" &
            "    WHERE l.item_id = i.id AND g.required = 1 AND g.multi_select = 0), 0) AS SizeUpCents," &
            "  ISNULL(disc.OffCents, 0) AS OffCents," &
            "  ROW_NUMBER() OVER (ORDER BY c.sort_order, c.name, i.sort_order, i.name) AS SortOrder" &
            " FROM items i JOIN categories c ON c.id = i.category_id" &
            " OUTER APPLY (SELECT MAX(x.OffCents) AS OffCents FROM (" &
            "    SELECT CASE WHEN d.type = 'percent' THEN ROUND(i.price * d.value / 100.0, 0)" &
            "                WHEN d.value > i.price THEN i.price ELSE d.value END AS OffCents" &
            "    FROM auto_discounts d" &
            "    WHERE d.active = 1" &
            "      AND (d.starts_on IS NULL OR d.starts_on <= CONVERT(char(10), GETDATE(), 23))" &
            "      AND (d.ends_on IS NULL OR d.ends_on >= CONVERT(char(10), GETDATE(), 23))" &
            "      AND EXISTS (SELECT 1 FROM auto_discount_targets t WHERE t.discount_id = d.id" &
            "        AND ((t.target_type = 'item' AND t.target_id = i.id) OR (t.target_type = 'category' AND t.target_id = i.category_id)))) x) disc" &
            " WHERE i.show_on_menu_board = 1 AND i.archived = 0)" &
            " SELECT Category, ItemNumber, Description, CAST(PriceCents / 100.0 AS money) AS Price," &
            "  CASE WHEN SizeUpCents > 0 THEN CAST((PriceCents + SizeUpCents) / 100.0 AS money) ELSE NULL END AS Price2," &
            "  SoldOut, SortOrder," &
            "  CASE WHEN EXISTS (SELECT 1 FROM (SELECT TOP 1 prepaid FROM shifts WHERE closed_at IS NULL ORDER BY opened_at DESC) s WHERE s.prepaid = 1)" &
            "       THEN CAST(0 AS money)" &
            "       WHEN OffCents > 0 THEN CAST((PriceCents - OffCents) / 100.0 AS money) ELSE CAST(-1 AS money) END AS SalesPrice" &
            " FROM board WHERE SortOrder > " & AfterSortOrder & " ORDER BY SortOrder"
    End Function

    Private Sub FillMenu()
        SQLConnection = New SqlConnection(ConnectionString)
        Dim CategoryIx As Integer = 0
        Dim ItemIx As Integer = 0
        Dim SaveCategory As String = ""
        Dim TopPosition As Integer = 10
        Dim LeftPosition As Integer = AdPicture.Left + AdPicture.Width + 10

        Try
            'show the category
            Dim DS As DataSet = AnnawareData.ReturnRecords(MenuBoardSql(LastSortSeq), SQLConnection, "SQL")
            If DS.Tables(0).Rows.Count = 0 Then
                DS = AnnawareData.ReturnRecords(MenuBoardSql(0), SQLConnection, "SQL")
                LastSortSeq = 0
            Else
                CategoryIx = CategoryIx + 1
                Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Visible = True
                Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Top = TopPosition
                Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Left = LeftPosition
                Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Controls("Category" & (CategoryIx).ToString).Text = DS.Tables(0).Rows(0)("Category").ToString
                TopPosition = TopPosition + 60
                SaveCategory = DS.Tables(0).Rows(0)("Category").ToString
            End If
            For Each MenuItem In DS.Tables(0).Rows
                If MenuItem("Category").ToString <> SaveCategory Then
                    If TopPosition > (Me.Height - Me.CategoryGroupBox1.Height) - 300 Then
                        If LeftPosition > AdPicture.Left + AdPicture.Width + 20 Then
                            Exit For
                        End If
                        LeftPosition = LeftPosition + Me.CategoryGroupBox1.Width + 10
                        TopPosition = 10
                    End If
                    CategoryIx = CategoryIx + 1
                    Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Visible = True
                    Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Top = TopPosition
                    Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Left = LeftPosition
                    Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Controls("Category" & (CategoryIx).ToString).Text = MenuItem("Category").ToString
                    TopPosition = TopPosition + 60
                    SaveCategory = MenuItem("Category").ToString
                End If
                If TopPosition > (Me.Height - Me.ItemGroupBox1.Height) - 100 Then
                    LeftPosition = LeftPosition + Me.CategoryGroupBox1.Width + 10
                    TopPosition = 10
                End If
                ItemIx = ItemIx + 1
                Me.Controls("ItemGroupBox" & ItemIx.ToString).Visible = True
                Me.Controls("ItemGroupBox" & ItemIx.ToString).Top = TopPosition
                Me.Controls("ItemGroupBox" & ItemIx.ToString).Left = LeftPosition
                Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Item" & ItemIx.ToString).Text = MenuItem("ItemNumber").ToString
                If MenuItem("SalesPrice") > -1 Then
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Price" & ItemIx.ToString).ForeColor = Color.Gray
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Price" & ItemIx.ToString).Text = "$" & FormatNumber(MenuItem("Price")).Replace(".00", "")
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("SalePrice" & ItemIx.ToString).Text = "$" & FormatNumber(MenuItem("SalesPrice")).Replace(".00", "")
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("SalePrice" & ItemIx.ToString).Visible = True
                Else
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Price" & ItemIx.ToString).ForeColor = Color.Black
                    If Val("0" & MenuItem("Price2").ToString) > 0 Then
                        Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Price" & ItemIx.ToString).Text = "$" & FormatNumber(MenuItem("Price")).Replace(".00", "") & "-" & FormatNumber(MenuItem("Price2")).Replace(".00", "")
                    Else
                        Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Price" & ItemIx.ToString).Text = "$" & FormatNumber(MenuItem("Price")).Replace(".00", "")
                    End If
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("SalePrice" & ItemIx.ToString).Visible = False
                End If
                Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("Description" & ItemIx.ToString).Text = MenuItem("Description").ToString
                If MenuItem("SoldOut") = 1 Then
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("SoldOut" & ItemIx.ToString).Visible = True
                Else
                    Me.Controls("ItemGroupBox" & ItemIx.ToString).Controls("SoldOut" & ItemIx.ToString).Visible = False
                End If
                LastSortSeq = MenuItem("SortOrder")
                TopPosition = TopPosition + 96
                If ItemIx = 20 Or (LeftPosition > AdPicture.Left + AdPicture.Width + 20 And TopPosition > (Me.Height - Me.ItemGroupBox1.Height) - 100) Then
                    Exit For
                End If
            Next
            Do
                CategoryIx = CategoryIx + 1
                If CategoryIx > 9 Then
                    Exit Do
                End If
                Me.Controls("CategoryGroupBox" & (CategoryIx).ToString).Visible = False
            Loop
            Do
                ItemIx = ItemIx + 1
                If ItemIx > 20 Then
                    Exit Do
                End If
                Me.Controls("ItemGroupBox" & (ItemIx).ToString).Visible = False
            Loop
            ErrorLabel.Text = ""
        Catch ex As Exception
            My.Computer.FileSystem.WriteAllText("c:\KFDisplay\log.txt", Now.ToString & Chr(9) & "Error on menu fill: " & ex.Message & " | " & ex.StackTrace & vbCrLf, True)
            ErrorLabel.Text = "Fill : " & ex.Message
        End Try
    End Sub
    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AdPicture.Left = 10
        AdPicture.Width = Me.Width - 1000
        AdPicture.Height = AdPicture.Width
        ErrorLabel.Top = AdPicture.Top + AdPicture.Height + 10
        ErrorLabel.Left = AdPicture.Left
        ImageDescription.Top = AdPicture.Top + AdPicture.Height + 10
        ImageDescription.Left = AdPicture.Left
        ImageDescription.Width = AdPicture.Width
        ImageDescription.BringToFront()
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Timer1.Enabled = False
        FillMenu()
        Timer1.Enabled = True
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Timer2.Enabled = False
        Try
            DisclaimerCount = DisclaimerCount + 1
            If DisclaimerCount > 15 Then
                DisclaimerCount = 0
                AdPicture.Image = System.Drawing.Image.FromFile("C:\images\disclaimer.jpg")
                AdPicture.Refresh()
                ImageDescription.Text = ""
                Timer2.Interval = 20000
            Else
                Dim DS As DataSet = AnnawareData.ReturnRecords("SELECT * FROM Gallery ORDER BY NEWID()", SQLConnection, "SQL")
                If DS.Tables(0).Rows.Count > 0 Then
                    If Dir(DS.Tables(0).Rows(0)("Url").ToString) > "" Then
                        AdPicture.Image = System.Drawing.Image.FromFile(DS.Tables(0).Rows(0)("Url").ToString)
                        AdPicture.Refresh()
                        ImageDescription.Text = DS.Tables(0).Rows(0)("Description").ToString
                    End If
                End If
                Timer2.Interval = 3000
            End If
        Catch ex As Exception
        End Try
        Timer2.Enabled = True
    End Sub

    'Double-click the picture: view the menu as the tablet sent it (view only).
    Private Sub AdPicture_DoubleClick(sender As Object, e As EventArgs) Handles AdPicture.DoubleClick
        Form2.ShowDialog()
    End Sub

    Private Sub AdPicture_Paint(sender As Object, e As PaintEventArgs) Handles AdPicture.Paint
        'AdPicture.BorderStyle = BorderStyle.None

        'Dim g As Graphics = e.Graphics
        'Dim rect As Rectangle = New Rectangle(0, 0, AdPicture.Width - 1, AdPicture.Height - 1)
        'Dim bevelWidth As Integer = 8 ' Thickness of the bevel

        '' Light color for top/left (simulating light source)
        'Dim lightPen As New Pen(Color.DarkViolet, bevelWidth)
        '' Dark color for bottom/right (simulating shadow)
        'Dim darkPen As New Pen(Color.Purple, bevelWidth)

        '' Draw top bevel (light)
        'g.DrawLine(lightPen, rect.X, rect.Y, rect.Right, rect.Y)
        '' Draw left bevel (light)
        'g.DrawLine(lightPen, rect.X, rect.Y, rect.X, rect.Bottom)
        '' Draw bottom bevel (shadow)
        'g.DrawLine(darkPen, rect.X, rect.Bottom, rect.Right, rect.Bottom)
        '' Draw right bevel (shadow)
        'g.DrawLine(darkPen, rect.Right, rect.Y, rect.Right, rect.Bottom)

        '' Clean up
        'lightPen.Dispose()
        'darkPen.Dispose()

        ''Dim transparentImage As Image
        ''transparentImage = Image.FromFile("C:\Source\KFIDisplay\sandwich.png")
        ''g.DrawImage(transparentImage, New Point(50, 50))
    End Sub

    Private Sub Price_Paint(sender As Object, e As PaintEventArgs) Handles Price1.Paint, Price2.Paint, Price3.Paint, Price4.Paint, Price5.Paint, Price6.Paint, Price7.Paint, Price8.Paint, Price9.Paint, Price10.Paint, Price11.Paint, Price12.Paint, Price13.Paint, Price14.Paint, Price15.Paint, Price16.Paint, Price17.Paint, Price18.Paint, Price19.Paint, Price20.Paint
        If sender.ForeColor = Color.Gray Then
            Dim img As Image = Image.FromFile("C:\images\slash.gif")
            e.Graphics.DrawImage(img, sender.width - 30, 0, 20, 20)
        End If

    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        'ToggleWifi(True)
    End Sub
End Class
