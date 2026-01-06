
---

# Checklist — Sistema de Fases (Phase / Stage)

## Marco 0 — Base segura (CONCLUÍDO)

* [x] O sistema sabe quando não reiniciar o mundo (Menu / Startup)
* [x] O sistema sabe quando reiniciar o mundo (Gameplay)
* [x] Reset nunca ocorre durante loading ou transição
* [x] Gameplay só inicia após reset completo
* [x] Pause e PostGame bloqueiam corretamente a simulação

---

## Marco 1 — Fase como conceito explícito

### PhasePlan (fase solicitada)

* [ ] Existe uma forma explícita de solicitar uma fase
* [ ] PhasePlan é definida antes do reset
* [ ] PhasePlan não depende da cena carregada
* [ ] PhasePlan sobrevive a transições de cena
* [ ] Existe log claro de PhasePlan solicitada

Exemplo de evidência:

```
[Phase] Requested phase=GameplayStage1 reason=Menu/Play
```

---

### Active Phase (fase ativa)

* [ ] Existe um conceito explícito de fase ativa
* [ ] Fase ativa só é marcada após reset completo
* [ ] Fase ativa nunca muda durante loading ou transição
* [ ] Existe log claro de fase aplicada

Exemplo de evidência:

```
[Phase] Applied phase=GameplayStage1 context=WorldReset
```

---

## Marco 2 — Conteúdo dependente de fase (futuro)

* [ ] Spawners consultam a fase ativa
* [ ] Conteúdo varia por fase
* [ ] É possível avançar fase sem trocar cena
* [ ] É possível trocar fase com SceneFlow

---

## Marco 3 — Progressão de fases (futuro)

* [ ] Sistema decide próxima fase automaticamente
* [ ] Condições de avanço por fase
* [ ] Persistência de progresso entre fases

---

## Resumo final (não técnico)

> Até aqui, o sistema aprendeu **quando uma fase pode começar**.
> O próximo passo é o sistema saber **qual fase deve começar** —
> e garantir que essa decisão aconteça **antes** do mundo ser criado.

Se quiser, no próximo passo eu já avanço direto para:

* implementação do **PhasePlanService**
* integração mínima com Menu / Restart
* checklist de validação do Marco 1 (logs reais)

Sem retrabalho e sem quebrar o que já está validado.
