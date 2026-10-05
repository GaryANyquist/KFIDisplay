Imports System.Data.Common
Imports System.Data.SqlClient
Imports AnnawareServices

''' <summary>
''' A view of the menu as the register tablet sent it (double-click the photo on the menu board).
''' The tablet owns the menu now, so nothing can be edited or reordered here: an edit made in this
''' database would be overwritten the next time the tablet syncs that item. Change the menu on the
''' tablet (Items), including "Show on menu board", "Out of stock" and the description.
''' </summary>
Public Class Form2
    Dim SQLConnection As DbConnection

    Private Shared Function Q(ByVal s As String) As String
        Return s.Replace("'", "''")
    End Function

    Private Shared Function Money(ByVal cents As Object) As String
        Dim dollars As Decimal = CDec(cents) / 100D
        Return "$" & FormatNumber(dollars, 2).Replace(".00", "")
    End Function

    Public Sub FillTreeView()
        Dim ExpandNode As String = ""
        For Each TVNode As TreeNode In TreeView1.Nodes
            If TVNode.IsExpanded Then
                ExpandNode = TVNode.Tag.ToString
            End If
        Next

        TreeView1.Nodes.Clear()
        Dim DS As DataSet = AnnawareData.ReturnRecords("SELECT id, name FROM categories ORDER BY sort_order, name", SQLConnection, "SQL")
        For Each Cat As DataRow In DS.Tables(0).Rows
            Dim CatNode As TreeNode = TreeView1.Nodes.Add(Cat("name").ToString)
            CatNode.Tag = Cat("id").ToString
            Dim DS2 As DataSet = AnnawareData.ReturnRecords("SELECT id, name, price, show_on_menu_board, out_of_stock, archived FROM items WHERE category_id = '" & Q(Cat("id").ToString) & "' ORDER BY sort_order, name", SQLConnection, "SQL")
            For Each Item As DataRow In DS2.Tables(0).Rows
                Dim Text As String = Item("name").ToString & " " & Money(Item("price"))
                If CInt(Item("archived")) = 1 Then
                    Text &= "  (hidden)"
                ElseIf CInt(Item("show_on_menu_board")) = 0 Then
                    Text &= "  (not on board)"
                End If
                If CInt(Item("out_of_stock")) = 1 Then
                    Text &= "  SOLD OUT"
                End If
                Dim ItemNode As TreeNode = CatNode.Nodes.Add(Text)
                ItemNode.Tag = Item("id").ToString
            Next
            If CatNode.Tag.ToString = ExpandNode Then
                CatNode.Expand()
            End If
        Next
    End Sub

    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Text = "Menu (view only: change it on the register tablet)"
        ItemPanel.Top = 0
        ItemPanel.Left = 305
        CategoryPanel.Top = 0
        CategoryPanel.Left = 305
        'view only
        TreeView1.AllowDrop = False
        AddMenuItem.Visible = False
        ItemSaveButton.Visible = False
        DeleteButton.Visible = False
        ItemNumber.ReadOnly = True
        Description.ReadOnly = True
        Price.ReadOnly = True
        Category.Enabled = False
        Enabled.AutoCheck = False
        SoldOut.AutoCheck = False
        SQLConnection = New SqlConnection(Form1.ConnectionString)
        FillTreeView()
    End Sub

    Private Sub TreeView1_DoubleClick(sender As Object, e As EventArgs) Handles TreeView1.DoubleClick
        CategoryPanel.Visible = False
        ItemPanel.Visible = False
        If TreeView1.SelectedNode Is Nothing Then
            Exit Sub
        End If
        If TreeView1.SelectedNode.Level = 0 Then
            CategoryPanel.Visible = True
        End If
        If TreeView1.SelectedNode.Level = 1 Then
            Dim DS As DataSet = AnnawareData.ReturnRecords("SELECT i.id, i.name, i.description, i.price, i.show_on_menu_board, i.out_of_stock, c.name AS category FROM items i LEFT JOIN categories c ON c.id = i.category_id WHERE i.id = '" & Q(TreeView1.SelectedNode.Tag.ToString) & "'", SQLConnection, "SQL")
            If DS.Tables(0).Rows.Count > 0 Then
                Dim Row As DataRow = DS.Tables(0).Rows(0)
                ItemID.Text = Row("id").ToString
                ItemNumber.Text = Row("name").ToString
                Category.Items.Clear()
                Category.Items.Add(Row("category").ToString)
                Category.Text = Row("category").ToString
                Description.Text = Row("description").ToString
                Price.Text = Money(Row("price"))
                Enabled.Checked = CInt(Row("show_on_menu_board")) = 1
                SoldOut.Checked = CInt(Row("out_of_stock")) = 1
                ItemPanel.Visible = True
            End If
        End If
    End Sub

    Private Sub CategoryCloseButton_Click(sender As Object, e As EventArgs) Handles CategoryCloseButton.Click
        CategoryPanel.Visible = False
    End Sub

    Private Sub CategoryButtonSave_Click(sender As Object, e As EventArgs) Handles CategoryButtonSave.Click
        Me.Close()
    End Sub

    Private Sub ItemCloseButton_Click(sender As Object, e As EventArgs) Handles ItemCloseButton.Click
        Me.Close()
    End Sub
End Class
