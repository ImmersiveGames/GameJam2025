# Foundation / Platform / Config

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Configuração de boot global do runtime.

## Para que serve

- Centralizar referências obrigatórias de boot.
- Evitar busca dispersa por `Resources`.
- Dar ao boot um ponto único de entrada para configs globais.

## Quando usar

Use para configs que precisam existir antes da composição do runtime.

## Como usar

O asset principal identificado no pacote é:

```text
Foundation/Platform/Config/BootstrapConfigAsset.cs
```

Ele deve apontar para configs obrigatórias de boot, como runtime mode e outros assets globais exigidos pelo profile.

## Quando não usar

- Não usar como depósito genérico de configuração de gameplay.
- Não usar para substituir assets autorais de módulos.
- Não usar fallback silencioso quando uma config obrigatória estiver ausente.

## Arquivos principais

- `Foundation/Platform/Config/BootstrapConfigAsset.cs`

## Cuidados

- Config obrigatória inválida deve falhar cedo.
- Cada módulo ainda deve manter seu próprio owner de config quando a configuração for específica dele.
