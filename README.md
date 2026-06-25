# DBS Logs

Visualizador interno, somente leitura, para consulta dos logs funcionais da aplicação Credibox.

## Estado atual

A estrutura inicial contém uma página ASP.NET de diagnóstico para validar a execução em ASP.NET 4.x, o application pool dedicado e o Website IIS independente.

## Ambiente validado

- Website IIS: `DBSLogs`
- Application Pool: `DBSLogsPool`
- Caminho físico: `C:\inetpub\DBSLogs`
- Endereço inicial: `http://localhost:8081/`

## Publicação manual

Copie os ficheiros para `C:\inetpub\DBSLogs` e execute no PowerShell como Administrador:

```powershell
Import-Module WebAdministration
Start-WebAppPool -Name "DBSLogsPool"
Start-Website -Name "DBSLogs"
```

## Próximas etapas

- ligação read-only ao SQL Server;
- grelha com os 100 registos mais recentes;
- filtros por tipo, utilizador e proposta/operação;
- paginação por `DetailId`;
- página de detalhe carregada sob pedido.
