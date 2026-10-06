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
    Dim LastSortSeq As Integer = 0
    Dim DisclaimerCount As Integer = 0

    Function ToInt(ByVal B As Boolean) As Integer
        If B Then
            Return 1
        Else
            Return 0
        End If
    End Function

    'The menu tables (categories, items, modifier_groups, ...) belong to the register tablet, which creates
    'and fills them through the KFDisplay sync service (C:\Source\kfdisplay-sync). This app only reads them;
    'it no longer has tables of its own (the old Settings and Gallery tables are no longer used, 2026-10-06).

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.DoubleBuffered = True
        Try
            MkDir("c:\KFDisplay")
        Catch ex As Exception
        End Try
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
    ''' A sale-price discount (from Wix, auto_discounts.sale_price, added by kfdisplay-sync setup 07) takes
    ''' off the difference to the sale price; before 07 is run the column is missing and is left out.
    ''' </summary>
    Private Function MenuBoardSql(ByVal AfterSortOrder As Long) As String
        Dim SalePrice As String = If(HasSalePriceColumn(),
            "WHEN d.sale_price IS NOT NULL THEN CASE WHEN i.price > d.sale_price THEN i.price - d.sale_price ELSE 0 END ", "")
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
            "    SELECT CASE " & SalePrice & "WHEN d.type = 'percent' THEN ROUND(i.price * d.value / 100.0, 0)" &
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

    ''' <summary>True once kfdisplay-sync setup 07 has added auto_discounts.sale_price.</summary>
    Private Function HasSalePriceColumn() As Boolean
        Try
            Dim DS As DataSet = AnnawareData.ReturnRecords(
                "SELECT CASE WHEN COL_LENGTH('dbo.auto_discounts', 'sale_price') IS NULL THEN 0 ELSE 1 END AS HasIt",
                New SqlConnection(ConnectionString), "SQL")
            Return CInt(DS.Tables(0).Rows(0)("HasIt")) = 1
        Catch ex As Exception
            Return False
        End Try
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

    ''' <summary>
    ''' Where the photo for an item is on this PC. The tablet sends the photo's file name
    ''' (e.g. 5fbd284e-....jpg, the Wix item id), or a tablet path (file:///.../item-photos/x.jpg) for a photo
    ''' taken or downloaded on the tablet; the file is looked for by that name in C:\images. A tablet photo's name
    ''' changes whenever the tablet downloads it again (it_160-wix-1791315247281.jpg), so if that exact file isn't
    ''' here, the newest file in C:\images that starts with the item's id and a dash (it_160-...) is used instead.
    ''' </summary>
    Private Function ItemPhotoPath(ByVal ItemId As String, ByVal Image As String) As String
        If Image = "" Then Return ""
        Dim Name As String = Image
        Try
            If Image.Contains("://") Then Name = Uri.UnescapeDataString(New Uri(Image).Segments.Last())
        Catch ex As Exception
        End Try
        Dim Candidate As String = Path.Combine("C:\images", Path.GetFileName(Name))
        If File.Exists(Candidate) Then Return Candidate
        If ItemId = "" Then Return ""
        Try
            Dim Newest As String = ""
            For Each F As String In Directory.GetFiles("C:\images", ItemId & "-*")
                Dim Ext As String = Path.GetExtension(F).ToLower()
                If Ext = ".jpg" OrElse Ext = ".jpeg" OrElse Ext = ".png" OrElse Ext = ".gif" OrElse Ext = ".bmp" Then
                    If Newest = "" OrElse File.GetLastWriteTime(F) > File.GetLastWriteTime(Newest) Then Newest = F
                End If
            Next
            Return Newest
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>
    ''' Rotates the big picture through the photos of the items switched on with "Show on menu board"
    ''' (items.show_on_menu_board = 1), in random order, with the item's name under it. Every 16th turn
    ''' shows the disclaimer instead.
    ''' </summary>
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Timer2.Enabled = False
        Try
            DisclaimerCount = DisclaimerCount + 1
            If DisclaimerCount > 15 Then
                DisclaimerCount = 0
                ShowPicture("C:\images\disclaimer.jpg", "")
                Timer2.Interval = 20000
            Else
                Dim DS As DataSet = AnnawareData.ReturnRecords(
                    "SELECT id, name, image FROM items WHERE show_on_menu_board = 1 AND image IS NOT NULL AND image <> '' ORDER BY NEWID()",
                    New SqlConnection(ConnectionString), "SQL")
                For Each Row As DataRow In DS.Tables(0).Rows
                    Dim Photo As String = ItemPhotoPath(Row("id").ToString, Row("image").ToString)
                    If Photo <> "" Then
                        ShowPicture(Photo, Row("name").ToString)
                        Exit For
                    End If
                Next
                Timer2.Interval = 3000
            End If
        Catch ex As Exception
        End Try
        Timer2.Enabled = True
    End Sub

    Private Sub ShowPicture(ByVal FileName As String, ByVal Caption As String)
        Dim Previous As Image = AdPicture.Image
        AdPicture.Image = System.Drawing.Image.FromFile(FileName)
        AdPicture.Refresh()
        ImageDescription.Text = Caption
        If Previous IsNot Nothing Then Previous.Dispose()
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

End Class
