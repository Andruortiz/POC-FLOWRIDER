# POC-FLOWRIDER

Pruebas de concepto aisladas para validar (o rechazar) decisiones de arquitectura de
FlowRide que en el [registro de ADRs en Notion](https://app.notion.com/p/3d51a61595fc8188a4f6dcab4fd4034a)
siguen en estado `proposed` — sin evidencia real todavía. Este repo no es el proyecto
FlowRide en sí; es el "banco de pruebas" antes de comprometer código de producción a
una decisión no validada.

Siguiendo la disciplina que el propio registro de ADRs pide ("una PoC chica y
aislada"), **cada tecnología a comparar vive en su propia rama, con su propio
proyecto** — no un solo proyecto con flags de configuración para alternar entre
alternativas. `master` solo tiene lo compartido (este README, `.gitignore`,
`docker-compose.yml` con la infraestructura, y `docs/` con los resultados).

## Ramas

| Rama | ADR | Qué prueba |
|---|---|---|
| [`poc/outbox-polling`](../../tree/poc/outbox-polling) | [ADR-20](https://app.notion.com/p/3d51a61595fc8104970cf6ab99a7d6ca) | Patrón Outbox con `BackgroundService` haciendo polling in-process, sin locking adicional |
| [`poc/outbox-rabbit`](../../tree/poc/outbox-rabbit) | ADR-20 | Mismo patrón Outbox, relay a RabbitMQ con `SELECT ... FOR UPDATE SKIP LOCKED` |
| [`poc/whatsapp-twilio`](../../tree/poc/whatsapp-twilio) | [ADR-23](https://app.notion.com/p/3d51a61595fc81e09611d69cfedf2074) | Envío de plantilla de WhatsApp vía Twilio |
| [`poc/whatsapp-meta`](../../tree/poc/whatsapp-meta) | ADR-23 | Envío de plantilla de WhatsApp vía Meta Cloud API directa |

Cada rama tiene su propio proyecto .NET independiente (no comparten código entre
sí) y su propio README con instrucciones específicas para correrla.

## Infraestructura compartida

`docker-compose.yml` en la raíz levanta Postgres y RabbitMQ — cualquier rama toma
de ahí lo que necesite (las de WhatsApp no necesitan ninguno de los dos):

```bash
docker compose up -d
```

## Resultados

- [docs/ADR-20-RESULTADOS.md](docs/ADR-20-RESULTADOS.md) — corrida real comparando
  `poc/outbox-polling` vs `poc/outbox-rabbit` con dos instancias contra la misma base.
- [docs/ADR-23-README.md](docs/ADR-23-README.md) — qué credenciales faltan para
  correr `poc/whatsapp-twilio` y `poc/whatsapp-meta` de verdad.

## Participantes

Sebastián Zapata Naranjo, Andrés David Ortiz Mejía, Jhonny Estiven Benítez Caro — equipo FlowRide, Software 2.
