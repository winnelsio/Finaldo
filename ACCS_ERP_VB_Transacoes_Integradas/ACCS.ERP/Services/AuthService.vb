Imports MySqlConnector
Imports BCrypt.Net
Imports ACCS.ERP.Data
Imports ACCS.ERP.Models
Imports System.IO
Imports System.Threading.Tasks

Namespace Services
    Public Class AuthService

        ''' <summary>
        ''' Autentica o utilizador de forma assíncrona contra a base de dados MySQL
        ''' validando a hash BCrypt compatível com PHP password_hash().
        ''' </summary>
        Public Async Function LoginAsync(username As String, password As String, Optional companyId As Integer? = Nothing) As Task(Of UserSession)
            If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(password) Then
                Return Nothing
            End If

            Using cn = Await Db.OpenConnectionAsync()
                Dim sql = "SELECT id, company_id, name, username, password_hash, role, active " &
                          "FROM users WHERE LOWER(username)=LOWER(@username)"
                If companyId.HasValue Then sql &= " AND company_id=@companyId"
                sql &= " LIMIT 1"

                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@username", username.Trim())
                    If companyId.HasValue Then cmd.Parameters.AddWithValue("@companyId", companyId.Value)

                    Using rd = Await cmd.ExecuteReaderAsync()
                        If Not Await rd.ReadAsync() Then
                            LogAudit($"Falha de autenticação: Utilizador '{username}' não encontrado.")
                            Return Nothing
                        End If

                        If Convert.ToInt32(rd("active")) <> 1 Then
                            LogAudit($"Falha de autenticação: Conta '{username}' desactivada/suspensa.")
                            Return Nothing
                        End If

                        Dim hash = If(rd("password_hash") Is DBNull.Value, "", rd("password_hash").ToString())
                        Dim valid As Boolean = False

                        If Not String.IsNullOrWhiteSpace(hash) Then
                            Try
                                valid = BCrypt.Net.BCrypt.Verify(password, hash)
                            Catch
                                valid = False
                            End Try
                        End If

                        If Not valid Then
                            LogAudit($"Falha de autenticação: Palavra-passe incorrecta para '{username}'.")
                            Return Nothing
                        End If

                        Dim session As New UserSession With {
                            .Id = Convert.ToInt32(rd("id")),
                            .Name = rd("name").ToString(),
                            .Username = rd("username").ToString(),
                            .Role = rd("role").ToString()
                        }

                        If companyId.HasValue Then
                            session.CompanyId = companyId.Value
                        ElseIf rd("company_id") IsNot DBNull.Value Then
                            session.CompanyId = Convert.ToInt32(rd("company_id"))
                        Else
                            session.CompanyId = 1
                        End If

                        LogAudit($"Autenticação bem-sucedida: Utilizador '{session.Username}' ({session.Role}) na Empresa #{session.CompanyId}.")
                        Return session
                    End Using
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Método síncrono para compatibilidade com versões anteriores.
        ''' </summary>
        Public Function Login(username As String, password As String, Optional companyId As Integer? = Nothing) As UserSession
            Return Task.Run(Function() LoginAsync(username, password, companyId)).GetAwaiter().GetResult()
        End Function

        Private Shared Sub LogAudit(message As String)
            Try
                Dim logPath = Path.Combine(AppContext.BaseDirectory, "App.log")
                Dim entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [AUTH] {message}{Environment.NewLine}"
                File.AppendAllText(logPath, entry)
            Catch
            End Try
        End Sub
    End Class
End Namespace
