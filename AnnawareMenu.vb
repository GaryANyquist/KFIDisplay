Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports Newtonsoft.Json.Linq

''' <summary>
''' The menu board straight from annaware.com (2.0.25, Gary 2026-10-28: "Business key in KFIDisplay"). With
''' AnnawareKey=crk_... in C:\KFDisplay\display.ini, the board reads the business's menu from annaware.com instead of
''' the KFDisplay SQL tables: no SQL Server, sync service or photo copying is needed. annaware.com keeps the menu in the
''' shapes Wix's APIs use (the register's lib/wixMenu.ts reads the same): menus (sectionIds, in order), sections
''' (itemIds, in order), items (priceInfo or priceVariants, labels, visible, orderSettings.inStock, image.url) and
''' discountRules. The board's menu is the setup pages' Business -> "Menu board shows" (boardMenu), else display.ini's
''' Menu=. Fetched at most once a minute; when annaware.com can't be reached the last menu stays on the board.
''' </summary>
Public Class AnnawareMenu
    Public Const Api As String = "https://www.annaware.com/CashRegister/api.ashx"
    Private Const HideLabel As String = "DontShowOnMenu"
    'Unticked "On the menu board" on the setup pages (2.0.26): on the registers, not on the board.
    Private Const BoardHideLabel As String = "DontShowOnMenuBoard"
    Private Const PhotoDir As String = "C:\KFDisplay\photos"

    Private Shared CatalogCache As JObject
    Private Shared BoardMenuCache As String = ""
    Private Shared FetchedAt As DateTime = DateTime.MinValue

    ''' <summary>True when display.ini has an annaware.com business key.</summary>
    Public Shared ReadOnly Property Enabled As Boolean
        Get
            Return DisplaySettings.AnnawareKey <> ""
        End Get
    End Property

    Private Shared Function Post(ByVal Body As String) As JObject
        ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol Or CType(3072, SecurityProtocolType)
        Dim Req As HttpWebRequest = CType(WebRequest.Create(Api), HttpWebRequest)
        Req.Method = "POST"
        Req.ContentType = "application/json"
        Req.Headers("X-Business-Key") = DisplaySettings.AnnawareKey
        Req.Timeout = 20000
        Dim Bytes As Byte() = Encoding.UTF8.GetBytes(Body)
        Using S As Stream = Req.GetRequestStream()
            S.Write(Bytes, 0, Bytes.Length)
        End Using
        Try
            Using Res As WebResponse = Req.GetResponse()
                Using R As New StreamReader(Res.GetResponseStream(), Encoding.UTF8)
                    Return JObject.Parse(R.ReadToEnd())
                End Using
            End Using
        Catch ex As WebException
            'annaware.com answers errors as {error: "..."}: say that rather than "(403) Forbidden".
            If ex.Response IsNot Nothing Then
                Using R As New StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)
                    Dim Text As String = R.ReadToEnd()
                    Try
                        Dim J As JObject = JObject.Parse(Text)
                        If J("error") IsNot Nothing Then Throw New Exception("annaware.com: " & J("error").ToString())
                    Catch inner As Newtonsoft.Json.JsonException
                    End Try
                End Using
            End If
            Throw
        End Try
    End Function

    ''' <summary>The menu (refreshed at most once a minute); the last one when annaware.com can't be reached.</summary>
    Public Shared Function Catalog() As JObject
        If CatalogCache Is Nothing OrElse (DateTime.Now - FetchedAt).TotalSeconds >= 60 Then
            Try
                CatalogCache = Post("{""action"":""catalog""}")
                Try
                    Dim B As JObject = Post("{""action"":""businessSettings""}")
                    Dim S As JObject = TryCast(B("settings"), JObject)
                    BoardMenuCache = If(S IsNot Nothing AndAlso S("boardMenu") IsNot Nothing, S("boardMenu").ToString(), "")
                Catch
                End Try
                FetchedAt = DateTime.Now
            Catch
                If CatalogCache Is Nothing Then Throw
                FetchedAt = DateTime.Now 'try again in a minute
            End Try
        End If
        Return CatalogCache
    End Function

    ''' <summary>The menu the board shows: the setup pages' choice, else display.ini's Menu= ("" = every menu).</summary>
    Public Shared Function BoardMenuId() As String
        Catalog()
        Dim Id As String = If(BoardMenuCache <> "", BoardMenuCache, DisplaySettings.MenuId)
        'A register's menu ids are "wxm_" + the menu's own id.
        If Id.StartsWith("wxm_") Then Id = Id.Substring(4)
        Return Id
    End Function

    Private Shared Function List(ByVal C As JObject, ByVal Name As String) As JArray
        Dim A As JArray = TryCast(C(Name), JArray)
        Return If(A, New JArray())
    End Function

    Private Shared Function Str(ByVal O As JToken, ByVal Name As String) As String
        Dim J As JObject = TryCast(O, JObject)
        If J Is Nothing OrElse J(Name) Is Nothing OrElse J(Name).Type = JTokenType.Null Then Return ""
        Return J(Name).ToString()
    End Function

    Private Shared Function Cents(ByVal V As String) As Long
        Dim D As Decimal
        If Decimal.TryParse(V, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, D) Then Return CLng(Math.Round(D * 100D))
        Return 0
    End Function

    Private Class BoardItem
        Public Item As JObject
        Public Section As String
    End Class

    ''' <summary>The board's items in order (menu, then section, then item), each once, as the registers show them.</summary>
    Private Shared Function BoardItems(ByVal C As JObject) As List(Of BoardItem)
        Dim MenuId As String = BoardMenuId()
        Dim Sections As New Dictionary(Of String, JObject)
        For Each S As JObject In List(C, "sections")
            Sections(Str(S, "id")) = S
        Next
        Dim Items As New Dictionary(Of String, JObject)
        For Each I As JObject In List(C, "items")
            Items(Str(I, "id")) = I
        Next
        Dim HideId As String = "", BoardHideId As String = ""
        For Each L As JObject In List(C, "labels")
            If Str(L, "name") = HideLabel Then HideId = Str(L, "id")
            If Str(L, "name") = BoardHideLabel Then BoardHideId = Str(L, "id")
        Next
        Dim Order As New List(Of String)
        For Each M As JObject In List(C, "menus")
            If MenuId <> "" AndAlso Str(M, "id") <> MenuId Then Continue For
            For Each Sid As JToken In If(TryCast(M("sectionIds"), JArray), New JArray())
                If Not Order.Contains(Sid.ToString()) Then Order.Add(Sid.ToString())
            Next
        Next
        Dim Out As New List(Of BoardItem)
        Dim Seen As New HashSet(Of String)
        For Each Sid As String In Order
            Dim S As JObject = Nothing
            If Not Sections.TryGetValue(Sid, S) Then Continue For
            If S("visible") IsNot Nothing AndAlso S("visible").Type = JTokenType.Boolean AndAlso Not S.Value(Of Boolean)("visible") Then Continue For
            For Each Iid As JToken In If(TryCast(S("itemIds"), JArray), New JArray())
                Dim I As JObject = Nothing
                If Not Items.TryGetValue(Iid.ToString(), I) OrElse Not Seen.Add(Iid.ToString()) Then Continue For
                If I("visible") IsNot Nothing AndAlso I("visible").Type = JTokenType.Boolean AndAlso Not I.Value(Of Boolean)("visible") Then Continue For
                Dim Hidden As Boolean = False
                For Each L As JToken In If(TryCast(I("labels"), JArray), New JArray())
                    If HideId <> "" AndAlso Str(L, "id") = HideId Then Hidden = True
                    If BoardHideId <> "" AndAlso Str(L, "id") = BoardHideId Then Hidden = True
                Next
                If Hidden Then Continue For
                Out.Add(New BoardItem With {.Item = I, .Section = Str(S, "name")})
            Next
        Next
        Return Out
    End Function

    ''' <summary>Lowest and highest price in cents (sizes are Wix price variants).</summary>
    Private Shared Sub Prices(ByVal I As JObject, ByRef Low As Long, ByRef High As Long)
        Dim Vs As JArray = TryCast(If(I("priceVariants"), New JObject())("variants"), JArray)
        Dim List As New List(Of Long)
        If Vs IsNot Nothing Then
            For Each V As JToken In Vs
                Dim P As String = Str(V("priceInfo"), "price")
                If P = "" Then P = Str(V, "price")
                List.Add(Cents(P))
            Next
        End If
        Dim Own As String = Str(I("priceInfo"), "price")
        If Own <> "" AndAlso List.Count < 2 Then
            Low = Cents(Own)
            High = Low
        ElseIf List.Count > 0 Then
            Low = List.Min()
            High = List.Max()
        Else
            Low = 0
            High = 0
        End If
    End Sub

    ''' <summary>The items a deal's scopes name: item ids, "menu_item" ids, sections and whole menus.</summary>
    Private Shared Function ScopeItems(ByVal C As JObject, ByVal Scopes As JArray) As HashSet(Of String)
        Dim Out As New HashSet(Of String)
        If Scopes Is Nothing Then Return Out
        For Each Sc As JToken In Scopes
            Dim P As JObject = TryCast(If(TryCast(Sc, JObject)?("customFilter"), New JObject())("params"), JObject)
            If P Is Nothing Then Continue For
            For Each X As JToken In If(TryCast(P("itemIds"), JArray), New JArray())
                Out.Add(X.ToString())
            Next
            For Each X As JToken In If(TryCast(P("menuItemIds"), JArray), New JArray())
                Dim T As String = X.ToString()
                Out.Add(If(T.Contains("_"), T.Substring(T.IndexOf("_") + 1), T))
            Next
            Dim SectionIds As New List(Of String)
            For Each X As JToken In If(TryCast(P("sectionIds"), JArray), New JArray())
                SectionIds.Add(X.ToString())
            Next
            For Each X As JToken In If(TryCast(P("menuIds"), JArray), New JArray())
                For Each M As JObject In List(C, "menus")
                    If Str(M, "id") = X.ToString() Then
                        For Each Sid As JToken In If(TryCast(M("sectionIds"), JArray), New JArray())
                            SectionIds.Add(Sid.ToString())
                        Next
                    End If
                Next
            Next
            For Each S As JObject In List(C, "sections")
                If SectionIds.Contains(Str(S, "id")) Then
                    For Each Iid As JToken In If(TryCast(S("itemIds"), JArray), New JArray())
                        Out.Add(Iid.ToString())
                    Next
                End If
            Next
        Next
        Return Out
    End Function

    Private Shared ReadOnly Days As String() = {"SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"}

    ''' <summary>
    ''' Whether a deal applies now: on, within its dates and its weekly hours, and with no condition (Gary 2026-10-27:
    ''' only dates and times may decide a board price; a minimum order or Buy X get Y depends on the order).
    ''' </summary>
    Private Shared Function InEffect(ByVal Rule As JObject, ByVal Now As DateTime) As Boolean
        If Rule("active") IsNot Nothing AndAlso Rule("active").Type = JTokenType.Boolean AndAlso Not Rule.Value(Of Boolean)("active") Then Return False
        Dim Trig As JObject = TryCast(Rule("trigger"), JObject)
        If Trig IsNot Nothing AndAlso Trig.Count > 0 AndAlso Str(Trig, "triggerType") <> "UNDEFINED" Then Return False
        Dim Ati As JObject = TryCast(Rule("activeTimeInfo"), JObject)
        If Ati Is Nothing Then Return True
        Dim T As DateTime
        If Str(Ati, "start") <> "" AndAlso DateTime.TryParse(Str(Ati, "start"), Nothing, Globalization.DateTimeStyles.RoundtripKind, T) AndAlso Now < T.ToLocalTime() Then Return False
        If Str(Ati, "end") <> "" AndAlso DateTime.TryParse(Str(Ati, "end"), Nothing, Globalization.DateTimeStyles.RoundtripKind, T) AndAlso Now >= T.ToLocalTime() Then Return False
        Dim Windows As JArray = TryCast(If(TryCast(If(TryCast(Ati("recurrence"), JObject), New JObject())("weeklySchedule"), JObject), New JObject())("timeWindows"), JArray)
        If Windows Is Nothing OrElse Windows.Count = 0 Then Return Ati("recurrence") Is Nothing
        Dim Minute As Integer = Now.Hour * 60 + Now.Minute
        For Each W As JObject In Windows
            Dim Day As Integer = Array.IndexOf(Days, Str(W, "dayOfWeek").ToUpper().PadRight(3).Substring(0, 3))
            Dim From As Integer = CInt(Val(Str(W("startTime"), "hours"))) * 60 + CInt(Val(Str(W("startTime"), "minutes")))
            Dim Upto As Integer = CInt(Val(Str(W("endTime"), "hours"))) * 60 + CInt(Val(Str(W("endTime"), "minutes")))
            If Upto > From Then
                If Day = Now.DayOfWeek AndAlso Minute >= From AndAlso Minute < Upto Then Return True
            Else
                'Past midnight: from its start that day, until its end the next day.
                If (Day = Now.DayOfWeek AndAlso Minute >= From) OrElse ((Day + 1) Mod 7 = Now.DayOfWeek AndAlso Minute < Upto) Then Return True
            End If
        Next
        Return False
    End Function

    ''' <summary>The biggest deal on an item right now, in cents off its (lowest) price.</summary>
    Private Shared Function OffCents(ByVal C As JObject, ByVal ItemId As String, ByVal Price As Long, ByVal Now As DateTime) As Long
        Dim Best As Long = 0
        For Each Rule As JObject In List(C, "discountRules")
            If Not InEffect(Rule, Now) Then Continue For
            For Each V As JObject In If(TryCast(If(TryCast(Rule("discounts"), JObject), New JObject())("values"), JArray), New JArray())
                If Str(V, "targetType") <> "" AndAlso Str(V, "targetType") <> "SPECIFIC_ITEMS" Then Continue For
                Dim Info As JObject = TryCast(V("specificItemsInfo"), JObject)
                If Info Is Nothing Then Continue For
                Dim Covered As HashSet(Of String) = ScopeItems(C, TryCast(Info("scopes"), JArray))
                Covered.ExceptWith(ScopeItems(C, TryCast(Info("excludedScopes"), JArray)))
                If Not Covered.Contains(ItemId) Then Continue For
                Dim Off As Long = 0
                Select Case Str(V, "discountType")
                    Case "PERCENTAGE" : Off = CLng(Math.Round(Price * Val(Str(V, "percentage")) / 100.0))
                    Case "FIXED_AMOUNT" : Off = Math.Min(Cents(Str(V, "fixedAmount")), Price)
                    Case "FIXED_PRICE" : Off = Math.Max(Price - Cents(Str(V, "fixedPrice")), 0)
                End Select
                If Off > Best Then Best = Off
            Next
        Next
        Return Best
    End Function

    ''' <summary>
    ''' The board rows after AfterSortOrder, with the columns Form1.FillMenu reads from the SQL query: Category,
    ''' ItemNumber (the item's name), Description, Price, Price2 (the dearest size, or NULL), SoldOut, SortOrder and
    ''' SalesPrice (after today's deal, or -1).
    ''' </summary>
    Public Shared Function BoardPage(ByVal AfterSortOrder As Long) As DataSet
        Dim C As JObject = Catalog()
        Dim T As New DataTable()
        T.Columns.Add("Category", GetType(String))
        T.Columns.Add("ItemNumber", GetType(String))
        T.Columns.Add("Description", GetType(String))
        T.Columns.Add("Price", GetType(Decimal))
        T.Columns.Add("Price2", GetType(Decimal))
        T.Columns.Add("SoldOut", GetType(Integer))
        T.Columns.Add("SortOrder", GetType(Long))
        T.Columns.Add("SalesPrice", GetType(Decimal))
        Dim Now As DateTime = DateTime.Now
        Dim N As Long = 0
        For Each B As BoardItem In BoardItems(C)
            N += 1
            If N <= AfterSortOrder Then Continue For
            Dim Low As Long, High As Long
            Prices(B.Item, Low, High)
            Dim Off As Long = OffCents(C, Str(B.Item, "id"), Low, Now)
            Dim InStock As JToken = If(TryCast(B.Item("orderSettings"), JObject), New JObject())("inStock")
            T.Rows.Add(B.Section, Str(B.Item, "name"), Str(B.Item, "description"), Low / 100D,
                       If(High > Low, CType(High / 100D, Object), DBNull.Value),
                       If(InStock IsNot Nothing AndAlso InStock.Type = JTokenType.Boolean AndAlso Not InStock.Value(Of Boolean)(), 1, 0),
                       N, If(Off > 0, (Low - Off) / 100D, -1D))
        Next
        Dim DS As New DataSet()
        DS.Tables.Add(T)
        Return DS
    End Function

    ''' <summary>A random photo of a board item for the big picture: (file on this PC, caption), or Nothing.</summary>
    Public Shared Function RandomPhoto() As Tuple(Of String, String)
        Dim Withs As New List(Of JObject)
        For Each B As BoardItem In BoardItems(Catalog())
            If Str(B.Item("image"), "url") <> "" Then Withs.Add(B.Item)
        Next
        If Withs.Count = 0 Then Return Nothing
        Dim R As New Random()
        For Tries As Integer = 1 To 3
            Dim I As JObject = Withs(R.Next(Withs.Count))
            Dim F As String = PhotoFile(Str(I("image"), "url"))
            If F <> "" Then Return Tuple.Create(F, Str(I, "name"))
        Next
        Return Nothing
    End Function

    ''' <summary>The photo at Url, downloaded once into C:\KFDisplay\photos (a new photo always has a new address).</summary>
    Private Shared Function PhotoFile(ByVal Url As String) As String
        Try
            Dim Name As String
            Using H As SHA1 = SHA1.Create()
                Name = BitConverter.ToString(H.ComputeHash(Encoding.UTF8.GetBytes(Url))).Replace("-", "").ToLower() & ".jpg"
            End Using
            Dim F As String = Path.Combine(PhotoDir, Name)
            If File.Exists(F) Then Return F
            Directory.CreateDirectory(PhotoDir)
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol Or CType(3072, SecurityProtocolType)
            Dim Fetch As String = Url
            'Wix's own photo address (a menu copied before its photos were): 800px from Wix's image service.
            If Url.StartsWith("https://static.wixstatic.com/media/") AndAlso Not Url.Contains("/v1/") Then Fetch = Url & "/v1/fit/w_800,h_800,q_85/photo.jpg"
            Using W As New WebClient()
                W.DownloadFile(Fetch, F & ".part")
            End Using
            File.Move(F & ".part", F)
            Return F
        Catch ex As Exception
            Return ""
        End Try
    End Function
End Class
