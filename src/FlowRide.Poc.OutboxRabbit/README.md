# poc/outbox-rabbit

Rama aislada de [ADR-20](https://app.notion.com/p/3d51a61595fc8104970cf6ab99a7d6ca):
patrón Transactional Outbox + Message Relay. `RabbitOutboxPublisher` reclama filas
de `OutboxMessages` con `SELECT ... FOR UPDATE SKIP LOCKED` antes de publicarlas a
RabbitMQ; `RabbitOutboxConsumer` las procesa compitiendo por la cola
(competing consumers).

Comparar contra [`poc/outbox-polling`](../../../tree/poc/outbox-polling), que no
reclama las filas y por eso duplica el procesamiento con más de una instancia.
Resultados de ambas corriendo juntas en
[docs/ADR-20-RESULTADOS.md](../../../docs/ADR-20-RESULTADOS.md).

## Cómo correr

```bash
docker compose up -d   # Postgres + RabbitMQ, esta rama necesita ambos
cd src/FlowRide.Poc.OutboxRabbit
dotnet run
```

Aplica la migración inicial de EF Core automáticamente al iniciar. La consola de
management de RabbitMQ queda en http://localhost:15672 (usuario/clave `flowride`).

```bash
curl -X POST http://localhost:5080/conductores -H "Content-Type: application/json" -d '{"nombre":"Ana"}'
curl -X PUT http://localhost:5080/conductores/{id}/estado -H "Content-Type: application/json" -d '{"estado":"Ocupado"}'
curl http://localhost:5080/outbox
```

## Reproducir la ausencia de duplicados con dos instancias

```bash
INSTANCE_NAME=A dotnet run --urls=http://localhost:5080 &
INSTANCE_NAME=B dotnet run --urls=http://localhost:5081 &
```

Cada mensaje se relaya una sola vez (el publisher que pierde la carrera por el
`SKIP LOCKED` simplemente no encuentra filas para reclamar) y se consume una sola
vez, repartido entre A y B.
