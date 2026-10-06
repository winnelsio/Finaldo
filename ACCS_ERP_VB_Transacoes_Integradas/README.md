# ACCS ERP Desktop — VB.NET

Primeira base da migração do ACCS ERP PHP para Windows Forms / VB.NET.

## Fase 1
- .NET 8 Windows Forms
- MySQL através de MySqlConnector
- Login compatível com hashes bcrypt gerados pelo PHP `password_hash()`
- Leitura da tabela `users`
- Dashboard inicial

## Como executar
1. Instale o Visual Studio 2022 com **Desktop development with .NET**.
2. Abra `ACCS.ERP.vbproj`.
3. Ajuste `appsettings.json` para a sua base MySQL.
4. Garanta que a tabela `users` da base ACCS esteja disponível.
5. Execute o projecto.

## Próxima fase
Migrar Clientes:
- lista/pesquisa
- novo cliente
- editar cliente
- NUIT/telefone/email/endereço
- saldo
- empresa/tenant
- permissões

Depois: Produtos/Stock e, em seguida, Documentos/Facturação.

> Não coloque credenciais reais no repositório Git. O `appsettings.json` incluído contém apenas valores de desenvolvimento.
