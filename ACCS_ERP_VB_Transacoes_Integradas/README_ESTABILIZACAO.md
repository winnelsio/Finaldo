# ACCS ERP Desktop — Base Estabilizada

Esta revisão corrige os problemas estruturais encontrados na varredura inicial.

## Correcções aplicadas
- removidas palavras-passe de demonstração/fallback; autenticação apenas BCrypt;
- autenticação agora valida também a empresa do utilizador;
- empresas/filiais carregadas da tabela `companies`;
- removidas empresas fictícias fixas do formulário de login;
- numeração de documentos passa a usar `document_sequences` dentro da mesma transacção;
- removido IVA 16% inventado como fallback: sem configuração/taxa no produto, a taxa é 0 até ser configurada;
- clientes e produtos passam a ser desactivados (`active=0`) em vez de apagados;
- incluído `database/001_stabilization.sql`.

## Antes de executar
1. Faça backup da base MySQL.
2. Reveja e execute `ACCS.ERP/database/001_stabilization.sql`.
3. Configure a ligação em `ACCS.ERP/appsettings.json`.
4. Abra `ACCS.ERP.vbproj` no Visual Studio 2022 com .NET 8.
5. Restaure os pacotes NuGet e compile.

## Importante sobre IVA
O sistema não deve assumir uma taxa fiscal sem configuração. O script cria `fiscal_settings.default_tax_rate` inicialmente como 0. Defina a taxa aplicável à empresa após confirmar a configuração fiscal pretendida.

## Próxima etapa
Criar movimentos de stock/conta corrente e o formulário completo de emissão de documentos, preservando transacções e auditoria.
