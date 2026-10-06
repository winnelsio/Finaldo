Imports System.IO
Imports System.Threading
Imports System.Windows.Forms

Module Program
    <STAThread>
    Public Sub Main()
        ' Inicializar configurações básicas da aplicação WinForms
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Captura exceções não tratadas na Thread da UI WinForms
        AddHandler Application.ThreadException, AddressOf OnThreadException
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)

        ' Captura exceções não tratadas no AppDomain
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException

        ' Testar ligação à base de dados antes de abrir a UI
        Try
            Dim testMsg = Task.Run(Function() Data.Db.TestConnectionAsync()).GetAwaiter().GetResult()
            If Not testMsg.StartsWith("OK:") Then
                MessageBox.Show("Erro ao ligar à base de dados:" & Environment.NewLine & testMsg, "ACCS ERP — Erro de BD", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
        Catch ex As Exception
            MessageBox.Show($"Erro ao verificar ligação à base de dados: {ex.Message}", "ACCS ERP — Erro de BD", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        Application.Run(New Forms.LoginForm())
    End Sub

    Private Sub OnThreadException(sender As Object, e As ThreadExceptionEventArgs)
        LogError("UI Thread Exception", e.Exception)
        MessageBox.Show("Ocorreu um erro inesperado na aplicação:" & Environment.NewLine & e.Exception.Message,
                        "ACCS ERP — Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    Private Sub OnUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
        Dim ex = TryCast(e.ExceptionObject, Exception)
        If ex IsNot Nothing Then
            LogError("Unhandled AppDomain Exception", ex)
        End If
    End Sub

    Private Sub LogError(source As String, ex As Exception)
        Try
            Dim logPath As String = Path.Combine(AppContext.BaseDirectory, "App.log")
            Dim logMessage As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{New String("-"c, 80)}{Environment.NewLine}"
            File.AppendAllText(logPath, logMessage)
        Catch
            ' Evita erros ao tentar escrever no ficheiro de log
        End Try
    End Sub
End Module
