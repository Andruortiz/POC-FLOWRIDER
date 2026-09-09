# poc/whatsapp-meta

Rama aislada de [ADR-23](https://app.notion.com/p/3d51a61595fc81e09611d69cfedf2074):
lado Meta Cloud API directa de la comparación Twilio vs. Meta para WhatsApp. Sin
base de datos ni infraestructura — solo `HttpClient` contra la Graph API (no hay
SDK oficial de Meta para .NET).

Comparar contra [`poc/whatsapp-twilio`](../../../tree/poc/whatsapp-twilio). Qué
credenciales hacen falta y cómo conseguirlas: [docs/ADR-23-README.md](../../../docs/ADR-23-README.md)
— en particular, cronometrar cuánto tarda conseguirlas, que es justo lo que el
ADR pide re-verificar.

## Cómo correr

```bash
cd src/FlowRide.Poc.WhatsAppMeta
cp .env.example .env   # llenar META_ACCESS_TOKEN / META_PHONE_NUMBER_ID
dotnet run
```

```bash
curl -X POST http://localhost:5080/notificaciones/whatsapp/enviar-prueba \
  -H "Content-Type: application/json" \
  -d '{"numero":"57XXXXXXXXXX","plantilla":"nombre_de_la_plantilla_aprobada","parametros":["Ana","3:00pm"]}'
```

Sin credenciales, responde `400` con un mensaje claro de qué falta (comportamiento
esperado, no un bug). La respuesta exitosa incluye `duracion` — repetir varias
veces y promediar para tener un número de latencia comparable contra Twilio.
