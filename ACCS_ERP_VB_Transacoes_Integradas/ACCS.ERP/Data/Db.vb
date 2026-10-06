Imports MySqlConnector
Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace Data
    Public Module Db
        Private ReadOnly SettingsPath As String = Path.Combine(AppContext.BaseDirectory, "appsettings.json")

        Public Function ConnectionString() As String
            If Not File.Exists(SettingsPath) Then
                Throw New FileNotFoundException("Ficheiro appsettings.json não encontrado.", SettingsPath)
            End If

            Dim json = File.ReadAllText(SettingsPath)
            Using doc = JsonDocument.Parse(json)
                If Not doc.RootElement.TryGetProperty("ConnectionStrings", Nothing) Then
                    Throw New InvalidDataException("appsettings.json não contém a secção 'ConnectionStrings'.")
                End If

                Dim cs As String = Nothing
                Try
                    cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("AccsMySql").GetString()
                Catch ex As Exception
                    Throw New InvalidDataException("Chave 'ConnectionStrings:AccsMySql' não encontrada em appsettings.json.", ex)
                End Try

                If String.IsNullOrWhiteSpace(cs) Then
                    Throw New InvalidDataException("Connection string 'AccsMySql' está vazia em appsettings.json.")
                End If

                ' Se a password estiver vazia e não existir SslMode, adicionar opções seguras para ambiente de desenvolvimento
                Dim pwdMatch = Regex.Match(cs, "(?:^|;)\s*Password\s*=\s*([^;]*)", RegexOptions.IgnoreCase)
                Dim pwd = If(pwdMatch.Success, pwdMatch.Groups(1).Value, Nothing)
                If pwd IsNot Nothing AndAlso pwd = String.Empty AndAlso Not cs.ToLowerInvariant().Contains("sslmode=") Then
                    cs &= ";SslMode=None;AllowPublicKeyRetrieval=True"
                End If

                Return cs
            End Using
        End Function

        ''' <summary>
        ''' Abre uma conexão síncrona com a base de dados.
        ''' </summary>
        Public Function OpenConnection() As MySqlConnection
            Dim cn = New MySqlConnection(ConnectionString())
            cn.Open()
            Return cn
        End Function

        ''' <summary>
        ''' Abre uma conexão de forma assíncrona para não bloquear a UI thread.
        ''' </summary>
        Public Async Function OpenConnectionAsync() As Task(Of MySqlConnection)
            Dim cn = New MySqlConnection(ConnectionString())
            Await cn.OpenAsync()
            Return cn
        End Function

        ''' <summary>
        ''' Testa a ligação à base de dados e retorna uma mensagem detalhada.
        ''' Uso: Dim msg = Await Db.TestConnectionAsync()
        ''' </summary>
        Public Async Function TestConnectionAsync() As Task(Of String)
            Try
                Using cn = New MySqlConnection(ConnectionString())
                    Await cn.OpenAsync()
                    Using cmd = New MySqlCommand("SELECT 1", cn)
                        Dim result = Await cmd.ExecuteScalarAsync()
                        If result IsNot Nothing AndAlso Convert.ToInt32(result) = 1 Then
                            Return "OK: Ligação bem-sucedida à base de dados 'accs_escritorio'."
                        Else
                            Return "ERRO: Ligação estabelecida, mas teste SQL devolveu resultado inesperado."
                        End If
                    End Using
                End Using
            Catch ex As Exception
                Return $"ERRO: Não foi possível ligar à base de dados. {ex.GetType().Name}: {ex.Message}"
            End Try
        End Function
    End Module
End Namespace
