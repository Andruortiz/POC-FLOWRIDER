# poc/whatsapp-twilio

Rama aislada de [ADR-23](https://app.notion.com/p/3d51a61595fc81e09611d69cfedf2074):
lado Twilio de la comparación Twilio vs. Meta Cloud API para WhatsApp. Sin base de
datos ni infraestructura — solo el SDK oficial de Twilio.

Comparar contra [`poc/whatsapp-meta`](../../../tree/poc/whatsapp-meta). Qué
credenciales hacen falta y cómo conseguirlas: [docs/ADR-23-README.md](../../../docs/ADR-23-README.md).

## Cómo correr

```bash
cd src/FlowRide.Poc.WhatsAppTwilio
cp .env.example .env   # llenar TWILIO_ACCOUNT_SID / TWILIO_AUTH_TOKEN / TWILIO_WHATSAPP_FROM
dotnet run
```

```bash
curl -X POST http://localhost:5080/notificaciones/whatsapp/enviar-prueba \
  -H "Content-Type: application/json" \
  -d '{"numero":"+57XXXXXXXXXX","plantilla":"Hola {0}, tu viaje esta confirmado para las {1}","parametros":["Ana","3:00pm"]}'
```

Sin credenciales, responde `400` con un mensaje claro de qué falta (comportamiento
esperado, no un bug). La respuesta exitosa incluye `duracion` — repetir varias
veces y promediar para tener un número de latencia comparable contra Meta.
