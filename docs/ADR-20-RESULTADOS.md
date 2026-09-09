# ADR-20 — Resultados de la PoC (2026-09-09)

> Esta corrida se hizo con las dos alternativas en un solo proyecto combinado
> (config `Outbox:Mode=Polling|Rabbit`), antes de separarlas en las ramas
> `poc/outbox-polling` y `poc/outbox-rabbit`. La lógica de cada relay es
> exactamente la misma que quedó en su rama — solo cambió cómo se organiza el
> código, no el comportamiento — así que estos resultados siguen siendo válidos.

Corrida real, no simulada: dos instancias de `FlowRide.Api` (puertos 5080 y 5081,
`INSTANCE_NAME=A`/`B`) contra el mismo PostgreSQL, primero en modo `Polling` y
después en modo `Rabbit`, con RabbitMQ real corriendo vía `docker compose`.

## Modo `Polling` (`Outbox:Mode=Polling`, default)

Se crearon 5 conductores y se les cambió el estado casi simultáneamente (5
`OutboxMessage` pendientes de golpe). Resultado en los logs de ambas instancias:

```
A: Procesando OutboxMessage e8e8e8f7... (ConductorEstadoCambiado)
A: Procesando OutboxMessage 63288902... (ConductorEstadoCambiado)
A: Procesando OutboxMessage 45ed5b01... (ConductorEstadoCambiado)
A: Procesando OutboxMessage 20c62efb... (ConductorEstadoCambiado)
A: Procesando OutboxMessage 33f34ecc... (ConductorEstadoCambiado)

B: Procesando OutboxMessage e8e8e8f7... (ConductorEstadoCambiado)
B: Procesando OutboxMessage 63288902... (ConductorEstadoCambiado)
B: Procesando OutboxMessage 45ed5b01... (ConductorEstadoCambiado)
B: Procesando OutboxMessage 20c62efb... (ConductorEstadoCambiado)
B: Procesando OutboxMessage 33f34ecc... (ConductorEstadoCambiado)
```

**Las 5 notificaciones (100%) fueron procesadas por las dos instancias.** Confirma
exactamente el riesgo que describe el ADR: sin locking adicional, dos instancias
leyendo la misma tabla `OutboxMessages` sin coordinación procesan el mismo mensaje
más de una vez — en producción esto sería, por ejemplo, mandar la misma
notificación de WhatsApp dos veces al mismo pasajero.

Efecto secundario encontrado (no estaba explícito en el ADR): como las dos
instancias también compiten para *escribir* `ProcesadoEn`/`ProcesadoPor`, el
registro final en la base solo refleja la última escritura (siempre `B` en esta
corrida) — el "lost update" esconde el problema en la auditoría si solo se mira la
tabla al final; solo los logs de aplicación exponen que ambas instancias
efectivamente ejecutaron la lógica de negocio.

## Modo `Rabbit` (`Outbox:Mode=Rabbit`)

Mismo experimento, 5 conductores nuevos, con las dos instancias corriendo
`RabbitOutboxPublisher` + `RabbitOutboxConsumer` cada una.

**Lado publisher** (reclama filas con `SELECT ... FOR UPDATE SKIP LOCKED` antes de
publicar):
```
B: Relayado OutboxMessage 3081e9b3... a RabbitMQ
B: Relayado OutboxMessage d368ddaa... a RabbitMQ
B: Relayado OutboxMessage a89041c0... a RabbitMQ
B: Relayado OutboxMessage 068dcbea... a RabbitMQ
B: Relayado OutboxMessage 6cb251a0... a RabbitMQ
```
Las 5 filas fueron reclamadas por una sola instancia (en esta corrida, `B` ganó la
carrera las 5 veces); `A` no relayó ninguna — no porque falló, sino porque al
consultar con `SKIP LOCKED` ya no quedaban filas libres para reclamar. **Cero
duplicados en el relay**, a diferencia del modo Polling.

**Lado consumer** (competing consumers sobre la cola `flowride.outbox`):
```
A: Consumido mensaje ... conductorId=4e477fe1...
A: Consumido mensaje ... conductorId=e633b208...
A: Consumido mensaje ... conductorId=49292e81...
B: Consumido mensaje ... conductorId=7c405a8d...
B: Consumido mensaje ... conductorId=dfe118b4...
```
5 conductorId distintos, cada uno consumido exactamente una vez, repartidos entre
las dos instancias (3 en A, 2 en B) — el patrón competing-consumers de RabbitMQ
funciona como se esperaba incluso con dos instancias corriendo en paralelo.

## Conclusión

Con datos reales (no solo el razonamiento en papel del ADR):

- **Polling in-process sin locking: descartado para más de una instancia.** Duplicó
  el 100% de los mensajes en esta corrida. Solo sería aceptable si el monolito
  corre con una única instancia — lo cual limita el escalado horizontal que pide
  el Escenario 2, así que no es una solución real a largo plazo.
- **Transactional Outbox + Message Relay con RabbitMQ: funciona correctamente con
  múltiples instancias**, siempre que el relay reclame filas con `SKIP LOCKED` (o
  un mecanismo equivalente) antes de publicar — la sola presencia de RabbitMQ no
  alcanza, el paso de reclamar filas es lo que realmente resuelve el problema.

**Recomendación para Notion:** pasar ADR-20 de `proposed` a `accepted`, eligiendo
la alternativa "Broker de mensajes externo (RabbitMQ)" — pero documentando
explícitamente que el mecanismo de claim (`SKIP LOCKED`) es parte necesaria de la
decisión, no un detalle de implementación libre.
