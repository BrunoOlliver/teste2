Imports System.Reflection
Imports System.Globalization
Imports System.IO
Imports System.IO.IsolatedStorage
Imports System.Threading
Imports System.Text
Imports System.Xml
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

Public Class SelectLanguage

    '----------------------------------------------
    'Enums
    '----------------------------------------------
    Public Enum enumStartupMode
        UseDefaultCulture = 0
        UseSavedCulture = 1
        ShowDialog = 2
    End Enum

    Private Enum enumCultureMatch
        None = 0
        Language = 1
        Neutral = 2
        Region = 3
    End Enum

    '----------------------------------------------
    'Member variables
    '----------------------------------------------
    Private StartupMode as enumStartupMode
    Private SelectedCulture as CultureInfo

    'The array of supported cultures is updated automatically by Multi-Language for Visual Studio
    Private SupportedCultures() As String = {"pt-BR", "en"} 'MLHIDE

    '----------------------------------------------
    'Public Methods
    '----------------------------------------------

    Public Sub LoadSettingsAndShow(Optional Byval ForceShow as Boolean = False)

        LoadSettings

        if ForceShow orelse StartupMode = enumStartupMode.ShowDialog then

            'Show the dialog
            me.ShowDialog

            if not lstCultures.SelectedItem is Nothing then
                SelectedCulture = TryCast(lstCultures.SelectedItem, CultureInfo)
            End If

            SaveSettings

        End If

        if StartupMode <> enumStartupMode.UseDefaultCulture then
            if not SelectedCulture is Nothing then

                Thread.CurrentThread.CurrentUICulture = SelectedCulture

                If ForceShow Then
#if true then
                    'The code generated by VS.NET cannot be used to change the 
                    'language of an active form. Show a message to this effect.
                    MessageBox.Show("The settings have been saved." & ControlChars.NewLine & _
                                      "The language change will take full effect the next time you start the program.", _
                                      "Select language", _
                                      MessageBoxButtons.OK)
#else
          MlRuntime.MlRuntime.BroadcastLanguageChanged()
