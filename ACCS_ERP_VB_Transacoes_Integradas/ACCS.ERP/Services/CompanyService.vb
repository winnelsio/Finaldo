Imports MySqlConnector
Imports ACCS.ERP.Data
Imports ACCS.ERP.Models

Namespace Services
    Public Class CompanyService
        Public Async Function ListActiveAsync() As Task(Of List(Of Company))
            Dim result As New List(Of Company)()
            Using cn = Await Db.OpenConnectionAsync()
                Dim hasActive As Boolean
                Using check As New MySqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='companies' AND COLUMN_NAME='active'", cn)
                    hasActive = Convert.ToInt32(Await check.ExecuteScalarAsync()) > 0
                End Using
                Dim sql = "SELECT id, name FROM companies" & If(hasActive, " WHERE active=1", "") & " ORDER BY name"
                Using cmd As New MySqlCommand(sql, cn)
                    Using rd = Await cmd.ExecuteReaderAsync()
                        While Await rd.ReadAsync()
                            result.Add(New Company With {.Id=Convert.ToInt32(rd("id")), .Name=rd("name").ToString()})
                        End While
                    End Using
                End Using
            End Using
            Return result
        End Function
    End Class
End Namespace
