<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form2
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.TreeView1 = New System.Windows.Forms.TreeView()
        Me.CategoryPanel = New System.Windows.Forms.Panel()
        Me.CategoryID = New System.Windows.Forms.TextBox()
        Me.CategoryCloseButton = New System.Windows.Forms.Button()
        Me.CategoryButtonSave = New System.Windows.Forms.Button()
        Me.AddMenuItem = New System.Windows.Forms.Button()
        Me.ItemSaveButton = New System.Windows.Forms.Button()
        Me.ItemCloseButton = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.ItemNumber = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Category = New System.Windows.Forms.ComboBox()
        Me.Description = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Price = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Enabled = New System.Windows.Forms.CheckBox()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.ItemID = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.SoldOut = New System.Windows.Forms.CheckBox()
        Me.ItemPanel = New System.Windows.Forms.Panel()
        Me.DeleteButton = New System.Windows.Forms.Button()
        Me.CategoryPanel.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ItemPanel.SuspendLayout()
        Me.SuspendLayout()
        '
        'TreeView1
        '
        Me.TreeView1.AllowDrop = True
        Me.TreeView1.FullRowSelect = True
        Me.TreeView1.Location = New System.Drawing.Point(-3, 0)
        Me.TreeView1.Name = "TreeView1"
        Me.TreeView1.Size = New System.Drawing.Size(303, 539)
        Me.TreeView1.TabIndex = 1
        '
        'CategoryPanel
        '
        Me.CategoryPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CategoryPanel.Controls.Add(Me.CategoryID)
        Me.CategoryPanel.Controls.Add(Me.CategoryCloseButton)
        Me.CategoryPanel.Controls.Add(Me.CategoryButtonSave)
        Me.CategoryPanel.Location = New System.Drawing.Point(384, 12)
        Me.CategoryPanel.Name = "CategoryPanel"
        Me.CategoryPanel.Size = New System.Drawing.Size(491, 539)
        Me.CategoryPanel.TabIndex = 2
        Me.CategoryPanel.Visible = False
        '
        'CategoryID
        '
        Me.CategoryID.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CategoryID.Location = New System.Drawing.Point(8, 3)
        Me.CategoryID.Name = "CategoryID"
        Me.CategoryID.Size = New System.Drawing.Size(27, 20)
        Me.CategoryID.TabIndex = 16
        Me.CategoryID.Visible = False
        '
        'CategoryCloseButton
        '
        Me.CategoryCloseButton.Location = New System.Drawing.Point(412, 3)
        Me.CategoryCloseButton.Name = "CategoryCloseButton"
        Me.CategoryCloseButton.Size = New System.Drawing.Size(75, 23)
        Me.CategoryCloseButton.TabIndex = 15
        Me.CategoryCloseButton.Text = "Close"
        Me.CategoryCloseButton.UseVisualStyleBackColor = True
        '
        'CategoryButtonSave
        '
        Me.CategoryButtonSave.Location = New System.Drawing.Point(336, 3)
        Me.CategoryButtonSave.Name = "CategoryButtonSave"
        Me.CategoryButtonSave.Size = New System.Drawing.Size(75, 23)
        Me.CategoryButtonSave.TabIndex = 14
        Me.CategoryButtonSave.Text = "Save"
        Me.CategoryButtonSave.UseVisualStyleBackColor = True
        '
        'AddMenuItem
        '
        Me.AddMenuItem.Location = New System.Drawing.Point(-2, 515)
        Me.AddMenuItem.Name = "AddMenuItem"
        Me.AddMenuItem.Size = New System.Drawing.Size(22, 23)
        Me.AddMenuItem.TabIndex = 16
        Me.AddMenuItem.Text = "+"
        Me.AddMenuItem.UseVisualStyleBackColor = True
        '
        'ItemSaveButton
        '
        Me.ItemSaveButton.Location = New System.Drawing.Point(336, 3)
        Me.ItemSaveButton.Name = "ItemSaveButton"
        Me.ItemSaveButton.Size = New System.Drawing.Size(75, 23)
        Me.ItemSaveButton.TabIndex = 0
        Me.ItemSaveButton.Text = "Save"
        Me.ItemSaveButton.UseVisualStyleBackColor = True
        '
        'ItemCloseButton
        '
        Me.ItemCloseButton.Location = New System.Drawing.Point(412, 3)
        Me.ItemCloseButton.Name = "ItemCloseButton"
        Me.ItemCloseButton.Size = New System.Drawing.Size(75, 23)
        Me.ItemCloseButton.TabIndex = 1
        Me.ItemCloseButton.Text = "Close"
        Me.ItemCloseButton.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(56, 58)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(57, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Menu Item"
        '
        'ItemNumber
        '
        Me.ItemNumber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ItemNumber.Location = New System.Drawing.Point(119, 55)
        Me.ItemNumber.Name = "ItemNumber"
        Me.ItemNumber.Size = New System.Drawing.Size(322, 20)
        Me.ItemNumber.TabIndex = 3
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(56, 79)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(49, 13)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Category"
        '
        'Category
        '
        Me.Category.FormattingEnabled = True
        Me.Category.Location = New System.Drawing.Point(119, 76)
        Me.Category.Name = "Category"
        Me.Category.Size = New System.Drawing.Size(322, 21)
        Me.Category.TabIndex = 5
        '
        'Description
        '
        Me.Description.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Description.Location = New System.Drawing.Point(119, 99)
        Me.Description.Multiline = True
        Me.Description.Name = "Description"
        Me.Description.Size = New System.Drawing.Size(322, 56)
        Me.Description.TabIndex = 6
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(56, 102)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(60, 13)
        Me.Label3.TabIndex = 7
        Me.Label3.Text = "Description"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(56, 177)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(31, 13)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "Price"
        '
        'Price
        '
        Me.Price.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Price.Location = New System.Drawing.Point(119, 174)
        Me.Price.Name = "Price"
        Me.Price.Size = New System.Drawing.Size(65, 20)
        Me.Price.TabIndex = 9
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(56, 197)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(34, 13)
        Me.Label5.TabIndex = 10
        Me.Label5.Text = "Show"
        '
        'Enabled
        '
        Me.Enabled.AutoSize = True
        Me.Enabled.Location = New System.Drawing.Point(119, 197)
        Me.Enabled.Name = "Enabled"
        Me.Enabled.Size = New System.Drawing.Size(15, 14)
        Me.Enabled.TabIndex = 11
        Me.Enabled.UseVisualStyleBackColor = True
        '
        'PictureBox1
        '
        Me.PictureBox1.Location = New System.Drawing.Point(119, 239)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(322, 269)
        Me.PictureBox1.TabIndex = 12
        Me.PictureBox1.TabStop = False
        '
        'ItemID
        '
        Me.ItemID.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ItemID.Location = New System.Drawing.Point(8, 3)
        Me.ItemID.Name = "ItemID"
        Me.ItemID.Size = New System.Drawing.Size(27, 20)
        Me.ItemID.TabIndex = 13
        Me.ItemID.Visible = False
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(56, 215)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(48, 13)
        Me.Label6.TabIndex = 14
        Me.Label6.Text = "Sold Out"
        '
        'SoldOut
        '
        Me.SoldOut.AutoSize = True
        Me.SoldOut.Location = New System.Drawing.Point(119, 215)
        Me.SoldOut.Name = "SoldOut"
        Me.SoldOut.Size = New System.Drawing.Size(15, 14)
        Me.SoldOut.TabIndex = 15
        Me.SoldOut.UseVisualStyleBackColor = True
        '
        'ItemPanel
        '
        Me.ItemPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ItemPanel.Controls.Add(Me.DeleteButton)
        Me.ItemPanel.Controls.Add(Me.SoldOut)
        Me.ItemPanel.Controls.Add(Me.Label6)
        Me.ItemPanel.Controls.Add(Me.ItemID)
        Me.ItemPanel.Controls.Add(Me.PictureBox1)
        Me.ItemPanel.Controls.Add(Me.Enabled)
        Me.ItemPanel.Controls.Add(Me.Label5)
        Me.ItemPanel.Controls.Add(Me.Price)
        Me.ItemPanel.Controls.Add(Me.Label4)
        Me.ItemPanel.Controls.Add(Me.Label3)
        Me.ItemPanel.Controls.Add(Me.Description)
        Me.ItemPanel.Controls.Add(Me.Category)
        Me.ItemPanel.Controls.Add(Me.Label2)
        Me.ItemPanel.Controls.Add(Me.ItemNumber)
        Me.ItemPanel.Controls.Add(Me.Label1)
        Me.ItemPanel.Controls.Add(Me.ItemCloseButton)
        Me.ItemPanel.Controls.Add(Me.ItemSaveButton)
        Me.ItemPanel.Location = New System.Drawing.Point(342, 45)
        Me.ItemPanel.Name = "ItemPanel"
        Me.ItemPanel.Size = New System.Drawing.Size(491, 539)
        Me.ItemPanel.TabIndex = 3
        Me.ItemPanel.Visible = False
        '
        'DeleteButton
        '
        Me.DeleteButton.Location = New System.Drawing.Point(255, 3)
        Me.DeleteButton.Name = "DeleteButton"
        Me.DeleteButton.Size = New System.Drawing.Size(75, 23)
        Me.DeleteButton.TabIndex = 16
        Me.DeleteButton.Text = "Delete"
        Me.DeleteButton.UseVisualStyleBackColor = True
        '
        'Form2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 540)
        Me.Controls.Add(Me.ItemPanel)
        Me.Controls.Add(Me.CategoryPanel)
        Me.Controls.Add(Me.AddMenuItem)
        Me.Controls.Add(Me.TreeView1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "Form2"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Configure"
        Me.CategoryPanel.ResumeLayout(False)
        Me.CategoryPanel.PerformLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ItemPanel.ResumeLayout(False)
        Me.ItemPanel.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents CategoryPanel As Panel
    Friend WithEvents CategoryID As TextBox
    Friend WithEvents CategoryCloseButton As Button
    Friend WithEvents CategoryButtonSave As Button
    Friend WithEvents AddMenuItem As Button
    Friend WithEvents ItemSaveButton As Button
    Friend WithEvents ItemCloseButton As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents ItemNumber As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Category As ComboBox
    Friend WithEvents Description As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents Price As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents Enabled As CheckBox
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents ItemID As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents SoldOut As CheckBox
    Friend WithEvents ItemPanel As Panel
    Friend WithEvents DeleteButton As Button
End Class