#end if
                End If
            End If
        End If

    End Sub

    '----------------------------------------------
    'Private Methods
    '----------------------------------------------

    '
    ' SaveSettings and LoadSettings use an XML file, saved in so called
    ' Isolated Storage.
    '
    ' I'm not convinced that this is really the best way or the best place
    ' to store this information, but it's certainly a .NET way to do it.
    '

    Private Sub LoadSettings

        'Set the defaults
        StartupMode = enumStartupMode.ShowDialog
        SelectedCulture = Thread.CurrentThread.CurrentUICulture

        ' Create an IsolatedStorageFile object and get the store
        ' for this application.
        Dim isoStorage As IsolatedStorageFile = IsolatedStorageFile.GetUserStoreForDomain

        'Check whether the file exists
        if isoStorage.GetFileNames("CultureSettings.xml").Length > 0 then

            ' Create isoStorage StreamReader.
            Dim stmReader As New StreamReader(New IsolatedStorageFileStream("CultureSettings.xml", FileMode.Open, isoStorage))
            Dim xmlReader As New XmlTextReader(stmReader)

            ' Loop through the XML file until all Nodes have been read and processed.
            Do While xmlReader.Read()
                Select Case xmlReader.Name
                    Case "StartupMode"
                        StartupMode = DirectCast(Integer.Parse(XmlReader.ReadString), enumStartupMode)
                    Case "Culture"
                        Dim CultName as String = XmlReader.ReadString
                        Dim CultInfo as New CultureInfo(CultName)
                        SelectedCulture = CultInfo
                End Select
            Loop

            ' Close the reader
            xmlReader.Close()
            stmReader.Close()

        End If

        isoStorage.Close()

    End Sub

    Private Sub SaveSettings

        ' Get an isolated store for user, domain, and assembly and put it into 
        ' an IsolatedStorageFile object.
        Dim isoStorage As IsolatedStorageFile = IsolatedStorageFile.GetUserStoreForDomain

        ' Create isoStorage StreamWriter and assign it to an XmlTextWriter variable.
        Dim stmWriter As New IsolatedStorageFileStream("CultureSettings.xml", FileMode.Create, isoStorage)
        Dim writer As New XmlTextWriter(stmWriter, Encoding.UTF8)

        writer.Formatting = Formatting.Indented
        writer.WriteStartDocument()
        writer.WriteStartElement("CultureSettings")
        writer.WriteStartElement("StartupMode")
        writer.WriteString(CType(StartupMode, Integer).ToString)
        writer.WriteEndElement()
        writer.WriteStartElement("Culture")
        writer.WriteString(SelectedCulture.Name)
        writer.WriteEndElement()
        writer.WriteEndElement()
        writer.Flush()
        writer.Close()

        stmWriter.Close()
        isoStorage.Close()

    End Sub

    Private Sub SelectLanguage_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Dim Match as enumCultureMatch = enumCultureMatch.None
        Dim NewMatch as enumCultureMatch

        'Version 1 detected which subdirectories are present.

        '     Dim AsmLocation   as String           = [Assembly].GetExecutingAssembly.Location
        '     Dim AsmPath       as String           = Path.GetDirectoryName ( AsmLocation )
        '     Dim DirList       as New System.Collections.Generic.List ( of String )
        '     Dim SubDirName    as String
        '
        '     DirList.AddRange ( Directory.GetDirectories(AsmPath,"??") )
        '     DirList.AddRange ( Directory.GetDirectories(AsmPath,"??-??*") )
        '     For Each SubDirName in DirList
        '       try
        '
        '         Dim BaseName as String = Path.GetFileName ( SubDirName )
        '         Dim Cult     as New CultureInfo ( BaseName )

        'Version 2 used the SupportedCultures array in MlString.h,
        'which is autoamatically updated by Multi-Language for Visual Studio
        '    For Each IetfTag As String In SupportedCultures

        'Version 3 uses the SupportedCultures array in this file, 
        'which is autoamatically updated by Multi-Language for Visual Studio
        For Each IetfTag As String In SupportedCultures
            try

                Dim Cult as New CultureInfo(IetfTag)

                'Note: The property lstCultures.DisplayName is set to "NativeName" in order to
                '      show language name in its own language.
                lstCultures.Items.Add(Cult)

                'The rest of this logic is just to find the nearest match to the 
                'current UI culture.
                'How well does this culture match?        
                if SelectedCulture.Equals(Cult) then
                    NewMatch = enumCultureMatch.Region
                elseif Cult.TwoLetterISOLanguageName = SelectedCulture.TwoLetterISOLanguageName then
                    if Cult.IsNeutralCulture then
                        NewMatch = enumCultureMatch.Neutral
                    else
                        NewMatch = enumCultureMatch.Language
                    End If
                End If

                'Is that better than the best match so far?
                if NewMatch > Match then
                    Match = NewMatch
                    lstCultures.SelectedItem = Cult
                End If

            catch
            End Try
        Next

        select case StartupMode
            case enumStartupMode.ShowDialog
                rbShow.Checked = True
            case enumStartupMode.UseDefaultCulture
                rbDefault.Checked = True
            case enumStartupMode.UseSavedCulture
                rbSelected.Checked = True
        End Select

    End Sub

    Private Sub btOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) _
      Handles btOK.Click, lstCultures.DoubleClick

        if not lstCultures.SelectedItem is Nothing then
            SelectedCulture = TryCast(lstCultures.SelectedItem, CultureInfo)
        End If
        me.Close

    End Sub

    Private Sub OnStartup_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) _
      Handles rbShow.CheckedChanged, rbSelected.CheckedChanged, rbDefault.CheckedChanged

        if rbShow.Checked then
            StartupMode = enumStartupMode.ShowDialog
        else if rbSelected.Checked then
            StartupMode = enumStartupMode.UseSavedCulture
        else if rbDefault.Checked then
            StartupMode = enumStartupMode.UseDefaultCulture
        End If

    End Sub

End Class