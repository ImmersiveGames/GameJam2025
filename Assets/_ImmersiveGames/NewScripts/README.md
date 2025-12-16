# New Scripts Workspace

Esta pasta isola o novo desenvolvimento sem misturar com o legado em `Assets/_ImmersiveGames/Scripts/`. Use os documentos abaixo para alinhar arquitetura, decisões e utilitários:

- [ADR](docs/adr/ADR.md)
- [ARCHITECTURE](docs/ARCHITECTURE.md)
- [DECISIONS](docs/DECISIONS.md)
- [UTILS-SYSTEMS-GUIDE](docs/UTILS-SYSTEMS-GUIDE.md)

Nenhum script C# deve ser adicionado aqui neste estágio inicial; mantenha apenas documentação e configurações de suporte.

## Como ativar o modo NEWSCRIPTS_MODE

Para impedir que os bootstraps legados rodem (e permitir que o novo pipeline inicialize sozinho), ative o símbolo de compilação `NEWSCRIPTS_MODE` nas Player Settings do Unity:

1. Abra **Project Settings → Player → Other Settings → Scripting Define Symbols**.
2. Acrescente `NEWSCRIPTS_MODE` à lista de símbolos (separe por ponto e vírgula quando houver outros).
3. Salve e rode o Play Mode — os bootstraps legados serão ignorados por causa dos guard clauses condicionais.
