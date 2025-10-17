# 🎥 Sistema de Câmera para Canvas — Guia de Uso (v1.0)

## 📚 Índice
1. [Visão Geral](#visão-geral)
2. [Responsabilidades](#responsabilidades)
3. [Fluxo de Funcionamento](#fluxo-de-funcionamento)
4. [Integração no Projeto](#integração-no-projeto)
5. [Boas Práticas](#boas-práticas)

---

## 🎯 Visão Geral

O **CanvasCameraBinder** garante que canvases em **World Space** utilizem automaticamente a `Camera.main` do cenário ativo. O objetivo é simplificar a configuração de HUDs locais em cenas com múltiplas câmeras ou transições frequentes, mantendo o setup **determinístico** e **livre de referências diretas** em prefabs.

---

## 🧩 Responsabilidades

* Validar o modo de renderização do `Canvas`.
* Procurar uma câmera com a tag `MainCamera`.
* Associar a referência ao `Canvas.worldCamera` no `OnEnable`.
* Notificar via `Debug.LogWarning` quando a câmera principal não é encontrada, permitindo diagnósticos rápidos.

---

## 🔁 Fluxo de Funcionamento

1. **Awake** — captura o componente `Canvas` requerido.
2. **OnEnable** — aciona `BindCamera()` sempre que o objeto é reativado.
3. **BindCamera** —
   * Ignora canvases que não estejam em `RenderMode.WorldSpace` (evitando sobrecarga desnecessária).
   * Busca `Camera.main`; se ausente, loga um aviso e mantém a configuração atual.
   * Define `canvas.worldCamera = Camera.main` ao encontrar a câmera.

Este fluxo é idempotente e pode ser chamado em cenas com hot-reload ou troca de câmera em runtime (desde que `Camera.main` seja atualizado corretamente).

---

## 🚀 Integração no Projeto

1. **Adicionar ao Canvas**
   * No prefab ou cena, garanta que o `Canvas` esteja em `World Space`.
   * Anexe o componente `CanvasCameraBinder` (ou deixe-o presente via prefab base).

2. **Configurar a Câmera**
   * Certifique-se de que a câmera principal possua a tag `MainCamera`.
   * Para cenários split-screen, altere dinamicamente `Camera.main` via `Camera.tag` para que cada jogador utilize seu canvas correto.

3. **Integração com DI**
   * Não requer injeção; o componente é autocontido.
   * Combine com `DependencyManager` apenas se precisar registrar câmeras específicas como serviços globais.

---

## ✅ Boas Práticas

| Situação | Recomendações |
| --- | --- |
| Cena sem `MainCamera` | Adicione uma câmera padrão ou configure via `Camera.tag = "MainCamera"` em runtime antes do `OnEnable`. |
| Múltiplas câmeras ativas | Controle explicitamente a tag `MainCamera` por jogador durante o setup (idealmente na `GameManagerStateMachine`). |
| UI não atualiza direção | Garanta que o canvas permaneça em World Space; Screen Space não precisa de binder. |
| Prefabs compartilhados | Deixe o componente no prefab raiz para que todas as instâncias recebam o bind automaticamente. |

Este módulo respeita os princípios SOLID ao manter uma única responsabilidade e permitir substituição fácil caso novas estratégias de binding sejam necessárias no futuro.
