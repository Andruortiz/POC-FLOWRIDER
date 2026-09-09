# poc/outbox-polling

Rama aislada de [ADR-20](https://app.notion.com/p/3d51a61595fc8104970cf6ab99a7d6ca):
patrón Outbox con un `BackgroundService` (`PollingOutboxRelay`) que hace polling
directo a la tabla `OutboxMessages`, sin ningún locking adicional a nivel de fila.

Comparar contra [`poc/outbox-rabbit`](../../../tree/poc/outbox-rabbit), que sí
reclama las filas antes de procesarlas. Resultados de ambas corriendo juntas
(dos instancias, misma base) en [docs/ADR-20-RESULTADOS.md](../../../docs/ADR-20-RESULTADOS.md).

## Cómo correr

```bash
docker compose up -d postgres   # solo Postgres, esta rama no usa RabbitMQ
cd src/FlowRide.Poc.OutboxPolling
dotnet run
```

Aplica la migración inicial de EF Core automáticamente al iniciar.

```bash
curl -X POST http://localhost:5080/conductores -H "Content-Type: application/json" -d '{"nombre":"Ana"}'
curl -X PUT http://localhost:5080/conductores/{id}/estado -H "Content-Type: application/json" -d '{"estado":"Ocupado"}'
curl http://localhost:5080/outbox
```

## Reproducir la condición de carrera

Correr dos instancias contra la misma base y generar varios cambios de estado casi
al mismo tiempo — ambas van a procesar (loguear) el mismo `OutboxMessage`:

```bash
INSTANCE_NAME=A dotnet run --urls=http://localhost:5080 &
INSTANCE_NAME=B dotnet run --urls=http://localhost:5081 &
```
